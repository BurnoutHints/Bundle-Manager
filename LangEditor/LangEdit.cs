using PluginAPI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LangEditor
{
    public partial class LangEdit : Form, IEntryEditor
    {
        public delegate void OnChanged();
        public event OnChanged Changed;

        private bool _ignoreChanges;
        private bool _suppressHandlers;
        private bool _dirty; // Grid has edits that haven't been applied to the resource
        private object _originalCellValue;

        private string searchVal;
        private Dictionary<uint, string> dict;
        private Language _lang;
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public Language Lang
        {
            get => _lang;
            set
            {
                _ignoreChanges = true;
                _suppressHandlers = true;
                
                _lang = value;
                UpdateDisplay();

                _suppressHandlers = false;
                _ignoreChanges = false;
            }
        }

        public LangEdit()
        {
            _ignoreChanges = true;
            _suppressHandlers = true;
            
            InitializeComponent();
            FormatCellDimensions();
            GenerateDictionary();
            
            dgvMain.CellBeginEdit += dgvMain_CellBeginEdit;
            dgvMain.CellEndEdit += dgvMain_CellEndEdit;
            dgvMain.CellValueChanged += dgvMain_CellValueChanged;
            dgvMain.UserDeletedRow += (sender, e) => _dirty = true;
            FormClosing += LangEdit_FormClosing;
            
            _suppressHandlers = false;
            _ignoreChanges = false;
        }

        private void FormatCellDimensions()
        {
            dgvMain.RowTemplate.Height = 20;

            int width = TextRenderer.MeasureText("0x00000000", dgvMain.Font).Width + 16;
            colID.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colID.MinimumWidth = width;
            colID.Width = width;

            // Strings can contain line breaks, so wrapping makes the cell editor multiline
            colString.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        }

        private void dgvMain_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (!_ignoreChanges && !_suppressHandlers)
                _dirty = true;
        }

        private void LangEdit_FormClosing(object sender, FormClosingEventArgs e)
        {
            dgvMain.EndEdit();
            if (!_dirty)
                return;

            DialogResult result = MessageBox.Show(this, "Apply changes before closing?", "Unapplied changes",
                MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
            if (result == DialogResult.Cancel || (result == DialogResult.Yes && RebuildLanguage() != 0))
                e.Cancel = true;
        }

        private void dgvMain_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            DataGridView grid = (DataGridView)sender;
            _originalCellValue = grid[e.ColumnIndex, e.RowIndex].Value;
        }

        private void dgvMain_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            if (_suppressHandlers)
                return;

            DataGridView grid = (DataGridView)sender;
            DataGridViewColumn column = grid.Columns[e.ColumnIndex];

            if (column.Name != "colID" && column.Name != "colName")
                return;

            DataGridViewCell cell = grid[e.ColumnIndex, e.RowIndex];
            object newValue = cell.Value;

            if (Equals(_originalCellValue, newValue))
                return;

            // A new or empty cell has nothing to overwrite, so only confirm when replacing a value
            if (!string.IsNullOrEmpty(Convert.ToString(_originalCellValue)))
            {
                string associated = column.Name == "colName" ? "ID" : "Name";
                DialogResult result = MessageBox.Show(
                    this,
                    $"Are you sure you want to apply this change to this {column.HeaderText}?" +
                    $"\nThis will update the associated {associated} field!" +
                    $"\n\nNew: {newValue}\nOriginal: {_originalCellValue}",
                    "Confirm change",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                );

                if (result == DialogResult.No)
                {
                    _suppressHandlers = true;
                    cell.Value = _originalCellValue;
                    _suppressHandlers = false;
                    _originalCellValue = null;
                    return;
                }
            }

            _suppressHandlers = true;
            try
            {
                DataGridViewRow row = grid.Rows[e.RowIndex];

                if (column.Name == "colName")
                {
                    string name = Convert.ToString(newValue) ?? string.Empty;
                    if (name.Length > 0)
                    {
                        uint id = Language.HashID(name);
                        row.Cells["colID"].Value = $"0x{id:X8}";
                    }
                }
                else if (column.Name == "colID")
                {
                    string idText = Convert.ToString(newValue) ?? string.Empty;
                    idText = idText.Trim();

                    if (idText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        idText = idText[2..];

                    if (!uint.TryParse(idText, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint id))
                    {
                        MessageBox.Show(
                            this,
                            "ID must be a valid 32-bit hexadecimal value, for example 0x00000001.",
                            "Invalid ID",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning
                        );

                        cell.Value = _originalCellValue;
                        _originalCellValue = null;
                        return;
                    }

                    row.Cells["colID"].Value = $"0x{id:X8}";

                    if (dict.TryGetValue(id, out string resolvedName))
                        row.Cells["colName"].Value = resolvedName;
                    else
                        row.Cells["colName"].Value = string.Empty;
                }
            }
            finally
            {
                _suppressHandlers = false;
                _originalCellValue = null;
            }
        }

        public void GenerateDictionary()
        {
            dict = new Dictionary<uint, string>();
            string keysDir = AppContext.BaseDirectory + "keys/";
            if (!File.Exists(keysDir + "keys.txt"))
                return;

            // Names only label rows, so a duplicate key or hash collision keeps the first name
            foreach (string key in File.ReadLines(keysDir + "keys.txt", Encoding.UTF8))
            {
                if (key.Contains("<f")) // Use all keys in a secondary list
                {
                    int subKeyFilePos = key.IndexOf("<f") + 2;
                    string subKeyFile = key.Substring(subKeyFilePos, key.IndexOf(">") - subKeyFilePos);
                    if (!File.Exists(keysDir + subKeyFile + ".txt"))
                        continue;
                    foreach (string subKey in File.ReadLines(keysDir + subKeyFile + ".txt", Encoding.UTF8))
                    {
                        string fullSubKey = key.Replace(subKeyFile, subKey); // Replace file name with subkey
                        fullSubKey = fullSubKey.Remove(subKeyFilePos - 2, 2); // Remove <f
                        fullSubKey = fullSubKey.Remove(subKeyFilePos - 2 + subKey.Length, 1); // Remove >
                        dict.TryAdd(Language.HashID(fullSubKey), fullSubKey);
                    }

                    continue;
                }
                else if (key.Contains("<r")) // Range. Prone to false positives
                {
                    int subKeyRangePos = key.IndexOf("<r") + 2;
                    string subKeyRange = key.Substring(subKeyRangePos, key.IndexOf(">") - subKeyRangePos);
                    int start = int.Parse(subKeyRange.Substring(0, subKeyRange.IndexOf('-')));
                    int end = int.Parse(subKeyRange.Substring(subKeyRange.IndexOf('-') + 1));
                    for (int i = start; i <= end; ++i)
                    {
                        string fullSubKey = key.Replace(subKeyRange, i.ToString());
                        fullSubKey = fullSubKey.Remove(subKeyRangePos - 2, 2); // Remove <f
                        fullSubKey = fullSubKey.Remove(subKeyRangePos - 2 + i.ToString().Length, 1); // Remove >
                        dict.TryAdd(Language.HashID(fullSubKey), fullSubKey);
                    }
                }

                dict.TryAdd(Language.HashID(key), key);
            }
        }

        public void UpdateDisplay()
        {
            bool ignoreChanges = _ignoreChanges;
            _ignoreChanges = true;

            int resolved = 0;
            foreach (KeyValuePair<uint, string> entry in _lang.mpEntries)
            {
                if (dict.TryGetValue(entry.Key, out string name))
                    resolved++;
                dgvMain.Rows.Add($"0x{entry.Key:X8}", name ?? "", entry.Value);
            }

            _ignoreChanges = ignoreChanges;
            lblStatus.Text = $"{_lang.LanguageName}: {_lang.mpEntries.Count} strings, {resolved} names resolved";
        }

        private void SelectRow(DataGridViewRow row)
        {
            dgvMain.ClearSelection();
            dgvMain.CurrentCell = row.Cells[0];
            row.Selected = true;
        }

        public int RebuildLanguage()
        {
            if (_ignoreChanges)
                return 1;

            dgvMain.EndEdit(); // Include the cell being edited
            Dictionary<uint, string> data = new Dictionary<uint, string>();

            foreach (DataGridViewRow row in dgvMain.Rows)
            {
                if (row.IsNewRow)
                    continue;

                string idString = (string)row.Cells[0].Value;

                if (idString == null)
                {
                    SelectRow(row);
                    MessageBox.Show(this, "ID cannot be left blank", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 2;
                }

                uint id;
                if (idString.StartsWith("0x") && idString.Length == 10) // Hash
                {
                    if (!uint.TryParse(idString[2..], NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out id))
                    {
                        SelectRow(row);
                        MessageBox.Show(this, "Failed to parse ID \"" + idString+ "\"", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return 3;
                    }
                }
                else // String
                {
                    id = Language.HashID(idString);
                }

                string value = (string)row.Cells[2].Value;
                if (data.ContainsKey(id))
                {
                    SelectRow(row);
                    MessageBox.Show(this, "ID " + idString + " already in use", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 4;
                }
                data.Add(id, value);
            }

            _lang.mpEntries = data;
            _dirty = false;

            Changed?.Invoke();

            return 0;
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Column-separated values (*.csv)|*.csv|All files(*.*)|*.*";
            if (openDialog.ShowDialog() == DialogResult.Cancel)
                return;

            string error = ImportCSV(openDialog.FileName);
            if (error != null)
                MessageBox.Show(this, error + " Import cancelled.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        internal string ImportCSV(string path)
        {
            Dictionary<uint, string> data = [];
            int row = 0;
            try
            {
                using StreamReader reader = new(path, Encoding.UTF8);
                foreach (List<string> fields in ReadCSV(reader))
                {
                    row++;
                    string idText = fields[0].Trim();
                    string name = fields.Count > 1 ? fields[1].Trim() : "";

                    uint key;
                    if (idText.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!uint.TryParse(idText[2..], NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out key))
                            return $"Could not parse ID {idText} on row {row}.";
                    }
                    else if (idText.Length != 0)
                        key = Language.HashID(idText);
                    else if (name.Length != 0)
                        key = Language.HashID(name);
                    else
                        return $"Row {row} has no ID or name.";

                    string value = string.Join(",", fields.Skip(2)); // An unquoted string may contain commas
                    value = value.Replace("\\r", "\xD").Replace("\\n", "\xA"); // Convert newlines

                    if (!data.TryAdd(key, value))
                        return $"ID {idText} on row {row} is a duplicate.";
                }
            }
            catch (InvalidDataException)
            {
                return $"Row {row + 1} has a quote that is never closed.";
            }
            catch (IOException ex)
            {
                return ex.Message;
            }

            _lang.mpEntries = data;

            dgvMain.Rows.Clear();
            UpdateDisplay();
            _dirty = true;
            return null;
        }

        private static IEnumerable<List<string>> ReadCSV(TextReader reader)
        {
            List<string> fields = [];
            StringBuilder field = new();
            bool quoted = false;
            bool rowStarted = false;
            int c;
            while ((c = reader.Read()) != -1)
            {
                if (quoted)
                {
                    if (c != '"')
                        field.Append((char)c);
                    else if (reader.Peek() == '"')
                        field.Append((char)reader.Read());
                    else
                        quoted = false;
                }
                else if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && reader.Peek() == '\n')
                        reader.Read();
                    if (rowStarted)
                    {
                        fields.Add(field.ToString());
                        yield return fields;
                    }
                    fields = [];
                    field.Clear();
                    rowStarted = false;
                    continue;
                }
                else if (c == '"')
                    quoted = true;
                else if (c == ',')
                {
                    fields.Add(field.ToString());
                    field.Clear();
                }
                else
                    field.Append((char)c);
                rowStarted = true;
            }

            if (quoted)
                throw new InvalidDataException("Unterminated quote");
            if (rowStarted)
            {
                fields.Add(field.ToString());
                yield return fields;
            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Column-separated values (*.csv)|*.csv";
            if (saveDialog.ShowDialog() == DialogResult.Cancel)
                return;

            string error = ExportCSV(saveDialog.FileName);
            if (error == null)
                MessageBox.Show("Export successful.", "Information", MessageBoxButtons.OK);
            else
                MessageBox.Show($"There was an issue exporting: {error}", "Error", MessageBoxButtons.OK);
        }

        // Keys are prepended with 0x and padded to 8 characters.
        // In values, incompatible characters are escaped.
        // Keys and names are enclosed in double quotes, as are non-empty values.
        internal string ExportCSV(string path)
        {
            dgvMain.EndEdit(); // Include the cell being edited
            StringBuilder languageContent = new();
            foreach (DataGridViewRow row in dgvMain.Rows)
            {
                if (row.IsNewRow)
                    continue;

                languageContent.Append('"').Append(row.Cells[0].Value).Append("\",\"").Append(row.Cells[1].Value).Append("\",");

                string tempValue = (string)row.Cells[2].Value ?? "";
                tempValue = tempValue.Replace("\xD", "\\r").Replace("\xA", "\\n"); // Convert newlines
                tempValue = tempValue.Replace("\"", "\"\""); // Convert double quotes
                if (tempValue.Length != 0) // Enclose non-empty strings in double quotes
                    languageContent.Append('"').Append(tempValue).Append('"');
                languageContent.Append("\r\n");
            }

            try
            {
                // The BOM lets spreadsheet apps detect UTF-8
                File.WriteAllText(path, languageContent.ToString(), new UTF8Encoding(true));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                return ex.Message;
            }
            return null;
        }

        private void applyChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int result = RebuildLanguage();

            if (result == 0)
                lblStatus.Text = $"{_lang.LanguageName}: {_lang.mpEntries.Count} strings, changes applied";
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to search for.");
            if (value == null)
                return;
            searchVal = value;
            findNextToolStripMenuItem.Enabled = true;
            FindFrom(0);
        }

        private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (searchVal == null)
                return;
            FindFrom(dgvMain.CurrentCell == null ? 0 : dgvMain.CurrentCell.RowIndex + 1);
        }

        private void FindFrom(int start)
        {
            int count = dgvMain.Rows.Count;
            for (int i = 0; i < count; ++i)
            {
                DataGridViewRow row = dgvMain.Rows[(start + i) % count];
                if (row.IsNewRow)
                    continue;

                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value is string text && text.Contains(searchVal, StringComparison.CurrentCultureIgnoreCase))
                    {
                        SelectRow(row);
                        return;
                    }
                }
            }

            MessageBox.Show(this, $"\"{searchVal}\" not found.", "Find", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void hashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to hash.");
            if (value == null)
                return;
            uint result = Language.HashID(value);

            MessageBox.Show(this, $"Hashed value is: 0x{result:X8}", "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dgvMain_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            dgvMain.Rows[e.RowIndex].ReadOnly = false;
        }
    }
}
