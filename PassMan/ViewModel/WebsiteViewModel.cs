namespace PassMan.ViewModel
{
    using PassMan.Core;
    using PassMan.Core.Commands;
    using System.ComponentModel;
    using System.Windows;

    public class WebsitesViewModel : ViewModelBase
    {
        private Website? currentWebsite;
        private Visibility currentWebsiteVisibility = Visibility.Collapsed;
        internal ICollectionView? websitesFilterView;
        private readonly MainViewModel mainViewModel;

        public WebsitesViewModel(MainViewModel mainViewModel)
        {
            RemoveWebsiteCommand = new(RemoveWebsite, () => currentWebsite is not null);
            AddWebsiteCommand = new(AddWebsite, () => mainViewModel.Vault is not null);
            RemoveAccountFromWebsiteCommand = new(RemoveAccountFromWebsite, x => currentWebsite is not null && x is not null);
            AddAccountToWebsiteCommand = new(AddAccountToWebsite, () => currentWebsite is not null);
            this.mainViewModel = mainViewModel;
        }

        public void Reset()
        {
            CurrentWebsiteVisibility = Visibility.Collapsed;
            CurrentWebsite = null;
            websitesFilterView = null;
        }

        public RelayCommand AddAccountToWebsiteCommand { get; }

        public RelayCommand<Account> RemoveAccountFromWebsiteCommand { get; }

        public RelayCommand AddWebsiteCommand { get; }

        public RelayCommand RemoveWebsiteCommand { get; }

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

        public void AddAccountToWebsite()
        {
            currentWebsite?.Accounts.Add(new());
        }

        public void RemoveAccountFromWebsite(Account account)
        {
            if (MainViewModel.ConstructMessageBox("Do you really want to delete this account?", "Delete").ShowDialog() == true)
            {
                currentWebsite?.Accounts.Remove(account);
                account.Dispose();
            }
        }

        public void AddWebsite()
        {
            Website website = new();
            if (mainViewModel.Vault is not null)
            {
                mainViewModel.Vault.Add(website);
                CurrentWebsite = website;
            }
        }

        public void RemoveWebsite()
        {
            if (MainViewModel.ConstructMessageBox("Do you really want to delete this website?", "Delete").ShowDialog() == true)
            {
                if (currentWebsite != null)
                {
                    Website website = currentWebsite;
                    mainViewModel.Vault?.Remove(website);
                    website.Dispose();
                }
            }
        }

        public void LockVault()
        {
            CurrentWebsite = null;
        }
    }
}