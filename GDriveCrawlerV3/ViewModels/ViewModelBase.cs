using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ReactiveUI;

namespace GDriveCrawlerV3.ViewModels;

public class ViewModelBase : ReactiveObject
{
    public bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        this.RaisePropertyChanged(propertyName);
        return true;
    }
}