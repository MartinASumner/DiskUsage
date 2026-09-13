using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using DiskUsage.Helpers;
using DiskUsage.Models;

namespace DiskUsage.ViewModels
{
    public partial class FileSystemItemViewModel : ObservableObject
    {
        private readonly FileSystemItem _model;
        private FileSystemItemViewModel? _parent;
        private ObservableCollection<FileSystemItemViewModel>? _directoryChildren;
        private ObservableCollection<FileSystemItemViewModel>? _allChildren;

        [ObservableProperty]
        private bool _isExpanded;

        [ObservableProperty]
        private bool _isSelected;

        public FileSystemItemViewModel(FileSystemItem model, FileSystemItemViewModel? parent = null)
        {
            _model = model ?? throw new ArgumentNullException(nameof(model));
            _parent = parent;
        }

        public FileSystemItem Model => _model;
        public string Name => _model.Name;
        public string FullPath => _model.FullPath;
        public FileSystemItemType ItemType => _model.ItemType;
        public long Size => _model.Size;
        public int FileCount => _model.FileCount;
        public int DirectoryCount => _model.DirectoryCount;
        public DateTime LastModified => _model.LastModified;
        public string Extension => _model.Extension;
        public bool HasAccessError => _model.HasAccessError;
        public string? ErrorMessage => _model.ErrorMessage;

        public bool IsDirectory => _model.IsDirectory;
        public bool IsFile => _model.IsFile;

        public string FormattedSize => ByteSizeFormatter.Format(_model.Size);

        public FileSystemItemViewModel? Parent
        {
            get => _parent;
            internal set => SetProperty(ref _parent, value);
        }

        public double PercentOfParent
        {
            get
            {
                if (_parent == null || _parent.Size == 0)
                {
                    return 100.0;
                }
                return Math.Clamp((double)Size / _parent.Size * 100.0, 0.0, 100.0);
            }
        }

        public string PercentOfParentText => $"{PercentOfParent:F1}%";

        /// <summary>
        /// Only directory children (useful for the Left TreeView)
        /// </summary>
        public ObservableCollection<FileSystemItemViewModel> DirectoryChildren
        {
            get
            {
                if (_directoryChildren == null)
                {
                    _directoryChildren = new ObservableCollection<FileSystemItemViewModel>();
                    foreach (var child in _model.Children)
                    {
                        if (child.IsDirectory)
                        {
                            _directoryChildren.Add(new FileSystemItemViewModel(child, this));
                        }
                    }
                }
                return _directoryChildren;
            }
        }

        /// <summary>
        /// All direct children (folders + files) for Details Grid
        /// </summary>
        public ObservableCollection<FileSystemItemViewModel> AllChildren
        {
            get
            {
                if (_allChildren == null)
                {
                    _allChildren = new ObservableCollection<FileSystemItemViewModel>();
                    foreach (var child in _model.Children)
                    {
                        _allChildren.Add(new FileSystemItemViewModel(child, this));
                    }
                }
                return _allChildren;
            }
        }
    }
}
