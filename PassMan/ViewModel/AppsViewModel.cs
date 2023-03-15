namespace PassMan.ViewModel
{
    using PassMan.Core;
    using PassMan.Core.Commands;
    using System.ComponentModel;
    using System.Windows;

    public class AppsViewModel : ViewModelBase
    {
        private Visibility currentAppVisibility = Visibility.Collapsed;
        private App? currentApp;
        internal ICollectionView? appsFilterView;
        private readonly MainViewModel mainViewModel;

        public AppsViewModel(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;
            RemoveAppCommand = new(RemoveApp, () => currentApp is not null);
            AddAppCommand = new(AddApp, () => mainViewModel.Vault is not null);
            RemoveAccountFromAppCommand = new(RemoveAccountFromApp, x => currentApp is not null && x is not null);
            AddAccountToAppCommand = new(AddAccountToApp, () => currentApp is not null);
        }

        public void Reset()
        {
            CurrentAppVisibility = Visibility.Collapsed;
            CurrentApp = null;
            appsFilterView = null;
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

        public RelayCommand AddAccountToAppCommand { get; }

        public RelayCommand<Account> RemoveAccountFromAppCommand { get; }

        public RelayCommand AddAppCommand { get; }

        public RelayCommand RemoveAppCommand { get; }

        public void AddAccountToApp()
        {
            currentApp?.Accounts.Add(new());
        }

        public void RemoveAccountFromApp(Account account)
        {
            if (MainViewModel.ConstructMessageBox("Do you really want to delete this account?", "Delete").ShowDialog() == true)
            {
                currentApp?.Accounts.Remove(account);
                account.Dispose();
            }
        }

        public void AddApp()
        {
            App app = new();
            if (mainViewModel.Vault is not null)
            {
                mainViewModel.Vault.Add(app);
                CurrentApp = app;
            }
        }

        public void RemoveApp()
        {
            if (MainViewModel.ConstructMessageBox("Do you really want to delete this application?", "Delete").ShowDialog() == true)
            {
                if (currentApp != null)
                {
                    App app = currentApp;
                    mainViewModel.Vault?.Remove(app);
                    app.Dispose();
                }
            }
        }
    }
}