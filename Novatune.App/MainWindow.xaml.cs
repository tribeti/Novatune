using DevWinUI;
using Microsoft.UI.Xaml;

namespace Novatune.App;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DragMoveAndResizeHelper.SetDragMove(this, Root);
    }
}
