using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Media.Animation;
using Windows.System.Profile;
using System;

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

            if (bounds.Width < 500)
            {
                if (bounds.Width < 350)
                {
                    From.Width = 235;
                    To.Width = 235;
                    Date.Width = 300;
                    Time.Width = 300;
                }
                else
                {
                    From.Width = 255;
                    To.Width = 255;
                    Date.Width = 320;
                    Time.Width = 320;
                }
                
                panel.Margin = new Thickness(0, 0, 0, bounds.Height * 0.15);
                SearchButton.Visibility = Visibility.Collapsed;
                commandbar.ClosedDisplayMode = AppBarClosedDisplayMode.Compact;
            }
            else
            {
                From.Width = 320;
                To.Width = 320;
                Date.Width = 320;
                Time.Width = 320;
                panel.Margin = new Thickness(65, 0, 0, bounds.Height * 0.15);
                SearchButton.Margin = new Thickness(0, 10, 65, 0);
                SearchButton.Visibility = Visibility.Visible;
                commandbar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;
            }

            if (bounds.Height < 730)
            {
                Date.Visibility = Visibility.Visible;
                Date2.Visibility = Visibility.Collapsed;
            }
            else
            {
                Date.Visibility = Visibility.Collapsed;
                Date2.Visibility = Visibility.Visible;
                panel.Margin = new Thickness(panel.Margin.Left, 0, 0, 0);
            }

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                title.Margin = new Thickness(48, 10, 0, 0);
                XboxPanel.Margin = new Thickness(0, 0, 48, 27);
                commandbar.Visibility = Visibility.Collapsed;
                XboxPanel.Visibility = Visibility.Visible;
                SearchButton.Visibility = Visibility.Collapsed;
            }
        }

        private void SyncCalendars(CalendarView sender, CalendarViewSelectedDatesChangedEventArgs args)
        {
            if (Date2.SelectedDates.Count > 0 && Date.Date != Date2.SelectedDates[0])
            {
                Date.Date = Date2.SelectedDates[0];
                selectedDate = Date.Date.Value.ToString("yyyy-MM-dd");
            }
        }

        private void SyncCalendars(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            if (Date2.SelectedDates.Count == 0 || Date.Date != Date2.SelectedDates[0])
            {
                Date2.SelectedDates.Clear();
                if (Date.Date != null)
                    Date2.SelectedDates.Add((DateTimeOffset)Date.Date);
                selectedDate = Date.Date.Value.ToString("yyyy-MM-dd");
            }
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
