namespace PassMan.ViewModel
{
    using PassMan.Core.Commands;
    using System.Security;
    using System.Security.Cryptography;
    using System.Windows;

    public class StartupViewModel : ViewModelBase
    {
        private readonly MainViewModel mainViewModel;

        public StartupViewModel(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;

            EnterPasswordCommand = new(EnterPassword);
        }

        public RelayCommand<Wpf.Ui.Controls.PasswordBox> EnterPasswordCommand { get; }

        private void EnterPassword(Wpf.Ui.Controls.PasswordBox pw)
        {
            SecureString str;

            unsafe
            {
                fixed (char* ptr = pw.Password)
                {
                    str = new(ptr, pw.Password.Length);
                }
            }
            pw.Clear();

            if (mainViewModel.StorageProvider.Exists())
            {
                try
                {
                    mainViewModel.SetVault(new(mainViewModel.StorageProvider, str.Copy()));
                    mainViewModel.Vault.Load();
                    str.Clear();
                    mainViewModel.PasswordDialogEnabled = false;
                    mainViewModel.PasswordDialogVisibility = Visibility.Collapsed;
                    mainViewModel.MenuVisibility = Visibility.Visible;
                    mainViewModel.LockVaultButtonVisibility = Visibility.Visible;
                }
                catch (CryptographicException)
                {
                    mainViewModel.PasswordDialogEnabled = true;
                    mainViewModel.PasswordDialogVisibility = Visibility.Visible;
                    mainViewModel.MenuVisibility = Visibility.Collapsed;
                    mainViewModel.LockVaultButtonVisibility = Visibility.Collapsed;
                }
            }
            else
            {
                mainViewModel.SetVault(new(mainViewModel.StorageProvider, str.Copy()));
                str.Clear();
                mainViewModel.Vault.Save();
                mainViewModel.PasswordDialogEnabled = false;
                mainViewModel.PasswordDialogVisibility = Visibility.Collapsed;
                mainViewModel.MenuVisibility = Visibility.Visible;
                mainViewModel.LockVaultButtonVisibility = Visibility.Visible;
            }
        }
    }
}