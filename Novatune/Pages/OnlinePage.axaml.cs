using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Novatune.Models;
using Novatune.ViewModels;

namespace Novatune.Pages
{
    public partial class OnlinePage : UserControl
    {
        public OnlineViewModel? ViewModel { get; private set; }
        
        public OnlinePage()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            
            if ( App.MainWindow is MainWindow mainWindow && mainWindow.GlobalMediaPlayerVM is not null )
            {
                ViewModel = new OnlineViewModel(mainWindow.GlobalMediaPlayerVM);
                this.DataContext = this;
            }
        }

        private void SearchTextBox_KeyDown(object? sender, KeyEventArgs e)
        {
            if ( e.Key == Key.Enter && ViewModel?.SearchCommand.CanExecute(null) == true )
            {
                ViewModel.SearchCommand.Execute(null);
            }
        }
    }
}
