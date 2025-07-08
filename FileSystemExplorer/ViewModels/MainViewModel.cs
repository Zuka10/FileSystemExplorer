using FileSystemExplorer.Events;
using FileSystemExplorer.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using System.Windows;

namespace FileSystemExplorer.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private ObservableCollection<DirectoryItem>? _directoryTree;
    private ObservableCollection<FileItem>? _filesList;
    private string? _currentPath;
    private FileItem? _selectedFile;
    private string? _statusMessage;
    private bool _isLoading;

    public ObservableCollection<DirectoryItem>? DirectoryTree
    {
        get => _directoryTree;
        set
        {
            _directoryTree = value;
            OnPropertyChanged(nameof(DirectoryTree));
        }
    }

    public ObservableCollection<FileItem>? FilesList
    {
        get => _filesList;
        set
        {
            _filesList = value;
            OnPropertyChanged(nameof(FilesList));
        }
    }

    public string? CurrentPath
    {
        get => _currentPath;
        set
        {
            _currentPath = value;
            OnPropertyChanged(nameof(CurrentPath));
        }
    }

    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            _selectedFile = value;
            OnPropertyChanged(nameof(SelectedFile));
        }
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        set
        {
            _statusMessage = value;
            OnPropertyChanged(nameof(StatusMessage));
        }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public ICommand? NavigateUpCommand { get; }
    public ICommand? RefreshCommand { get; }
    public ICommand? CreateFolderCommand { get; }
    public ICommand? DeleteItemCommand { get; }
    public ICommand? RenameItemCommand { get; }
    public ICommand? CopyItemCommand { get; }
    public ICommand? PasteItemCommand { get; }
    public ICommand? OpenItemCommand { get; }
    public ICommand? TreeItemSelectedCommand { get; }

    private FileItem? _copiedItem;
    public event FolderSelectedEventHandler? FolderSelected;

    public MainViewModel()
    {
        DirectoryTree = [];
        FilesList = [];

        NavigateUpCommand = new RelayCommand(NavigateUp, CanNavigateUp);
        RefreshCommand = new RelayCommand(Refresh);
        CreateFolderCommand = new RelayCommand(CreateFolder);
        DeleteItemCommand = new RelayCommand(DeleteItem, CanDeleteItem);
        RenameItemCommand = new RelayCommand(RenameItem, CanRenameItem);
        CopyItemCommand = new RelayCommand(CopyItem, CanCopyItem);
        PasteItemCommand = new RelayCommand(PasteItem, CanPasteItem);
        OpenItemCommand = new RelayCommand(OpenItem);
        TreeItemSelectedCommand = new RelayCommand(OnTreeItemSelected);

        LoadDrives();
    }

    private void LoadDrives()
    {
        try
        {
            IsLoading = true;

            if (DirectoryTree == null)
            {
                DirectoryTree = [];
            }
            else
            {
                DirectoryTree.Clear();
            }

            var drives = DriveInfo.GetDrives().Where(d => d.IsReady);
            foreach (var drive in drives)
            {
                var driveItem = new DirectoryItem
                {
                    Name = $"{drive.Name} ({drive.DriveType})",
                    FullPath = drive.RootDirectory.FullName,
                    HasUnrealizedChildren = HasSubDirectories(drive.RootDirectory.FullName)
                };
                DirectoryTree.Add(driveItem);
            }

            StatusMessage = "Drives loaded successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading drives: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static bool HasSubDirectories(string path)
    {
        try
        {
            return Directory.GetDirectories(path).Length != 0;
        }
        catch
        {
            return false;
        }
    }

    private void LoadDirectoryChildren(DirectoryItem parent)
    {
        try
        {
            if (parent.HasUnrealizedChildren)
            {
                parent.Children.Clear();
                var directories = Directory.GetDirectories(parent.FullPath);

                foreach (var dir in directories)
                {
                    var dirInfo = new DirectoryInfo(dir);
                    var child = new DirectoryItem
                    {
                        Name = dirInfo.Name,
                        FullPath = dirInfo.FullName,
                        HasUnrealizedChildren = HasSubDirectories(dirInfo.FullName),
                        LastModified = dirInfo.LastWriteTime
                    };
                    parent.Children.Add(child);
                }

                parent.HasUnrealizedChildren = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading directory: {ex.Message}";
        }
    }

    private void LoadFiles(string path)
    {
        try
        {
            IsLoading = true;

            if (FilesList == null)
            {
                FilesList = [];
            }
            else
            {
                FilesList.Clear();
            }

            CurrentPath = path;

            var directory = new DirectoryInfo(path);

            // Add directories
            foreach (var dir in directory.GetDirectories())
            {
                FilesList.Add(new FileItem
                {
                    Name = dir.Name,
                    FullPath = dir.FullName,
                    LastModified = dir.LastWriteTime,
                    IsDirectory = true
                });
            }

            // Add files
            foreach (var file in directory.GetFiles())
            {
                FilesList.Add(new FileItem
                {
                    Name = file.Name,
                    FullPath = file.FullName,
                    Size = file.Length,
                    LastModified = file.LastWriteTime,
                    IsDirectory = false
                });
            }

            StatusMessage = $"Loaded {FilesList.Count} items";
            OnFolderSelected(path);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error loading files: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void OnTreeItemSelected(object parameter)
    {
        if (parameter is DirectoryItem selectedItem)
        {
            if (selectedItem.HasUnrealizedChildren)
            {
                LoadDirectoryChildren(selectedItem);
            }
            LoadFiles(selectedItem.FullPath);
        }
    }

    private void NavigateUp(object parameter)
    {
        if (!string.IsNullOrEmpty(CurrentPath))
        {
            var parent = Directory.GetParent(CurrentPath);
            if (parent != null)
            {
                LoadFiles(parent.FullName);
            }
        }
    }

    private bool CanNavigateUp(object parameter)
    {
        return !string.IsNullOrEmpty(CurrentPath) && Directory.GetParent(CurrentPath) != null;
    }

    private void Refresh(object? parameter)
    {
        if (!string.IsNullOrEmpty(CurrentPath))
        {
            LoadFiles(CurrentPath);
        }
        else
        {
            LoadDrives();
        }
    }

    private void CreateFolder(object parameter)
    {
        if (string.IsNullOrEmpty(CurrentPath)) return;

        var folderName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter folder name:", "Create Folder", "New Folder");

        if (!string.IsNullOrEmpty(folderName))
        {
            try
            {
                var newFolderPath = Path.Combine(CurrentPath, folderName);
                Directory.CreateDirectory(newFolderPath);
                Refresh(null);
                StatusMessage = $"Folder '{folderName}' created successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error creating folder: {ex.Message}";
            }
        }
    }

    private void DeleteItem(object parameter)
    {
        if (SelectedFile == null) return;

        var result = MessageBox.Show(
            $"Are you sure you want to delete '{SelectedFile.Name}'?",
            "Confirm Delete",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            try
            {
                string fileName = SelectedFile.Name;
                if (SelectedFile.IsDirectory)
                {
                    Directory.Delete(SelectedFile.FullPath, true);
                }
                else
                {
                    File.Delete(SelectedFile.FullPath);
                }

                Refresh(null);
                StatusMessage = $"'{fileName}' deleted successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error deleting item: {ex.Message}";
            }
        }
    }

    private bool CanDeleteItem(object parameter)
    {
        return SelectedFile != null;
    }

    private void RenameItem(object parameter)
    {
        if (SelectedFile == null) return;

        var newName = Microsoft.VisualBasic.Interaction.InputBox(
            "Enter new name:", "Rename Item", SelectedFile.Name);

        if (!string.IsNullOrEmpty(newName) && newName != SelectedFile.Name)
        {
            try
            {
                var newPath = Path.Combine(Path.GetDirectoryName(SelectedFile.FullPath)!, newName);

                if (SelectedFile.IsDirectory)
                {
                    Directory.Move(SelectedFile.FullPath, newPath);
                }
                else
                {
                    File.Move(SelectedFile.FullPath, newPath);
                }

                Refresh(null);
                StatusMessage = $"Item renamed to '{newName}' successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error renaming item: {ex.Message}";
            }
        }
    }

    private bool CanRenameItem(object parameter)
    {
        return SelectedFile != null;
    }

    private void CopyItem(object parameter)
    {
        if (SelectedFile != null)
        {
            _copiedItem = SelectedFile;
            StatusMessage = $"'{SelectedFile.Name}' copied to clipboard";
        }
    }

    private bool CanCopyItem(object parameter)
    {
        return SelectedFile != null;
    }

    private void PasteItem(object parameter)
    {
        if (_copiedItem == null || string.IsNullOrEmpty(CurrentPath)) return;

        try
        {
            var destinationPath = Path.Combine(CurrentPath, _copiedItem.Name);

            if (_copiedItem.IsDirectory)
            {
                CopyDirectory(_copiedItem.FullPath, destinationPath);
            }
            else
            {
                File.Copy(_copiedItem.FullPath, destinationPath, true);
            }

            Refresh(null);
            StatusMessage = $"'{_copiedItem.Name}' pasted successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error pasting item: {ex.Message}";
        }
    }

    private bool CanPasteItem(object parameter)
    {
        return _copiedItem != null && !string.IsNullOrEmpty(CurrentPath);
    }

    private void CopyDirectory(string sourceDir, string destDir)
    {
        Directory.CreateDirectory(destDir);

        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            var destSubDir = Path.Combine(destDir, Path.GetFileName(dir));
            CopyDirectory(dir, destSubDir);
        }
    }

    private void OpenItem(object parameter)
    {
        if (parameter is FileItem item)
        {
            if (item.IsDirectory)
            {
                LoadFiles(item.FullPath);
            }
            else
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = item.FullPath,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Error opening file: {ex.Message}";
                }
            }
        }
    }

    protected virtual void OnFolderSelected(string folderPath)
    {
        FolderSelected?.Invoke(this, new FolderSelectedEventArgs(folderPath));
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected virtual void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
