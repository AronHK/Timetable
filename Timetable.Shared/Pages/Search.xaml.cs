#if !BACKGROUND
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
        private Windows.Storage.ApplicationDataContainer localSettings, roamingSettings;
        private List<Dictionary<string, string>> stops1, stops2;
        private string searchterm1, searchterm2;
        private ResourceLoader resourceLoader;
        private string selectedDate;
        private List<string> history;
        private List<string> historysid;
        private List<string> historylsid;

        public Search()
        {
            this.InitializeComponent();
            resourceLoader = ResourceLoader.GetForCurrentView();

#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar") && Application.Current.RequestedTheme == ApplicationTheme.Light)
#endif
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.White;
            }
#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView")) // PC
            {
                title.Foreground = new SolidColorBrush(Colors.White);
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.BackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.ForegroundColor = Colors.White;
            }
#endif

            localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            stops1 = new List<Dictionary<string, string>>();
            stops2 = new List<Dictionary<string, string>>();
            selectedDate = DateTime.Today.Date.ToString("yyyy-MM-dd");

            history = new List<string>();
            historysid = new List<string>();
            historylsid = new List<string>();
            string[] h1 = ((string)localSettings.Values["history1"]).Split('#');
            string[] h2 = ((string)localSettings.Values["history2"]).Split('#');
            string[] h3 = ((string)localSettings.Values["history3"]).Split('#');
            string[] h4 = ((string)localSettings.Values["history4"]).Split('#');
            if (h1.Length == 3)
            {
                history.Add(h1[0]);
                historysid.Add(h1[1]);
                historylsid.Add(h1[2]);
            }
            if (h2.Length == 3)
            {
                history.Add(h2[0]);
                historysid.Add(h2[1]);
                historylsid.Add(h2[2]);
            }
            if (h3.Length == 3)
            {
                history.Add(h3[0]);
                historysid.Add(h3[1]);
                historylsid.Add(h3[2]);
            }
            if (h4.Length == 3)
            {
                history.Add(h4[0]);
                historysid.Add(h4[1]);
                historylsid.Add(h4[2]);
            }
            From.ItemsSource = history;
            To.ItemsSource = history;

            Date.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

            Window.Current.SizeChanged += Resized;
            Resized(this, null);

            InputPane.GetForCurrentView().Showing += (s, args) =>
            {
                try { commandbar.Visibility = Visibility.Collapsed; } catch { }
            };
            InputPane.GetForCurrentView().Hiding += (s, args) =>
            {
                if (commandbar.Visibility == Visibility.Collapsed)
                    commandbar.Visibility = Visibility.Visible;
            };

            Loaded += (s, ev) => { From.Focus(FocusState.Programmatic); };
        }

        private async void StartSearch(object sender, RoutedEventArgs e)
        {
            inprogress.IsActive = true;
            From.IsEnabled = false;
            To.IsEnabled = false;
            Getloc1.Visibility = Visibility.Collapsed;
            Getloc2.Visibility = Visibility.Collapsed;
            Gethome1.Visibility = Visibility.Collapsed;
            Gethome2.Visibility = Visibility.Collapsed;
            Date.IsEnabled = false;
            Time.IsEnabled = false;
            AppbarGo.IsEnabled = false;
#if WINDOWS_UWP
            Date2.IsEnabled = false;
            SearchButton.IsEnabled = false;
#endif
            
            while (searchterm1 != From.Text || searchterm2 != To.Text)
                await Task.Delay(1000); // todo - replace

            int fromnum = -1, tonum = -1;               // find the stored bus-stop data that matches the textbox contents
            for (int i = 0; i < stops1.Count; i++)
            {
                string tocompare;
                stops1[i].TryGetValue("Nev", out tocompare);
                if (tocompare == From.Text)
                    fromnum = i;
            }
            for (int i = 0; i < stops2.Count; i++)
            {
                string tocompare;
                stops2[i].TryGetValue("Nev", out tocompare);
                if (tocompare == To.Text)
                    tonum = i;
            }

            inprogress.IsActive = false;
            From.IsEnabled = true;
            To.IsEnabled = true;
            Getloc1.Visibility = Visibility.Visible;
            Getloc2.Visibility = Visibility.Visible;
            Gethome1.Visibility = Visibility.Visible;
            Gethome2.Visibility = Visibility.Visible;
            Date.IsEnabled = true;
            Time.IsEnabled = true;
            AppbarGo.IsEnabled = true;
#if WINDOWS_UWP
            Date2.IsEnabled = true;
            SearchButton.IsEnabled = true;
#endif

            if ((fromnum == -1 && !history.Contains(From.Text)) || (tonum == -1 && !history.Contains(To.Text)))           // textbox content doesn't have matching IDs downloaded
            {
                From.Text = "";
                To.Text = "";
                From.ItemsSource = history;
                To.ItemsSource = history;

                var dialog = new MessageDialog(resourceLoader.GetString("BothError"), resourceLoader.GetString("Error"));

                dialog.Commands.Add(new UICommand("OK"));
                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 0;

                try { await dialog.ShowAsync(); } catch (UnauthorizedAccessException) { }
            }
            else                                        // get stored IDs based on the index found earlier
            {
                string temp, fromsetid, fromlsid, tosetid, tolsid;

                if (fromnum == -1)
                {
                    fromsetid = historysid[history.IndexOf(From.Text)];
                    fromlsid = historylsid[history.IndexOf(From.Text)];
                }
                else
                {
                    stops1[fromnum].TryGetValue("MegalloID", out temp);
                    fromlsid = temp;
                    stops1[fromnum].TryGetValue("VarosID", out temp);
                    fromsetid = temp;
                }

                if (tonum == -1)
                {
                    tosetid = historysid[history.IndexOf(To.Text)];
                    tolsid = historylsid[history.IndexOf(To.Text)];
                }
                else
                {
                    stops2[tonum].TryGetValue("MegalloID", out temp);
                    tolsid = temp;
                    stops2[tonum].TryGetValue("VarosID", out temp);
                    tosetid = temp;
                }
                
                if (!history.Contains(To.Text) && To.Text != (string)roamingSettings.Values["homename"])
                {
                    localSettings.Values["history4"] = localSettings.Values["history3"];
                    localSettings.Values["history3"] = localSettings.Values["history2"];
                    localSettings.Values["history2"] = localSettings.Values["history1"];
                    localSettings.Values["history1"] = $"{To.Text}#{tosetid}#{tolsid}";
                }
                if (!history.Contains(From.Text) && From.Text != (string)roamingSettings.Values["homename"])
                {
                    localSettings.Values["history4"] = localSettings.Values["history3"];
                    localSettings.Values["history3"] = localSettings.Values["history2"];
                    localSettings.Values["history2"] = localSettings.Values["history1"];
                    localSettings.Values["history1"] = $"{From.Text}#{fromsetid}#{fromlsid}";
                }

                // data to forward
                string[] data = new string[] { fromsetid, tosetid, fromlsid, tolsid, From.Text, To.Text, selectedDate, Time.Time.Hours.ToString(), Time.Time.Minutes.ToString() };
                Frame.Navigate(typeof(Results), data);
#if WINDOWS_UWP
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
#endif
            }
        }

        private async void RequestLocation(object sender, RoutedEventArgs e)
        {
            Getloc1.Visibility = Visibility.Collapsed;
            Getloc2.Visibility = Visibility.Collapsed;
            Gethome1.Visibility = Visibility.Collapsed;
            Gethome2.Visibility = Visibility.Collapsed;
            if (((Button)sender).Name == "Getloc1")
            {
                locprogress1.Visibility = Visibility.Visible;
                From.Focus(FocusState.Programmatic);
            }
            else
            {
                locprogress2.Visibility = Visibility.Visible;
                To.Focus(FocusState.Programmatic);
            }

            bool isallowed = false;
            if ((bool?)localSettings.Values["location"] != false)
            {
                isallowed = await Utilities.LocationFinder.IsLocationAllowed();
                if (isallowed)
                {
                    string town = await Utilities.LocationFinder.GetLocation();
                    if (town != null)
                    {
                        var arg = new AutoSuggestBoxTextChangedEventArgs();
                        arg.Reason = AutoSuggestionBoxTextChangeReason.UserInput;
                        if (((Button)sender).Name == "Getloc1")
                        {
                            From.Text = town;
                            InputChanged(From, arg);
                        }
                        else
                        {
                            To.Text = town;
                            InputChanged(To, arg);
                        }
                    }
                    else
                    {
                        var dialog = new MessageDialog(resourceLoader.GetString("LocationError"));
                        dialog.Commands.Add(new UICommand("OK"));
                        dialog.DefaultCommandIndex = 0;
                        await dialog.ShowAsync();
                    }
                }
            }
            if ((bool?)localSettings.Values["location"] == false || !isallowed)
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
            Gethome1.Visibility = Visibility.Visible;
            Gethome2.Visibility = Visibility.Visible;
            locprogress1.Visibility = Visibility.Collapsed;
            locprogress2.Visibility = Visibility.Collapsed;
        }

        private async void RequestHome(object sender, RoutedEventArgs e)
        {
            if (roamingSettings.Values["homename"] == null)
            {
                var dialog = new MessageDialog(resourceLoader.GetString("HomeNotSet"));

                dialog.Commands.Add(new UICommand("OK"));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("Settings"), (command) => { Frame.Navigate(typeof(Settings)); }));

                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 1;

                await dialog.ShowAsync();
            }
            else
            {
                Dictionary<string, string> newstop = new Dictionary<string, string>();
                newstop.Add("Nev", (string)roamingSettings.Values["homename"]);
                newstop.Add("VarosID", (string)roamingSettings.Values["homesid"]);
                newstop.Add("MegalloID", (string)roamingSettings.Values["homelsid"]);

                if (((Button)sender).Name == "Gethome1")
                {
                    From.Text = (string)roamingSettings.Values["homename"];
                    searchterm1 = (string)roamingSettings.Values["homename"];
                    From.ItemsSource = new List<string>();
                    stops1.Clear();
                    stops1.Add(newstop);
                }
                else
                {
                    To.Text = (string)roamingSettings.Values["homename"];
                    searchterm2 = (string)roamingSettings.Values["homename"];
                    To.ItemsSource = new List<string>();
                    stops2.Clear();
                    stops2.Add(newstop);
                }
            }
        }

        private async void InputChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (From.Text == "" || To.Text == "")
            {
                AppbarGo.IsEnabled = false;
#if WINDOWS_UWP
                SearchButton.IsEnabled = false;
#endif
            }
            else
            {
                AppbarGo.IsEnabled = true;
#if WINDOWS_UWP
                SearchButton.IsEnabled = true;
#endif
            }

            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string input = sender.Text;
                List<Dictionary<string, string>> stops = await Utilities.Autocomplete.GetSuggestions(input, selectedDate);

                if (sender.Name == "From")
                {
                    stops1.Clear();
                    stops1 = stops;
                }
                else
                {
                    stops2.Clear();
                    stops2 = stops;
                }

                List<string> suggestions = new List<string>();
                if (sender.Name == "From")
                {
                    searchterm1 = input;
                    foreach (Dictionary<string, string> stop in stops1)
                    {
                        string temp;
                        stop.TryGetValue("Nev", out temp);
                        if (!temp.Contains(" vá.") && !temp.Contains(" vmh."))
                            suggestions.Add(temp);
                    }
                }
                else
                {
                    searchterm2 = input;
                    foreach (Dictionary<string, string> stop in stops2)
                    {
                        string temp;
                        stop.TryGetValue("Nev", out temp);
                        if (!temp.Contains(" vá.") && !temp.Contains(" vmh."))
                            suggestions.Add(temp);
                    }
                }
                sender.ItemsSource = suggestions;
            }
        }

        private void SuggestboxFocused(object sender, RoutedEventArgs e)
        {
            if (((AutoSuggestBox)sender).Text == "" && ((AutoSuggestBox)sender).Items.Count > 0)
                ((AutoSuggestBox)sender).IsSuggestionListOpen = true;
        }

        private void StopChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            if (sender.Name == "From")
                searchterm1 = args.SelectedItem.ToString();
            else
                searchterm2 = args.SelectedItem.ToString();
        }

        private void HandleKeyboardEnter(object sender, KeyRoutedEventArgs e)
        {
#if WINDOWS_UWP
            FocusNavigationDirection down = FocusNavigationDirection.Down;
            FocusNavigationDirection up = FocusNavigationDirection.Up;
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.GamepadMenu)
#elif WINDOWS_PHONE_APP
            FocusNavigationDirection down = FocusNavigationDirection.Next;
            FocusNavigationDirection up = FocusNavigationDirection.Previous;
            if (e.Key == Windows.System.VirtualKey.Enter)
#endif
            {
                if (sender.Equals(From))
                {
                    if (From.Text == "" || To.Text == "")
                        FocusManager.TryMoveFocus(down);
                    else
                        StartSearch(this, null);
                }
                if (sender.Equals(To))
                {
                    if (From.Text == "" || To.Text == "")
                        FocusManager.TryMoveFocus(up);
                    else
                        StartSearch(this, null);
                }
            }

#if WINDOWS_UWP
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox" && e.Key == Windows.System.VirtualKey.Escape) // uwp doesn't respond to B in autosuggestbox
                Frame.GoBack();
#endif
        }
    }
}
#endif