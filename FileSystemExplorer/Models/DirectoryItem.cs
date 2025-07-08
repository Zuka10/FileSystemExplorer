using System.Collections.ObjectModel;

namespace FileSystemExplorer.Models;

public class DirectoryItem : FileItem
{
    public ObservableCollection<DirectoryItem> Children { get; set; }
    public bool IsExpanded { get; set; }
    public bool HasUnrealizedChildren { get; set; }

    public DirectoryItem()
    {
        Children = [];
        IsDirectory = true;
    }
}