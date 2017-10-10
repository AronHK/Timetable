using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

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

        private void Resized(object sender, WindowSizeChangedEventArgs e)
        {
            var bounds = Window.Current.Bounds;
            panel.Margin = new Thickness(0, 0, 0, bounds.Height * 0.2);
            if (bounds.Width < 490)
            {
                From.Width = 290;
                To.Width = 290;
                Date.Width = 145;
                Time.Width = 145;
                panel.Width = 370;
            }
            else if (bounds.Width >= 490)
            {
                From.Width = 320;
                To.Width = 320;
                Date.Width = 160;
                Time.Width = 160;
                panel.Width = 400;
            }

            /*if (bounds.Width > bounds.Height)
                title.Margin = new Thickness(20, 0, 0, 0);
            else
                title.Margin = new Thickness(15, 50, 0, 0);*/
        }
    }
}