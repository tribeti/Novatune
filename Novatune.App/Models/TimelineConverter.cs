using Microsoft.UI.Xaml.Data;
using System;

namespace Novatune.App.Models;

public partial class TimelineConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is double totalSeconds)
        {
            TimeSpan time = TimeSpan.FromSeconds(totalSeconds);
            if (time.TotalHours >= 1)
            {
                return time.ToString(@"hh\:mm\:ss");
            }
            else
            {
                return time.ToString(@"mm\:ss");
            }
        }
        return value;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
