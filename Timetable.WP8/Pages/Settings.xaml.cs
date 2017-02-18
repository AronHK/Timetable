using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml.Navigation;
using Windows.ApplicationModel.Resources;

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
            LocationEnabled.IsEnabled = false;
            Geolocator geolocator = new Geolocator();
            bool error = false;
            try { Geoposition pos = await geolocator.GetGeopositionAsync(); } catch (Exception) { error = true; }
            if (error)
            {
            localSettings.Values["location"] = false;
            LocationEnabled.IsOn = false;
            note1.Visibility = Visibility.Visible;
            note2.Visibility = Visibility.Visible;
            }
            else
            {
                localSettings.Values["location"] = LocationEnabled.IsOn;
                note1.Visibility = Visibility.Collapsed;
                note2.Visibility = Visibility.Collapsed;
            }
            LocationEnabled.IsEnabled = true;
        }
        
        private async void GotoPrivacySettings(object sender, TappedRoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
        }

    }
}
