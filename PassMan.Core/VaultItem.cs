namespace PassMan.Core
{
    using System;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public abstract class VaultItem : INotifyPropertyChanged, INotifyCollectionChanged, IDisposable
    {
        private bool disposedValue;

        public abstract string Type { get; }

        public event PropertyChangedEventHandler? PropertyChanged;

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public abstract int WriteTo(Span<byte> destination);

        public abstract int ReadFrom(Span<byte> source);

        public abstract int ComputeSize();

        protected void NotifyPropertyChanged([CallerMemberName] string name = "")
        {
            PropertyChanged?.Invoke(this, new(name));
        }

        protected void NotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            CollectionChanged?.Invoke(this, args);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    if (PropertyChanged != null)
                        foreach (Delegate d in PropertyChanged.GetInvocationList())
                        {
                            PropertyChanged -= (PropertyChangedEventHandler)d;
                        }
                    if (CollectionChanged != null)
                        foreach (Delegate d in CollectionChanged.GetInvocationList())
                        {
                            CollectionChanged -= (NotifyCollectionChangedEventHandler)d;
                        }
                }

                disposedValue = true;
            }
        }

        ~VaultItem()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}