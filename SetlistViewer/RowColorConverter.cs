using Microsoft.Maui.Controls;
using System;
using System.Globalization;

namespace SetlistViewer.Helpers
{
    public class RowColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int index)
            {
                return (index % 2 == 0) ? Colors.LightGray : Color.Parse("#E0F7FA");
            }
            return Colors.LightGray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
