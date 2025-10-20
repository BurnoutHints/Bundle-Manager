using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BundleManager.Services;

public class FileFolderService : IFileFolderService
{
	private readonly Window target;

	public FileFolderService(Window target)
	{
		this.target = target;
	}

	public async Task<IStorageFile?> OpenFileAsync(string title, IReadOnlyList<FilePickerFileType>? fileTypes = null, bool allowMultiple = false)
	{
		FilePickerOpenOptions options = new()
		{
			Title = title,
			AllowMultiple = allowMultiple,
			FileTypeFilter = fileTypes
		};
		var files = await target.StorageProvider.OpenFilePickerAsync(options);
		return files.Count > 0 ? files[0] : null;
	}

	public async Task<IStorageFile?> SaveFileAsync(string title)
	{
		FilePickerSaveOptions options = new()
		{
			Title = title,
			ShowOverwritePrompt = true
		};
		return await target.StorageProvider.SaveFilePickerAsync(options);
	}

	public async Task<IStorageFolder?> PickFolderAsync(string title, bool allowMultiple = false)
	{
		FolderPickerOpenOptions options = new()
		{
			Title = title,
			AllowMultiple = allowMultiple
		};
		var folders = await target.StorageProvider.OpenFolderPickerAsync(options);
		return folders.Count > 0 ? folders[0] : null;
	}
}
