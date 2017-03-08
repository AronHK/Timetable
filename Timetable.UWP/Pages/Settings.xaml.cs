using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Devices.Geolocation;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.Storage;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.UI.Core;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;
using Windows.Networking.Connectivity;

namespace Timetable
{
    /// <summary>
    /// Settings page.
    /// </summary>
    public sealed partial class Settings : Page
    {
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
        }
        
        private async void GotoPrivacySettings(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings:privacy-location"));
        }
        
        private void WindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    title.Foreground = new SolidColorBrush(Colors.White);
                    titlebg.Fill = new SolidColorBrush(Colors.Black);
                }
                else
                {
                    title.Foreground = new SolidColorBrush(Colors.Black);
                    titlebg.Fill = new SolidColorBrush(Colors.White);
                }
            }
            else
            {
                title.Foreground = new SolidColorBrush(Colors.White);
                titlebg.Fill = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
            }
        }
    }
}
