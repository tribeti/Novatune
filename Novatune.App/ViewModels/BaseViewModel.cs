using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Novatune.App.ViewModels;

public partial class BaseViewModel : ObservableObject, IDisposable
{
    public void Dispose()
    {
        // Dispose managed resources if any and suppress finalization.
        // If this class is intended to be inherited and holds unmanaged resources,
        // derived classes should override a protected virtual Dispose(bool) method.
        GC.SuppressFinalize(this);
    }
}
