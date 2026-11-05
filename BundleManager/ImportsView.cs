using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using BundleFormat;
using HexEditor;

namespace BundleManager;

public class ImportsView : UserControl
{
    private const int OffsetColumn = 0, IDColumn = 1, ResourceColumn = 2;
    private const int PreviewRows = 8;

    private readonly DataGridView grid;
    private readonly HexView preview;
    private readonly Font inferredFont;
    private BundleEntry entry;
    private List<Dependency> imports = [];

    public event Action<BundleEntry> OpenEntry;

    public event Action Changed;

    public bool CommitEdit() => grid.EndEdit();

    public ImportsView()
    {
        grid = new()
        {
            Dock = DockStyle.Fill,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            BackgroundColor = SystemColors.Window
        };
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "Offset",
            ToolTipText = "Where the pointer to the resource goes in the primary data.",
            FillWeight = 15,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "ID",
            ToolTipText = "ID of the resource it points to.",
            FillWeight = 15,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });
        grid.Columns.Add(new DataGridViewTextBoxColumn()
        {
            HeaderText = "Resource",
            ToolTipText = "Click a resource in this bundle to open it.",
            ReadOnly = true,
            FillWeight = 70,
            SortMode = DataGridViewColumnSortMode.NotSortable
        });

        grid.CellEndEdit += (_, e) => Apply(e.RowIndex, e.ColumnIndex);
        grid.CellContentClick += (_, e) =>
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == ResourceColumn && Target(imports[e.RowIndex].ID) is BundleEntry target)
                OpenEntry?.Invoke(target);
        };
        grid.CurrentCellChanged += (_, _) => ShowPointer();
        grid.DataError += (_, e) => e.ThrowException = false;

        preview = new()
        {
            Dock = DockStyle.Bottom,
            Font = new("Courier New", 10F)
        };
        preview.Height = preview.Font.Height * PreviewRows + 12;

        Controls.Add(grid);
        Controls.Add(preview);
        inferredFont = new(grid.Font, FontStyle.Italic);
    }

    [Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public BundleEntry Entry
    {
        get => entry;
        set
        {
            entry = value;
            imports = entry.GetImports();
            preview.HexData = entry.GetPrimaryWithoutImports();
            grid.Rows.Clear();

            foreach (Dependency _ in imports)
                FillRow(grid.Rows.Add());

            ShowPointer();
        }
    }

    private int PointerSize => entry.Platform == BundlePlatform.PCx64
        ? 8
        : 4;

    private BundleEntry Target(ulong id) => entry.Archive?.Entries.FindLast(e => e.ID == id);

    private void FillRow(int index)
    {
        Dependency import = imports[index];
        DataGridViewRow row = grid.Rows[index];
        row.Cells[OffsetColumn].Value = $"0x{import.EntryPointerOffset:X}";
        row.Cells[OffsetColumn].ErrorText = import.EntryPointerOffset + PointerSize > preview.HexData.Length
            ? "The pointer goes past the end of the primary data."
            : "";
        row.Cells[IDColumn].Value = $"0x{import.ID:X8}";
        row.Cells[IDColumn].ErrorText = "";

        // Only resources that can be opened are links
        BundleEntry target = Target(import.ID);
        DataGridViewCell cell = target != null
            ? new DataGridViewLinkCell()
            {
                TrackVisitedState = false
            }
            : new DataGridViewTextBoxCell();

        row.Cells[ResourceColumn] = cell;
        (cell.Value, bool inferred) = Describe(import.ID, target);
        cell.Style.Font = inferred
            ? inferredFont
            : null;
        cell.ToolTipText = inferred
            ? "Likely name for this resource"
            : "";
    }

    // Names no bundle stores are italic, as in the resource list
    private (string Text, bool Inferred) Describe(ulong id, BundleEntry target)
    {
        string name = target != null
            ? target.DebugInfo.Name
            : BundleCache.GetEntryDebugInfoByID(id).Name;
        bool inferred = string.IsNullOrWhiteSpace(name);

        if (inferred)
            name = ResourceDB.Lookup(id) ?? entry.Archive?.GuessName(id);

        inferred &= !string.IsNullOrEmpty(name);

        EntryType? currentType = target?.Type ?? BundleCache.GetInfoByEntryID(id)?.Type;
        string label = string.Join(" ", ((string[])[currentType?.ToString(), name]).Where(s => !string.IsNullOrEmpty(s)));

        if (target != null)
            return ($"{label} (#{target.Index:D3})", inferred: inferred);

        string currentFile = BundleCache.GetFileByEntryID(id);
        return ((currentFile != null
            ? $"In {BundleCache.GetRelativePath(currentFile)}"
            : "Not in this bundle") + (label != ""
            ? ": " + label
            : ""), inferred: inferred);
    }

    // A value that isn't usable goes back to what it was, with the reason on the cell
    private void Apply(int index, int column)
    {
        Dependency import = imports[index];
        string error = null;

        if (!ResourceImport.TryParseHex((grid.Rows[index].Cells[column].Value as string ?? "").Trim(), out uint value))
            error = "Enter a hexadecimal number.";
        else if (column == OffsetColumn && value + PointerSize > preview.HexData.Length)
            error = $"The pointer has to fit in the primary data, which is 0x{preview.HexData.Length:X} bytes.";
        else if (column == IDColumn && value == 0)
            error = "Enter the ID of a resource.";
        else if (column == OffsetColumn)
            import.EntryPointerOffset = value;
        else
            import.ID = value;

        if (error == null && !import.Equals(imports[index]))
        {
            imports[index] = import;
            entry.SetPrimary(preview.HexData, imports);
            Changed?.Invoke();
        }

        BeginInvoke(() => // Not while the grid is still ending the edit
        {
            FillRow(index);

            if (error != null)
                grid.Rows[index].Cells[column].ErrorText = error;

            ShowPointer();
        });
    }

    private void ShowPointer()
    {
        if (grid.CurrentRow?.Index is int index && index < imports.Count)
            preview.Select((int)imports[index].EntryPointerOffset, PointerSize);
    }
}
