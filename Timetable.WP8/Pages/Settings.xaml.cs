using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

namespace Timetable
{
    /// <summary>
    /// Settings page.
    /// </summary>
    public sealed partial class Settings : Page
    {
        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            /*var statusBar = StatusBar.GetForCurrentView();
            statusBar.BackgroundColor = (App.Current.Resources["PhoneAccentBrush"] as SolidColorBrush).Color;
            statusBar.ForegroundColor = Colors.White;
            statusBar.BackgroundOpacity = 1;*/

            await System.Threading.Tasks.Task.Delay(200);
            tileupdate.Width = ActualWidth - 115;
            changeslabel.Width = ActualWidth - 190;
            walklabel.Width = ActualWidth - 190;
            waitlabel.Width = ActualWidth - 190;
        }

        private async void LocationEnabled_Toggled(object sender, RoutedEventArgs e)
        {
            if (await Utilities.LocationFinder.IsLocationAllowed())
            {
                localSettings.Values["location"] = LocationEnabled.IsOn;
                note1.Visibility = Visibility.Collapsed;
                note2.Visibility = Visibility.Collapsed;
            }
            else
            {
                localSettings.Values["location"] = false;
                LocationEnabled.IsOn = false;
                note1.Visibility = Visibility.Visible;
                note2.Visibility = Visibility.Visible;
            }
            LocationEnabled.IsEnabled = true;
        }
        
        private async void GotoPrivacySettings(object sender, TappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
        }

    }
}
