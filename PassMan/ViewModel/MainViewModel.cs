namespace PassMan.ViewModel
{
    using PassMan.Core;
    using PassMan.Core.Commands;
    using PassMan.Model;
    using PassMan.View;
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Threading;
    using Wpf.Ui.Common;
    using Clipboard = Wpf.Ui.Common.Clipboard;
    using PasswordBox = System.Windows.Controls.PasswordBox;
    using RelayCommand = Core.Commands.RelayCommand;

    public class MainViewModel : ViewModelBase
    {
        static MainViewModel()
        {
        }

        public MainViewModel()
        {
            Startup = new(this);
            Websites = new(this);
            Apps = new(this);
            Notes = new(this);
            Files = new(this);
            Settings = new(this);
            _dispatcher = Dispatcher.CurrentDispatcher;

            switch (Config.Default.DataSource)
            {
                case DataSourceMode.Portable:
                    storageProvider = new LocalStorageProvider(Path.GetFullPath("./"));
                    break;

                case DataSourceMode.Appdata:
                    storageProvider = new LocalStorageProvider(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PassMan"));
                    break;

                case DataSourceMode.Cloud:
                    storageProvider = new GoogleDriveStorageProvider("./");
                    break;

                default:
                    throw new NotSupportedException("");
            }

            LockVaultCommand = new(LockVault);
            SaveVaultCommand = new(SaveVault);

            OpenCommand = new(Open, () => IsHidden);
            CloseCommand = new(Close);

            AppDomain.CurrentDomain.ProcessExit += ProcessExit;
        }

        private void ProcessExit(object? sender, EventArgs e)
        {
            vault?.Save();
        }

        private static Dispatcher? _dispatcher;

        private IStorageProvider storageProvider;
        private Visibility menuVisibility = Visibility.Collapsed;
        private Visibility passwordVisibility = Visibility.Visible;
        private Visibility lockVaultButtonVisibility = Visibility.Collapsed;
        private bool passwordDialogEnabled = true;
        private Vault? vault;

        private string? filter;

        private bool isHidden;
        private bool isExiting;
        private static bool isClearing;
        private static Task? cleanupTask;
        private static CancellationTokenSource? cancellationTokenSource;

        public bool IsExiting
        {
            get => isExiting;
            set => SetAndNotifyPropertyChanged(ref isExiting, value);
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

                _dispatcher?.Invoke(() => Clipboard.SetText(null), DispatcherPriority.Send);
                isClearing = false;
            });
        }

        private static void StopCleanupTask()
        {
            cancellationTokenSource?.Cancel();
            cleanupTask?.Wait();
            cleanupTask?.Dispose();
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

        public IStorageProvider StorageProvider => storageProvider;

        public Vault? Vault
        {
            get => vault;
        }

        public void SetVault(Vault? value)
        {
            vault = value;
            NotifyPropertyChanged();
            if (vault != null)
            {
                Websites.websitesFilterView = CollectionViewSource.GetDefaultView(vault.Websites);
                Websites.websitesFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((Website)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                Apps.appsFilterView = CollectionViewSource.GetDefaultView(vault.Apps);
                Apps.appsFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((App)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                Notes.notesFilterView = CollectionViewSource.GetDefaultView(vault.Notes);
                Notes.notesFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((Note)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
                Files.filesFilterView = CollectionViewSource.GetDefaultView(vault.Files);
                Files.filesFilterView.Filter = o => string.IsNullOrEmpty(filter) || (((EncryptedFile)o).Name?.Contains(filter, StringComparison.CurrentCultureIgnoreCase) ?? false);
            }
            else
            {
                Websites.websitesFilterView = null;
                Apps.appsFilterView = null;
                Notes.notesFilterView = null;
                Files.filesFilterView = null;
            }
        }

        public Visibility MenuVisibility
        {
            get => menuVisibility;
            set => SetAndNotifyPropertyChanged(ref menuVisibility, value);
        }

        public Visibility PasswordDialogVisibility
        {
            get => passwordVisibility;
            set => SetAndNotifyPropertyChanged(ref passwordVisibility, value);
        }

        public bool PasswordDialogEnabled
        {
            get => passwordDialogEnabled;
            set => SetAndNotifyPropertyChanged(ref passwordDialogEnabled, value);
        }

        public Visibility LockVaultButtonVisibility
        {
            get => lockVaultButtonVisibility;
            set => SetAndNotifyPropertyChanged(ref lockVaultButtonVisibility, value);
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
                Websites.websitesFilterView?.Refresh();
                Apps.appsFilterView?.Refresh();
                Notes.notesFilterView?.Refresh();
                Files.filesFilterView?.Refresh();
                NotifyPropertyChanged();
            }
        }

        public StartupViewModel Startup { get; }

        public WebsitesViewModel Websites { get; }

        public AppsViewModel Apps { get; }

        public NotesViewModel Notes { get; }

        public FilesViewModel Files { get; }

        public GeneratorViewModel Generator { get; } = new();

        public SettingsViewModel Settings { get; }

        public RelayCommand CloseCommand { get; }

        public RelayCommand OpenCommand { get; }

        public RelayCommand<string> CopyStringCommand { get; } = new(CopyString);

        public RelayCommand<PasswordBox> CopySecureStringCommand { get; } = new(CopySecureString);

        public RelayCommand LockVaultCommand { get; }

        public RelayCommand SaveVaultCommand { get; }

        public static Wpf.Ui.Controls.MessageBox ConstructMessageBox(string content, string title)
        {
            Wpf.Ui.Controls.MessageBox messageBox = new()
            {
                Content = content,
                Title = title,
                ButtonLeftName = "Delete",
                ButtonLeftAppearance = ControlAppearance.Danger,
                ButtonRightName = "Cancel",
                ButtonRightAppearance = ControlAppearance.Light
            };

            messageBox.ButtonLeftClick += (s, e) =>
            {
                if (s is Wpf.Ui.Controls.MessageBox box)
                {
                    box.DialogResult = true;
                    box.Close();
                }
            };

            messageBox.ButtonRightClick += (s, e) =>
            {
                if (s is Wpf.Ui.Controls.MessageBox box)
                {
                    box.DialogResult = false;
                    box.Close();
                }
            };

            return messageBox;
        }

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

        public static void CopyString(string str)
        {
            Clipboard.SetText(str);
            RestartCleanupTask();
        }

        public static void CopySecureString(PasswordBox str)
        {
            str.SecurePassword.Expose(Clipboard.SetText);
            RestartCleanupTask();
        }

        public void LockVault()
        {
            Vault? tmp = vault;
            tmp?.Save();
            SetVault(null);
            Filter = null;
            Websites.Reset();
            Apps.Reset();
            Notes.Reset();
            Files.Reset();
            PasswordDialogVisibility = Visibility.Visible;
            PasswordDialogEnabled = true;
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