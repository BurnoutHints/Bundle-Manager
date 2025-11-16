using PluginAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace LangEditor
{
    public partial class LangEdit : Form, IEntryEditor
    {
        public delegate void OnChanged();
        public event OnChanged Changed;

        private bool _ignoreChanges;

        private string searchVal;
        private Dictionary<uint, string> dict;
        private Language _lang;
        public Language Lang
        {
            get => _lang;
            set
            {
                _ignoreChanges = true;
                _lang = value;
                UpdateDisplay();
                _ignoreChanges = false;
            }
        }

        public LangEdit()
        {
            _ignoreChanges = true;
            InitializeComponent();
            GenerateDictionary();
            _ignoreChanges = false;
        }

        public void GenerateDictionary()
        {
            dict = new Dictionary<uint, string>();
            StreamReader keys = new StreamReader(AppContext.BaseDirectory + "keys/keys.txt", Encoding.UTF8);
            string key;
            while ((key = keys.ReadLine()) != null)
            {
                if (key.Contains("<f")) // Use all keys in a secondary list
                {
                    int subKeyFilePos = key.IndexOf("<f") + 2;
                    string subKeyFile = key.Substring(subKeyFilePos, key.IndexOf(">") - subKeyFilePos);
                    StreamReader subKeys = new StreamReader(AppContext.BaseDirectory + "keys/" + subKeyFile + ".txt", Encoding.UTF8);
                    string subKey;
                    while ((subKey = subKeys.ReadLine()) != null)
                    {
                        string fullSubKey = key.Replace(subKeyFile, subKey); // Replace file name with subkey
                        fullSubKey = fullSubKey.Remove(subKeyFilePos - 2, 2); // Remove <f
                        fullSubKey = fullSubKey.Remove(subKeyFilePos - 2 + subKey.Length, 1); // Remove >
                        try
                        {
                            dict.Add(Language.HashID(fullSubKey), fullSubKey);
                        }
                        catch
                        {
                            MessageBox.Show("ID " + fullSubKey + " (0x" + Language.HashID(fullSubKey).ToString("X8") + ") already exists.", "Error", MessageBoxButtons.OK);
                            continue;
                        }
                    }

                    subKeys.Close();

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
                        dict.Add(Language.HashID(fullSubKey), fullSubKey);
                    }
                }

                dict.Add(Language.HashID(key), key);
            }

            keys.Close();
        }

        public void UpdateDisplay()
        {
            int resolved = 0, total = 0;
            foreach (KeyValuePair<uint, string> entry in _lang.mpEntries)
            {
                if (dict.ContainsKey(entry.Key))
                {
                    dgvMain.Rows.Add(dict[entry.Key], entry.Value);
                    total++;
                    resolved++;
                }
                else
                {
                    dgvMain.Rows.Add("0x" + entry.Key.ToString("X8"), entry.Value);
                    total++;
                }
            }
            MessageBox.Show("Resolved " + resolved + "/" + total + " keys.", "Information",
                MessageBoxButtons.OK);
        }

        public int RebuildLanguage()
        {
            if (_ignoreChanges)
                return 1;

            Dictionary<uint, string> data = new Dictionary<uint, string>();

            for (int i = 0; i < dgvMain.Rows.Count - 1; ++i)
            {
                string idString = (string)dgvMain.Rows[i].Cells[0].Value;

                if (idString == null)
                {
                    MessageBox.Show(this, "ID cannot be left blank", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 2;
                }

                uint id;
                if (idString.StartsWith("0x") && idString.Length == 10) // Hash
                {
                    if (!uint.TryParse(idString[2..], NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out id))
                    {
                        MessageBox.Show(this, "Failed to parse ID \"" + idString + "\"", "Warning",
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return 3;
                    }
                }
                else // String
                {
                    id = Language.HashID(idString);
                }

                string value = (string)dgvMain.Rows[i].Cells[1].Value;
                if (data.ContainsKey(id))
                {
                    MessageBox.Show(this, "ID " + idString + " already in use", "Warning",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return 4;
                }
                data.Add(id, value);
            }

            _lang.mpEntries = data;

            Changed?.Invoke();

            return 0;
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "Column-separated values (*.csv)|*.csv|All files(*.*)|*.*";
            if (openDialog.ShowDialog() == DialogResult.Cancel)
                return;

            Dictionary<uint, string> data = new Dictionary<uint, string>();
            StreamReader file = new StreamReader(openDialog.FileName, Encoding.UTF8);
            string line;
            while ((line = file.ReadLine()) != null)
            {
                string[] pair = line.Split(',', 2);
                pair[0] = pair[0].Remove(0, 1).Remove(pair[0].Length - 2, 1); // Remove quotes
                uint key;
                if (pair[0].StartsWith("0x") && pair[0].Length == 10)
                {
                    if (!uint.TryParse(pair[0][2..], NumberStyles.AllowHexSpecifier, CultureInfo.CurrentCulture, out key))
                    {
                        MessageBox.Show("Could not parse ID " + pair[0] + ". Import cancelled.", "Error", MessageBoxButtons.OK);
                        return;
                    }
                }
                else
                    key = Language.HashID(pair[0]);

                if (pair[1].Length != 0) // Non-empty value
                {
                    pair[1] = pair[1].Substring(1, pair[1].Length - 2); // Remove enclosing double quotes
                    pair[1] = pair[1].Replace("\"\"", "\""); // Convert double quotes
                    pair[1] = pair[1].Replace("\\r", "\xD").Replace("\\n", "\xA"); // Convert newlines
                }

                data.Add(key, pair[1]);
            }

            _lang.mpEntries = data;

            dgvMain.Rows.Clear();
            UpdateDisplay();
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.Filter = "Column-separated values (*.csv)|*.csv";
            if (saveDialog.ShowDialog() == DialogResult.Cancel)
                return;

            // CSV content as a single contiguous string.
            // Keys are prepended with 0x and padded to 8 characters.
            // In values, incompatible characters are escaped.
            // Both keys and values are enclosed in double quotes.
            string languageContent = "";
            for (int i = 0; i < dgvMain.Rows.Count - 1; ++i)
            {
                languageContent += "\"" + (string)(dgvMain.Rows[i].Cells[0].Value) + "\",";
                string tempValue = (string)dgvMain.Rows[i].Cells[1].Value;
                tempValue = tempValue.Replace("\xD", "\\r").Replace("\xA", "\\n"); // Convert newlines
                tempValue = tempValue.Replace("\"", "\"\""); // Convert double quotes
                if (tempValue.Length != 0) // Enclose non-empty strings in double quotes
                {
                    tempValue = "\"" + tempValue;
                    tempValue += "\"";
                }
                languageContent += tempValue + "\r\n";
            }

            try
            {
                FileStream file = (FileStream)saveDialog.OpenFile();
                file.Write(Encoding.UTF8.GetBytes(languageContent));
                file.Flush();
                file.Close();

                MessageBox.Show("Export successful.", "Information", MessageBoxButtons.OK);
            }
            catch (IOException ex)
            {
                MessageBox.Show($"There was an issue exporting: {ex.Message}", "Error", MessageBoxButtons.OK);

            }
        }

        private void applyChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            int result = RebuildLanguage();

            if (result == 0)
                MessageBox.Show(this, "Done!", "Information", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void findToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to search for.");
            if (value == null)
                return;
            searchVal = value;
            findNextToolStripMenuItem.Enabled = true;

            foreach (DataGridViewRow row in dgvMain.Rows)
            {
                if ((row.Cells[0].Value != null && ((string)row.Cells[0].Value).Contains(value, StringComparison.CurrentCultureIgnoreCase))
                    || (row.Cells[1].Value != null && ((string)row.Cells[1].Value).Contains(value, StringComparison.CurrentCultureIgnoreCase)))
                {
                    dgvMain.ClearSelection();
                    dgvMain.CurrentCell = row.Cells[0];
                    row.Selected = true;
                    return;
                }
            }

            MessageBox.Show(this, "Hash not found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void findNextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (searchVal == null)
                return;
            int start = dgvMain.SelectedCells[0].RowIndex + 1;
            if (start > dgvMain.Rows.Count - 1)
            {
                MessageBox.Show(this, "Hash not found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            for (int i = start; i < dgvMain.Rows.Count - 1; ++i)
            {
                if (((string)dgvMain.Rows[i].Cells[0].Value).Contains(searchVal, StringComparison.CurrentCultureIgnoreCase)
                    || ((string)dgvMain.Rows[i].Cells[1].Value).Contains(searchVal, StringComparison.CurrentCultureIgnoreCase))
                {
                    dgvMain.ClearSelection();
                    dgvMain.CurrentCell = dgvMain.Rows[i].Cells[0];
                    dgvMain.Rows[i].Selected = true;
                    return;
                }
            }

            MessageBox.Show(this, "Hash not found!", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void hashToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string value = InputDialog.ShowInput(this, "Please enter the value to hash.");
            if (value == null)
                return;
            uint result = Language.HashID(value);

            MessageBox.Show(this, "Hashed value is: " + result.ToString("X8"), "Information",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void dgvMain_RowsAdded(object sender, DataGridViewRowsAddedEventArgs e)
        {
            dgvMain.Rows[e.RowIndex].ReadOnly = false;
        }
    }
}
