using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;

namespace MusicOnTheRoad.Converters
{

	public class BoolToSymbol : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, string language)
		{
			if (value == null || !(value is bool) || ((bool)value == false)) return Symbol.Forward;
			return Symbol.Back;
		}

		public object ConvertBack(object value, Type targetType, object parameter, string language)
		{
			throw new Exception("this is a one-way binding, it should never come here");
			if (value == null || !(value is int)) return 0;
			return Math.Max((int)value, 0);
		}
	}
}
