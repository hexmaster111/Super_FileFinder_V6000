using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using Avalonia.Input;
using DriveScanner;
using ReactiveUI;

namespace GDriveCrawlerV3.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    public string AppName => "Super Ultra File Finder Pro Dx 3009 v1 Definitive Edition 2.0.0.0.0.0.0.1 beta 6";

    public MainWindowViewModel()
    {
        Drives = Directory.GetLogicalDrives();
        SelectedDrive = Drives.First();
        ReScanDriveCommand = ReactiveCommand.Create(ReScanDrive);
        SearchCommand = ReactiveCommand.Create(Search);
        OpenSelectedFileCommand = ReactiveCommand.Create(OpenSelectedFile);
        OpenContainingFolderCommand = ReactiveCommand.Create(OpenContainingFolder);
    }

    private void OpenContainingFolder()
    {
        if (SelectedFile is null) throw new Exception("Selected File is Null");
        if (SelectedFile.ContainingFolder is null) throw new Exception("Containing Folder is Null");
        ExploreFile(SelectedFile.Path);
    }

    public bool CanOpenThisFile => SelectedFile is not null;

    private void OpenSelectedFile()
    {
        if (SelectedFile is null) throw new Exception("Selected File is Null");
        OpenWithDefault(SelectedFile.Path);
    }

    private void ExploreFile(string filePath)
    {
        if (!File.Exists(filePath)) return;

        filePath = Path.GetFullPath(filePath);
        Process.Start("explorer.exe", $"/select,\"{filePath}\"");
    }

    private void OpenWithDefault(string path)
    {
        using var proc = new Process();
        proc.StartInfo.FileName = "explorer";
        proc.StartInfo.Arguments = "\"" + path + "\"";
        proc.Start();
    }

    private void Search()
    {
        if (_scanner?.IsReadyToSearch != true) return;

        IsSearching = true;
        _scanner.SearchAsync(SearchText).ContinueWith(o =>
        {
            FoundFiles = new ObservableCollection<FileItem>(o.Result);
            IsSearching = false;
        });
    }

    private void ReScanDrive()
    {
        if (string.IsNullOrEmpty(SelectedDrive)) return;
        if (_scanner is null) throw new Exception("Scanner is null!");
        IsCrawling = true;
        _scanner.ScanAsync().ContinueWith(o => { IsCrawling = false; });
    }

    public bool IsUiEnabled => !IsCrawling && !IsSearching;

    private string _selectedDrive = string.Empty;
    private CashingScanner? _scanner;
    private ObservableCollection<FileItem> _foundFiles = new();
    public string[] Drives { get; }

    private bool _isCrawling;

    public bool IsCrawling
    {
        get => _isCrawling;
        set
        {
            SetField(ref _isCrawling, value);
            this.RaisePropertyChanged(nameof(IsNotCrawling));
            this.RaisePropertyChanged(nameof(IsUiEnabled));
        }
    }

    public bool IsNotCrawling => !IsCrawling;


    private string _searchText = string.Empty;

    private bool _isSearching;

    public bool IsNotSearching => !IsSearching;

    public bool IsSearching
    {
        get => _isSearching;
        set
        {
            SetField(ref _isSearching, value);
            this.RaisePropertyChanged(nameof(IsNotSearching));
            this.RaisePropertyChanged(nameof(IsUiEnabled));
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (IsSearching) return;
            SetField(ref _searchText, value);
        }
    }


    public ObservableCollection<FileItem> FoundFiles
    {
        get => _foundFiles;
        set => SetField(ref _foundFiles, value);
    }

    public string SelectedDrive
    {
        get => _selectedDrive;
        set
        {
            if (IsCrawling)
            {
                return;
            }

            SetField(ref _selectedDrive, value);
            _scanner?.Dispose();
            _scanner = new CashingScanner(value);
            if (!_scanner.IsReadyToSearch)
            {
                IsCrawling = true;
                _scanner.ScanAsync().ContinueWith(o => { IsCrawling = false; });
            }

            FoundFiles.Clear();
        }
    }

    public ReactiveCommand<Unit, Unit> ReScanDriveCommand { get; }
    public ReactiveCommand<Unit, Unit> SearchCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenSelectedFileCommand { get; }
    public ReactiveCommand<Unit, Unit> OpenContainingFolderCommand { get; }

    private FileItem? _selectedFile;

    public FileItem? SelectedFile
    {
        get => _selectedFile;
        set
        {
            SetField(ref _selectedFile, value);
            this.RaisePropertyChanged(nameof(CanOpenThisFile));
        }
    }
}