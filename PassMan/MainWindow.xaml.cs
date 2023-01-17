namespace PassMan
{
    using System.Windows;
    using System.Windows.Navigation;
    using Wpf.Ui.Controls;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : UiWindow
    {
        public MainWindow()
        {
            Wpf.Ui.Appearance.Accent.ApplySystemAccent();
            Instance = this;
            InitializeComponent();
        }

        public static MainWindow? Instance { get; private set; }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (sender is not MainWindow mainWindow)
                return;

            if (mainWindow.DataContext is not ViewModel.ViewModel viewModel)
                return;
            viewModel.LockVault();
            /*
            if (!viewModel.IsExiting)
            {
                e.Cancel = true;
                mainWindow.Hide();
                viewModel.IsHidden = true;
            }*/
        }

        private void RootFrame_LoadCompleted(object sender, NavigationEventArgs e)
        {
            UpdateFrameDataContext(sender);
        }

        private void RootFrame_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            UpdateFrameDataContext(sender);
        }

        private void UpdateFrameDataContext(object sender)
        {
            var content = RootFrame.Content as FrameworkElement;
            if (content == null)
                return;
            content.DataContext = RootFrame.DataContext;
        }
    }
}