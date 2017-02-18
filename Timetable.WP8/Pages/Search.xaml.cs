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
using Windows.UI.Xaml.Navigation;

namespace Timetable
{
    /// <summary>
    /// Search page.
    /// </summary>
    public sealed partial class Search : Page
    {
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            From.Focus(FocusState.Keyboard); // doesn't seem to work
        }

        private async void RequestLocation(object sender, TappedRoutedEventArgs e)
        {
            if ((bool?)localSettings.Values["location"] != false)
            {
                Getloc1.Visibility = Visibility.Collapsed;
                Getloc2.Visibility = Visibility.Collapsed;

                Geolocator geolocator = new Geolocator();
                Geoposition pos = null;
                bool error = false;
                try { pos = await geolocator.GetGeopositionAsync(); } catch (Exception) { error = true; }

                if (!error)
                {
                    localSettings.Values["location"] = true;
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

                Getloc1.Visibility = Visibility.Visible;
                Getloc2.Visibility = Visibility.Visible;
            }
            else
            {
                var dialog = new MessageDialog(resourceLoader.GetString("LocationDisabled"));

                dialog.Commands.Add(new UICommand("OK"));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("Settings"), (command) => { Frame.Navigate(typeof(Settings)); }));

                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            }
        }
        
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