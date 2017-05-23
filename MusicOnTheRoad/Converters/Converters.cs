using MusicOnTheRoad.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace MusicOnTheRoad.Converters
{

    public class ExpandedModeToSymbol : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || !(value is ExpandedModes) || (ExpandedModes)value == ExpandedModes.NotExpanded) return Symbol.Forward;
            if ((ExpandedModes)value == ExpandedModes.NotExpandable) return Symbol.Play;
            return Symbol.Back;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new Exception("this is a one-way binding, it should never come here");
            if (value == null || !(value is int)) return 0;
            return Math.Max((int)value, 0);
        }
    }
    public class BoolToVisible : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (!(value is bool) || (bool)value == false) return Visibility.Collapsed;
            return Visibility.Visible;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new Exception("this is a one-way binding, it should never come here");
        }
    }
}
