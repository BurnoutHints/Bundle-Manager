using BurnoutImage;
using System.IO;
using System.Windows.Forms;

namespace BundleManager
{
    internal static class ImageImportDialog
    {
        public static CompressionType? AskCompression(IWin32Window owner, string imagePath)
        {
            var (width, height, hasAlpha) = GameImage.InspectImage(imagePath);
            CompressionType compressed = hasAlpha ? CompressionType.DXT5 : CompressionType.DXT1;
            long compressedSize = (long)((width + 3) / 4) * ((height + 3) / 4) * (hasAlpha ? 16 : 8);
            long linearSize = (long)width * height * 4;

            TaskDialogCommandLinkButton compressedButton = new("Compressed (recommended)",
                $"{compressed}{(hasAlpha ? ", keeps the transparency" : "")}. {Size(compressedSize)}.");
            TaskDialogCommandLinkButton linear = new("High quality (linear)",
                $"Uncompressed, exact colours{(hasAlpha ? " and transparency" : "")}. {Size(linearSize)}, {linearSize / compressedSize}× the size.");

            TaskDialogPage page = new()
            {
                Caption = "Import Image",
                Heading = "How should this texture be stored?",
                Text = $"{Path.GetFileName(imagePath)}, {width}×{height}",
                Buttons = { compressedButton, linear, TaskDialogButton.Cancel },
                AllowCancel = true,
                Footnote = new TaskDialogFootnote("High quality textures can quickly exhaust the game's memory budget.") { Icon = TaskDialogIcon.Warning }
            };

            TaskDialogButton result = owner == null ? TaskDialog.ShowDialog(page) : TaskDialog.ShowDialog(owner, page);
            return result == compressedButton ? compressed : result == linear ? CompressionType.BGRA : null;
        }

        private static string Size(long bytes) =>
            bytes >= 1 << 20 ? $"{bytes / (double)(1 << 20):0.#} MB" : $"{bytes / 1024.0:0.#} KB";
    }
}
