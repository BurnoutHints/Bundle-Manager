using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BundleManager.Services;

public interface IFileFolderService
{
	Task<IStorageFile?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypes = null, bool allowMultiple = false);
	Task<IStorageFile?> SaveFileAsync(string title);
	
	Task<IStorageFolder?> PickFolderAsync(string title, bool allowMultiple = false);
}
