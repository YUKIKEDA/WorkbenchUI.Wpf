using System.Windows.Input;

namespace WorkbenchUI.Wpf.Gallery
{
    /// <summary>Minimal ICommand implementation for Gallery demos.</summary>
    internal sealed class RelayCommand(Action execute) : ICommand
    {
        public event EventHandler? CanExecuteChanged
        {
            add { }
            remove { }
        }

        public bool CanExecute(object? parameter) => true;

        public void Execute(object? parameter) => execute();
    }
}
