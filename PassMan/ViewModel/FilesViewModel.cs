namespace PassMan.ViewModel
{
    using Microsoft.Win32;
    using PassMan.Core;
    using PassMan.Core.Commands;
    using System.ComponentModel;
    using System.Windows;

    public class FilesViewModel : ViewModelBase
    {
        private readonly MainViewModel mainViewModel;

        private Visibility currentFileVisibility = Visibility.Collapsed;
        internal ICollectionView? filesFilterView;
        private EncryptedFile? currentFile;

        public FilesViewModel(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;

            ExportFileCommand = new(ExportFile, () => currentFile is not null);
            RemoveFileCommand = new(RemoveFile, () => currentFile is not null);
            AddFileCommand = new(AddFile, () => mainViewModel.Vault is not null);
        }

        public EncryptedFile? CurrentFile
        {
            get
            {
                return currentFile;
            }
            set
            {
                currentFile = value;
                CurrentFileVisibility = value is null ? Visibility.Collapsed : Visibility.Visible;
                NotifyPropertyChanged();
            }
        }

        public Visibility CurrentFileVisibility
        {
            get
            {
                return currentFileVisibility;
            }
            set
            {
                currentFileVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public RelayCommand AddFileCommand { get; }

        public RelayCommand RemoveFileCommand { get; }

        public RelayCommand ExportFileCommand { get; }

        public void AddFile()
        {
            if (mainViewModel.Vault is not null)
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                if (openFileDialog.ShowDialog() == true)
                {
                    var file = EncryptedFile.CreateAsync(mainViewModel.StorageProvider, openFileDialog.FileName, mainViewModel.Vault.GetPassword()).Result;
                    mainViewModel.Vault.Add(file);
                }
            }
        }

        public void RemoveFile()
        {
            if (MainViewModel.ConstructMessageBox("Do you really want to delete this file?", "Delete").ShowDialog() == true)
            {
                if (currentFile is not null)
                {
                    EncryptedFile file = currentFile;
                    mainViewModel.Vault?.Remove(file);
                    file.Delete();
                    file.Dispose();
                }
            }
        }

        public void ExportFile()
        {
            if (currentFile is not null)
            {
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.FileName = currentFile.Name;
                if (mainViewModel.Vault is not null && saveFileDialog.ShowDialog() == true && saveFileDialog.FileName is not null)
                    currentFile.Export(saveFileDialog.FileName, mainViewModel.Vault.GetPassword());
            }
        }
    }
}