using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;

namespace BundleManager.Models;

public partial class FileSystemEntryNode : ObservableObject
{
	[ObservableProperty]
	private FileSystemInfo info;
	public bool IsDirectory { get => (Info.Attributes & FileAttributes.Directory) != 0; }
	public ObservableCollection<FileSystemEntryNode> Entries { get; } = [];
	public string ShortName { get => Info.Name.Length > 35 ? Info.Name.Substring(0, 32) + "..." : Info.Name; }

	public FileSystemEntryNode(string path)
	{
		bool isDirectory = (File.GetAttributes(path) & FileAttributes.Directory) != 0;
		if (isDirectory)
		{
			DirectoryInfo info = new(path);
			Info = info;
			foreach (var directoryInfo in info.GetDirectories())
				Entries.Add(new FileSystemEntryNode(directoryInfo.FullName));
			foreach (var fileInfo in info.GetFiles())
				Entries.Add(new FileSystemEntryNode(fileInfo.FullName));
		}
		else
		{
			FileInfo info = new(path);
			Info = info;
		}
	}
}
