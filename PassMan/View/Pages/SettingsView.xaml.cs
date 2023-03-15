namespace PassMan.View.Pages
{
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Navigation;

    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : UserControl
    {
        public SettingsView()
        {
            InitializeComponent();
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