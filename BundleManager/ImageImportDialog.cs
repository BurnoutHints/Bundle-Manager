using BurnoutImage;
using System.IO;
using System.Windows.Forms;

namespace BundleManager;

internal static class ImageImportDialog
{
    public static CompressionType? AskCompression(IWin32Window owner, string imagePath, PlatformType platform)
    {
        var (width, height, hasAlpha) = GameImage.InspectImage(imagePath);
        CompressionType compressed = !hasAlpha ? CompressionType.DXT1
            : platform == PlatformType.Remastered ? CompressionType.BC7 // Only Remastered's D3D11 renderer reads BC7
            : CompressionType.DXT5;
        long compressedSize = GameImage.TextureSize(compressed, width, height);
        long linearSize = GameImage.TextureSize(CompressionType.BGRA, width, height);
        bool compressible = width % 4 == 0 && height % 4 == 0; // GameImage.SetImage refuses other sizes

        TaskDialogCommandLinkButton compressedButton = new("Compressed (recommended)", compressible
            ? $"{compressed}{(hasAlpha ? ", keeps the transparency" : "")}. {Size(compressedSize)}."
            : "Needs a width and height that are multiples of 4. Resize the image to use it.")
        {
            Enabled = compressible
        };
        TaskDialogCommandLinkButton linear = new("High quality (linear)",
            $"Uncompressed, exact colours{(hasAlpha ? " and transparency" : "")}. {Size(linearSize)}, {linearSize / compressedSize}× the size.");

        TaskDialogPage page = new()
        {
            Caption = "Import Image",
            Heading = "How should this texture be stored?",
            Text = $"{Path.GetFileName(imagePath)}, {width}×{height}",
            Buttons = { compressedButton, linear, TaskDialogButton.Cancel },
            DefaultButton = compressible ? compressedButton : linear,
            AllowCancel = true,
            Footnote = new TaskDialogFootnote("High quality textures can quickly exhaust the game's memory budget.") { Icon = TaskDialogIcon.Warning }
        };

        TaskDialogButton result = owner == null ? TaskDialog.ShowDialog(page) : TaskDialog.ShowDialog(owner, page);
        return result == compressedButton ? compressed : result == linear ? CompressionType.BGRA : null;
    }

    private static string Size(long bytes) =>
        bytes >= 1 << 20 ? $"{bytes / (double)(1 << 20):0.#} MB" : $"{bytes / 1024.0:0.#} KB";
}
