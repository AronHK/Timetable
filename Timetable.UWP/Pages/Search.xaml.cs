using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;

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
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }
        
        private void Resized(object sender, WindowSizeChangedEventArgs e)
        {
            var bounds = Window.Current.Bounds;
            if (bounds.Height < 735)
            {
                Date.Visibility = Visibility.Visible;
                Date2.Visibility = Visibility.Collapsed;
            }
            else
            {
                Date.Visibility = Visibility.Collapsed;
                Date2.Visibility = Visibility.Visible;
            }
        }

        private void SyncCalendars(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            Date.Date = Date2.SelectedDates[0];
            selectedDate = Date.Date.Value.ToString("yyyy-MM-dd");
        }

        private void SyncCalendars(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            //Date2.SelectedDates[0] = (DateTimeOffset)Date.Date;
            selectedDate = Date.Date.Value.ToString("yyyy-MM-dd");
        }

        private void HandleKeyboardStart(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.GamepadMenu)
            {
                if (From.Text != "" && To.Text != "")
                    StartSearch(this, null);
            }
        }
        
        private void HandleKeyboardSettings(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.GamepadView)
            {
                Frame.Navigate(typeof(Settings), null, new EntranceNavigationTransitionInfo());
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
        }
    }
}
