namespace FileSystemExplorer.Events;

public class FolderSelectedEventArgs(string folderPath) : EventArgs
{
    public string FolderPath { get; set; } = folderPath;
}

public delegate void FolderSelectedEventHandler(object sender, FolderSelectedEventArgs e);