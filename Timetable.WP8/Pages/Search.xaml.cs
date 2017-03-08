using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Timetable
{
    /// <summary>
    /// Search page.
    /// </summary>
    public sealed partial class Search : Page
    {      
        private void GotoSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }

        private void DateChanged(object sender, DatePickerValueChangedEventArgs e)
        {
            selectedDate = Date.Date.Year.ToString() + "-" + Date.Date.Month.ToString().PadLeft(2, '0') + "-" + Date.Date.Day.ToString().PadLeft(2, '0');
        }
    }
}