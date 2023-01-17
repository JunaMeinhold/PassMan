namespace PassMan.ViewModel
{
    using Microsoft.Win32;
    using PassMan.Core;
    using PassMan.Core.Commands;
    using PassMan.Core.Importer;
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Security;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Threading;

    public class ViewModel : INotifyPropertyChanged
    {
        static ViewModel()
        {
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PassMan");
        }

        public ViewModel()
        {
            _dispatcher = Dispatcher.CurrentDispatcher;
            storageProvider = new GoogleDriveStorageProvider(RootPath);
            EnterPasswordCommand = new(EnterPassword);
            LockVaultCommand = new(LockVault);
            SaveVaultCommand = new(SaveVault);
            RemoveWebsiteCommand = new(RemoveWebsite, () => currentWebsite is not null);
            AddWebsiteCommand = new(AddWebsite, () => vault is not null);
            RemoveAccountFromWebsiteCommand = new(RemoveAccountFromWebsite, x => currentWebsite is not null && x is not null);
            AddAccountToWebsiteCommand = new(AddAccountToWebsite, () => currentWebsite is not null);
            RemoveAppCommand = new(RemoveApp, () => currentApp is not null);
            AddAppCommand = new(AddApp, () => vault is not null);
            RemoveAccountFromAppCommand = new(RemoveAccountFromApp, x => currentApp is not null && x is not null);
            AddAccountToAppCommand = new(AddAccountToApp, () => currentApp is not null);
            RemoveNoteCommand = new(RemoveNote, () => currentNote is not null);
            AddNoteCommand = new(AddNote, () => vault is not null);
            ExportFileCommand = new(ExportFile, () => currentFile is not null);
            RemoveFileCommand = new(RemoveFile, () => currentFile is not null);
            AddFileCommand = new(AddFile, () => vault is not null);
            OpenCommand = new(Open, () => IsHidden);
            CloseCommand = new(Close);
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
        }

        private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
        {
            vault?.Save();
        }

        private static Dispatcher? _dispatcher;

        public static readonly string RootPath;
        private IStorageProvider storageProvider;
        private Visibility menuVisibility = Visibility.Collapsed;
        private Visibility passwordVisibility = Visibility.Visible;
        private Visibility lockVaultButtonVisibility = Visibility.Collapsed;
        private Vault? vault;
        private Website? currentWebsite;
        private App? currentApp;
        private string? filter;
        private ICollectionView? websitesFilterView;
        private ICollectionView? appsFilterView;
        private ICollectionView? notesFilterView;
        private ICollectionView? filesFilterView;
        private Note? currentNote;
        private EncryptedFile? currentFile;
        private bool isHidden;
        private bool isExiting;
        private static bool isClearing;
        private static Task? cleanupTask;
        private static CancellationTokenSource? cancellationTokenSource;
        private Visibility currentAppVisibility = Visibility.Collapsed;
        private Visibility currentWebsiteVisibility = Visibility.Collapsed;
        private Visibility currentNoteVisibility = Visibility.Collapsed;
        private Visibility currentFileVisibility = Visibility.Collapsed;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new(name));
        }

        private static void RestartCleanupTask()
        {
            StopCleanupTask();
            StartCleanupTask();
        }

        private static void StartCleanupTask()
        {
            if (isClearing)
                return;
            isClearing = true;
            cancellationTokenSource = new CancellationTokenSource();

            cleanupTask = Task.Run(() =>
            {
                CancellationToken token = cancellationTokenSource.Token;
                long timeToTrigger = Stopwatch.GetTimestamp() + Stopwatch.Frequency * 10;

                while (Stopwatch.GetTimestamp() < timeToTrigger)
                {
                    if (token.IsCancellationRequested)
                    {
                        return;
                    }
                    Thread.Sleep(1);
                }

                _dispatcher?.Invoke(Clipboard.Clear);
                isClearing = false;
            });
        }

        private static void StopCleanupTask()
        {
            cancellationTokenSource?.Cancel();
            cleanupTask?.Wait();
            cleanupTask?.Dispose();
        }

        public bool IsExiting
        {
            get
            {
                return isExiting;
            }
            set
            {
                isExiting = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsHidden
        {
            get
            {
                return isHidden;
            }
            set
            {
                isHidden = value;
                NotifyPropertyChanged();
            }
        }

        public Vault? Vault
        {
            get => vault;
            set
            {
                vault = value;
                NotifyPropertyChanged();
                if (vault != null)
                {
                    websitesFilterView = CollectionViewSource.GetDefaultView(vault.Websites);
                    websitesFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((Website)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                    appsFilterView = CollectionViewSource.GetDefaultView(vault.Apps);
                    appsFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((App)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                    notesFilterView = CollectionViewSource.GetDefaultView(vault.Notes);
                    notesFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((Note)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                    filesFilterView = CollectionViewSource.GetDefaultView(vault.Files);
                    filesFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((EncryptedFile)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                }
                else
                {
                    websitesFilterView = null;
                    appsFilterView = null;
                    notesFilterView = null;
                    filesFilterView = null;
                }
            }
        }

        public Visibility MenuVisibility
        {
            get => menuVisibility;
            set
            {
                menuVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility PasswordDialogVisibility
        {
            get
            {
                return passwordVisibility;
            }
            set
            {
                passwordVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public Visibility LockVaultButtonVisibility
        {
            get
            {
                return lockVaultButtonVisibility;
            }
            set
            {
                lockVaultButtonVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public string? Filter
        {
            get
            {
                return filter;
            }
            set
            {
                filter = value;
                if (websitesFilterView != null)
                    websitesFilterView.Refresh();
                if (appsFilterView != null)
                    appsFilterView.Refresh();
                if (notesFilterView != null)
                    notesFilterView.Refresh();
                if (filesFilterView != null)
                    filesFilterView.Refresh();
                NotifyPropertyChanged();
            }
        }

        public Website? CurrentWebsite
        {
            get
            {
                return currentWebsite;
            }
            set
            {
                currentWebsite = value;
                CurrentWebsiteVisibility = value is null ? Visibility.Collapsed : Visibility.Visible;
                NotifyPropertyChanged();
            }
        }

        public Visibility CurrentWebsiteVisibility
        {
            get
            {
                return currentWebsiteVisibility;
            }
            set
            {
                currentWebsiteVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public App? CurrentApp
        {
            get
            {
                return currentApp;
            }
            set
            {
                currentApp = value;
                CurrentAppVisibility = value is null ? Visibility.Collapsed : Visibility.Visible;
                NotifyPropertyChanged();
            }
        }

        public Visibility CurrentAppVisibility
        {
            get
            {
                return currentAppVisibility;
            }
            set
            {
                currentAppVisibility = value;
                NotifyPropertyChanged();
            }
        }

        public Note? CurrentNote
        {
            get
            {
                return currentNote;
            }
            set
            {
                currentNote = value;
                CurrentNoteVisibility = value is null ? Visibility.Collapsed : Visibility.Visible;
                NotifyPropertyChanged();
            }
        }

        public Visibility CurrentNoteVisibility
        {
            get
            {
                return currentNoteVisibility;
            }
            set
            {
                currentNoteVisibility = value;
                NotifyPropertyChanged();
            }
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

        public RelayCommand CloseCommand { get; }

        public RelayCommand OpenCommand { get; }

        public RelayCommand AddFileCommand { get; }

        public RelayCommand RemoveFileCommand { get; }

        public RelayCommand ExportFileCommand { get; }

        public RelayCommand AddNoteCommand { get; }

        public RelayCommand RemoveNoteCommand { get; }

        public RelayCommand AddAccountToAppCommand { get; }

        public RelayCommand<Account> RemoveAccountFromAppCommand { get; }

        public RelayCommand AddAppCommand { get; }

        public RelayCommand RemoveAppCommand { get; }

        public RelayCommand AddAccountToWebsiteCommand { get; }

        public RelayCommand<Account> RemoveAccountFromWebsiteCommand { get; }

        public RelayCommand AddWebsiteCommand { get; }

        public RelayCommand RemoveWebsiteCommand { get; }

        public RelayCommand<PasswordBox, int> GeneratePasswordCommand { get; } = new(GeneratePassword);

        public RelayCommand<string> CopyStringCommand { get; } = new(CopyString);

        public RelayCommand<PasswordBox> CopySecureStringCommand { get; } = new(CopySecureString);

        public RelayCommand<SecureString> EnterPasswordCommand { get; }

        public RelayCommand LockVaultCommand { get; }

        public RelayCommand SaveVaultCommand { get; }

        public void Close()
        {
            IsExiting = true;
            MainWindow.Instance?.Close();
            StopCleanupTask();
            Environment.Exit(0);
        }

        public void Open()
        {
            MainWindow.Instance?.Show();
            IsHidden = false;
        }

        public void AddFile()
        {
            if (vault is not null)
            {
                OpenFileDialog openFileDialog = new();
                openFileDialog.CheckFileExists = true;
                openFileDialog.CheckPathExists = true;
                if (openFileDialog.ShowDialog() == true)
                {
                    var file = EncryptedFile.CreateAsync(storageProvider, openFileDialog.FileName, vault.GetSecureString()).Result;
                    vault.Add(file);
                }
            }
        }

        public void RemoveFile()
        {
            if (currentFile is not null)
            {
                EncryptedFile file = currentFile;
                vault?.Remove(file);
                file.Delete();
                file.Dispose();
            }
        }

        public void ExportFile()
        {
            if (currentFile is not null)
            {
                SaveFileDialog saveFileDialog = new();
                saveFileDialog.FileName = currentFile.Name;
                if (vault is not null && saveFileDialog.ShowDialog() == true && saveFileDialog.FileName is not null)
                    currentFile.Export(saveFileDialog.FileName, vault.GetSecureString());
            }
        }

        public void AddNote()
        {
            if (vault is not null)
            {
                vault.Add(new Note());
            }
        }

        public void RemoveNote()
        {
            if (currentNote is not null)
            {
                Note note = currentNote;
                vault?.Remove(note);
                note.Dispose();
            }
        }

        public void AddAccountToApp()
        {
            currentApp?.Accounts.Add(new());
        }

        public void RemoveAccountFromApp(Account account)
        {
            if (MessageBox.Show("Delete account from current app?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                currentApp?.Accounts.Remove(account);
                account.Dispose();
            }
        }

        public void AddApp()
        {
            App app = new();
            if (vault is not null)
            {
                vault.Add(app);
                CurrentApp = app;
            }
        }

        public void RemoveApp()
        {
            if (MessageBox.Show("Delete app and all accounts from vault?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (currentApp != null)
                {
                    App app = currentApp;
                    vault?.Remove(app);
                    app.Dispose();
                }
            }
        }

        public void AddAccountToWebsite()
        {
            currentWebsite?.Accounts.Add(new());
        }

        public void RemoveAccountFromWebsite(Account account)
        {
            if (MessageBox.Show("Delete account from Website?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                currentWebsite?.Accounts.Remove(account);
                account.Dispose();
            }
        }

        public void AddWebsite()
        {
            Website website = new();
            if (vault is not null)
            {
                vault.Add(website);
                CurrentWebsite = website;
            }
        }

        public void RemoveWebsite()
        {
            if (MessageBox.Show("Delete website and all accounts from vault?", "Delete", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                if (currentWebsite != null)
                {
                    Website website = currentWebsite;
                    vault?.Remove(website);
                    website.Dispose();
                }
            }
        }

        public static void GeneratePassword(PasswordBox box, int length)
        {
            box.Password = Extensions.GetRandomAlphanumericStringEx(length);
        }

        public static void CopyString(string str)
        {
            Clipboard.SetText(str);
            RestartCleanupTask();
        }

        public static void CopySecureString(PasswordBox str)
        {
            str.SecurePassword.Expose(x => Clipboard.SetText(x));
            RestartCleanupTask();
        }

        private void EnterPassword(SecureString str)
        {
            if (storageProvider.Exists())
            {
                try
                {
                    Vault = new(storageProvider, str.Copy());

                    Vault.Load();
                    str.Clear();
                    PasswordDialogVisibility = Visibility.Collapsed;
                    MenuVisibility = Visibility.Visible;
                    LockVaultButtonVisibility = Visibility.Visible;
                }
                catch (CryptographicException)
                {
                    PasswordDialogVisibility = Visibility.Visible;
                    MenuVisibility = Visibility.Collapsed;
                    LockVaultButtonVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                Vault = new(storageProvider, str.Copy());
                str.Clear();
                OpenFileDialog dialog = new();
                if (dialog.ShowDialog() == true)
                {
                    KasperskyTextfileImporter.Import(dialog.FileName, Vault);
                }
                Vault.Save();
                PasswordDialogVisibility = Visibility.Collapsed;
                MenuVisibility = Visibility.Visible;
                LockVaultButtonVisibility = Visibility.Visible;
            }
        }

        public void LockVault()
        {
            Vault? tmp = vault;
            tmp?.Save();
            Vault = null;
            Filter = null;
            CurrentApp = null;
            CurrentWebsite = null;
            PasswordDialogVisibility = Visibility.Visible;
            MenuVisibility = Visibility.Collapsed;
            LockVaultButtonVisibility = Visibility.Collapsed;
            tmp?.Dispose();
        }

        public void SaveVault()
        {
            vault?.Save();
        }
    }
}