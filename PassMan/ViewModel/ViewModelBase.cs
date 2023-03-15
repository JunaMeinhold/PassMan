namespace PassMan.ViewModel
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void NotifyPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new(name));
        }

        protected void SetAndNotifyPropertyChanged<T>(ref T target, T newValue, [CallerMemberName] string name = "")
        {
            target = newValue;
            PropertyChanged?.Invoke(this, new(name));
        }
    }
}