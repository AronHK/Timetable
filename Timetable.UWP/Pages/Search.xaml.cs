using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.UI.Core;
using System.Net.Http;
using Windows.System.Profile;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Windows.UI;
using Windows.UI.Xaml.Media;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Resources;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media.Animation;

namespace Timetable
{
    /// <summary>
    /// Search page.
    /// </summary>
    public sealed partial class Search : Page
    {
        private async void RequestLocation(object sender, RoutedEventArgs e)
        {
            Getloc1.Visibility = Visibility.Collapsed;
            Getloc2.Visibility = Visibility.Collapsed;
            if (((Button)sender).Name == "Getloc1")
            {
                locprogress1.Visibility = Visibility.Visible;

                await Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => From.Focus(FocusState.Programmatic)));
            }
            else
            {
                locprogress2.Visibility = Visibility.Visible;

                await Task.Factory.StartNew(
                () => Dispatcher.RunAsync(CoreDispatcherPriority.Low,
                    () => To.Focus(FocusState.Programmatic)));
            }
            

            GeolocationAccessStatus accessStatus = GeolocationAccessStatus.Unspecified;
            if ((bool?)localSettings.Values["location"] != false)
            {
                accessStatus = await Geolocator.RequestAccessAsync();
                if (accessStatus == GeolocationAccessStatus.Allowed)
                {
                    Geolocator geolocator = new Geolocator();
                    geolocator.DesiredAccuracyInMeters = 1500;
                    Geoposition pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
                    BasicGeoposition basicpos = new BasicGeoposition();
                    basicpos.Latitude = pos.Coordinate.Point.Position.Latitude;
                    basicpos.Longitude = pos.Coordinate.Point.Position.Longitude; ;
                    Geopoint point = new Geopoint(basicpos);

                    MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(point);
                    try
                    {
                        var arg = new AutoSuggestBoxTextChangedEventArgs();
                        arg.Reason = AutoSuggestionBoxTextChangeReason.UserInput;
                        if (((Button)sender).Name == "Getloc1")
                        {
                            From.Text = result.Locations[0].Address.Town;
                            InputChanged(From, arg);
                        }
                        else
                        {
                            To.Text = result.Locations[0].Address.Town;
                            InputChanged(To, arg);
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        var dialog = new MessageDialog(resourceLoader.GetString("LocationError"));
                        dialog.Commands.Add(new UICommand("OK"));
                        dialog.DefaultCommandIndex = 0;
                        await dialog.ShowAsync();
                    }
                }
            }
            if ((bool?)localSettings.Values["location"] == false || accessStatus != GeolocationAccessStatus.Allowed)
            {
                var dialog = new MessageDialog(resourceLoader.GetString("LocationDisabled"));

                dialog.Commands.Add(new UICommand("OK"));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("Settings"), (command) => { Frame.Navigate(typeof(Settings)); }));

                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            }

            Getloc1.Visibility = Visibility.Visible;
            Getloc2.Visibility = Visibility.Visible;
            locprogress1.Visibility = Visibility.Collapsed;
            locprogress2.Visibility = Visibility.Collapsed;
        }

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
