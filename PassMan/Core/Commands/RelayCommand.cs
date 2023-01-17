namespace PassMan.Core.Commands
{
    using System;
    using System.Windows.Input;

    public class RelayCommand : ICommand
    {
        private readonly Action _execute;

        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action execute)
        {
            _execute = execute;
            _canExecute = () => true;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute();
        }

        public void Execute(object? parameter)
        {
            _execute();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;

        private readonly Func<T, bool> _canExecute;

        public RelayCommand(Action<T> execute, Func<T, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<T> execute)
        {
            _execute = execute;
            _canExecute = t => true;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is T t)
            {
                return _canExecute(t);
            }
            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is T t)
            {
                _execute(t);
            }
        }
    }

    public class RelayCommand<T1, T2> : ICommand
    {
        private readonly Action<T1, T2> _execute;

        private readonly Func<T1, T2, bool> _canExecute;

        public RelayCommand(Action<T1, T2> execute, Func<T1, T2, bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public RelayCommand(Action<T1, T2> execute)
        {
            _execute = execute;
            _canExecute = (x, y) => true;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        public bool CanExecute(object? parameter)
        {
            if (parameter is object[] objs)
            {
                if (objs.Length == 2)
                {
                    if (objs[0] is not T1 t1)
                    {
                        return false;
                    }
                    if (objs[1] is not T2 t2)
                    {
                        return false;
                    }
                    return _canExecute(t1, t2);
                }
            }

            return false;
        }

        public void Execute(object? parameter)
        {
            if (parameter is object[] objs)
            {
                if (objs.Length == 2)
                {
                    if (objs[0] is not T1 t1)
                    {
                        return;
                    }
                    if (objs[1] is not T2 t2)
                    {
                        return;
                    }
                    _execute(t1, t2);
                }
            }
        }
    }
}