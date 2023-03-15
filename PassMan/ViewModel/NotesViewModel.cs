namespace PassMan.ViewModel
{
    using PassMan.Core;
    using PassMan.Core.Commands;
    using System.ComponentModel;
    using System.Windows;

    public class NotesViewModel : ViewModelBase
    {
        private readonly MainViewModel mainViewModel;

        private Visibility currentNoteVisibility = Visibility.Collapsed;
        internal ICollectionView? notesFilterView;
        private Note? currentNote;

        public NotesViewModel(MainViewModel mainViewModel)
        {
            this.mainViewModel = mainViewModel;

            RemoveNoteCommand = new(RemoveNote, () => currentNote is not null);
            AddNoteCommand = new(AddNote, () => mainViewModel.Vault is not null);
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

        public RelayCommand AddNoteCommand { get; }

        public RelayCommand RemoveNoteCommand { get; }

        public void AddNote()
        {
            if (mainViewModel.Vault is not null)
            {
                mainViewModel.Vault.Add(new Note());
            }
        }

        public void RemoveNote()
        {
            if (MainViewModel.ConstructMessageBox("Do you really want to delete this note?", "Delete").ShowDialog() == true)
            {
                if (currentNote is not null)
                {
                    Note note = currentNote;
                    mainViewModel.Vault?.Remove(note);
                    note.Dispose();
                }
            }
        }
    }
}