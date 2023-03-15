namespace PassMan.ViewModel
{
    using PassMan.Core.Commands;
    using PassMan.Model;
    using Wpf.Ui.Controls;

    public class SettingsViewModel : ViewModelBase
    {
        private readonly MainViewModel viewModel;
        private DataSourceMode dataSource = Config.Default.DataSource;

        public SettingsViewModel(MainViewModel viewModel)
        {
            ChangePasswordCommand = new(ChangePassword);
            ApplyDataSourceCommand = new(ApplyDataSource);
            this.viewModel = viewModel;
        }

        public DataSourceMode DataSource
        {
            get => dataSource;
            set => SetAndNotifyPropertyChanged(ref dataSource, value);
        }

        public RelayCommand<PasswordBox, PasswordBox, PasswordBox> ChangePasswordCommand { get; }

        public RelayCommand ApplyDataSourceCommand { get; }

        public void ChangePassword(PasswordBox oldPw, PasswordBox newPw, PasswordBox repeatPw)
        {
            if (viewModel.Vault == null)
                return;

            var vault = viewModel.Vault;

            if (!vault.GetPassword().SecureStringEqual(oldPw.Password))
            {
                return;
            }

            if (newPw.Password != repeatPw.Password)
            {
                return;
            }

            vault.SetPassword(newPw.Password.ConvertToSecureString());
            oldPw.Clear();
            newPw.Clear();
            repeatPw.Clear();
        }

        public void ApplyDataSource()
        {
            Config.Default.DataSource = dataSource;
            Config.Default.Save();
        }
    }
}