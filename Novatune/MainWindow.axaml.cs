using Avalonia.Controls;
using Novatune.Controls;
using Novatune.ViewModels;

namespace Novatune;

public partial class MainWindow : Window
{
    public MediaPlayerViewModel? GlobalMediaPlayerVM { get; private set; }
    public MediaControlsView? GlobalMediaControlsPublic => this.GlobalMediaControls;
    
    public MainWindow()
    {
        InitializeComponent();
        
        var rootNavigationView = this.FindControl<NavigationView>("RootNavigationView");
        var contentFrame = this.FindControl<Frame>("ContentFrame");
        var globalMediaControls = this.FindControl<MediaControlsView>("GlobalMediaControls");
        
        if (globalMediaControls is null)
        {
            return;
        }
        
        this.GlobalMediaControls = globalMediaControls;
        GlobalMediaPlayerVM = new();
        this.GlobalMediaControls.Initialize(GlobalMediaPlayerVM);
        
        this.MinHeight = 600;
        this.MinWidth = 1000;
        this.Title = "Novatune";
        
        // Navigate to HomePage initially
        if (contentFrame is not null)
        {
            contentFrame.Navigate(new Pages.HomePage());
        }
        
        if (rootNavigationView is not null)
        {
            rootNavigationView.SelectionChanged += RootNavigationView_SelectionChanged;
        }
    }
    
    private void RootNavigationView_SelectionChanged(object? sender, NavigationViewSelectionChangedEventArgs e)
    {
        var contentFrame = this.FindControl<Frame>("ContentFrame");
        if (contentFrame is null) return;
        
        if (e.IsSettingsSelected)
        {
            contentFrame.Navigate(new Pages.SettingsPage());
        }
        else if (e.SelectedItemContainer is NavigationViewItem selectedItem)
        {
            string? selectedPage = selectedItem.Tag?.ToString();
            switch (selectedPage)
            {
                case "Novatune.Pages.HomePage":
                    contentFrame.Navigate(new Pages.HomePage());
                    break;
                case "Novatune.Pages.OnlinePage":
                    contentFrame.Navigate(new Pages.OnlinePage());
                    break;
            }
        }
    }
    
    public void Cleanup()
    {
        GlobalMediaPlayerVM?.Cleanup();
        GlobalMediaControls?.Cleanup();
    }
}
