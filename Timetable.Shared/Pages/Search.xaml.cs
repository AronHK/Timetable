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
        private Windows.Storage.ApplicationDataContainer localSettings;
        private List<Dictionary<string, string>> megallok1, megallok2;
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
            megallok1 = new List<Dictionary<string, string>>();
            megallok2 = new List<Dictionary<string, string>>();
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

            var bounds = Window.Current.Bounds;
            if (bounds.Width < 350)
            {
                From.Width = bounds.Width - 50;
                To.Width = bounds.Width - 50;
                Date.Width = bounds.Width - 50;
                Time.Width = bounds.Width - 50;
            }
            Date.Date = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day);

#if WINDOWS_UWP
            if (bounds.Height < 730)
            {
                Date.Visibility = Visibility.Visible;
                Date2.Visibility = Visibility.Collapsed;
            }
            else
            {
                Date.Visibility = Visibility.Collapsed;
                Date2.Visibility = Visibility.Visible;
            }

            if (AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Mobile")
            {
                SearchButton.Visibility = Visibility.Visible;
                commandbar.ClosedDisplayMode = AppBarClosedDisplayMode.Minimal;

                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                {
                    title.Margin = new Thickness(48, 10, 0, 0);
                    XboxPanel.Margin = new Thickness(0, 0, 48, 27);
                    commandbar.Visibility = Visibility.Collapsed;
                    XboxPanel.Visibility = Visibility.Visible;
                    SearchButton.Visibility = Visibility.Collapsed;
                }
            }

            Window.Current.SizeChanged += Resized;
#endif

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
            for (int i = 0; i < megallok1.Count; i++)
            {
                string tocompare;
                megallok1[i].TryGetValue("Nev", out tocompare);
                if (tocompare == From.Text)
                    fromnum = i;
            }
            for (int i = 0; i < megallok2.Count; i++)
            {
                string tocompare;
                megallok2[i].TryGetValue("Nev", out tocompare);
                if (tocompare == To.Text)
                    tonum = i;
            }

            inprogress.IsActive = false;
            From.IsEnabled = true;
            To.IsEnabled = true;
            Getloc1.Visibility = Visibility.Visible;
            Getloc2.Visibility = Visibility.Visible;
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

                await dialog.ShowAsync();
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
                    megallok1[fromnum].TryGetValue("MegalloID", out temp);
                    fromlsid = temp;
                    megallok1[fromnum].TryGetValue("VarosID", out temp);
                    fromsetid = temp;
                }

                if (tonum == -1)
                {
                    tosetid = historysid[history.IndexOf(To.Text)];
                    tolsid = historylsid[history.IndexOf(To.Text)];
                }
                else
                {
                    megallok2[tonum].TryGetValue("MegalloID", out temp);
                    tolsid = temp;
                    megallok2[tonum].TryGetValue("VarosID", out temp);
                    tosetid = temp;
                }
                
                if (!history.Contains(To.Text))
                {
                    localSettings.Values["history4"] = localSettings.Values["history3"];
                    localSettings.Values["history3"] = localSettings.Values["history2"];
                    localSettings.Values["history2"] = localSettings.Values["history1"];
                    localSettings.Values["history1"] = $"{To.Text}#{tosetid}#{tolsid}";
                }
                if (!history.Contains(From.Text))
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
            if (((Button)sender).Name == "Getloc1")
            {
#if WINDOWS_UWP
                locprogress1.Visibility = Visibility.Visible;
#endif

                From.Focus(FocusState.Programmatic);
            }
            else
            {
#if WINDOWS_UWP
                locprogress2.Visibility = Visibility.Visible;
#endif

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
#if WINDOWS_UWP
            locprogress1.Visibility = Visibility.Collapsed;
            locprogress2.Visibility = Visibility.Collapsed;
#endif
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

                if (sender.Name == "From") // clear old suggestions
                    megallok1.Clear();
                else
                    megallok2.Clear();

                using (var client = new HttpClient()) // get new suggestions
                {
                    string tosend = "{\"query\":\"get_stations2_json\",\"fieldvalue\":\"" + input + "\",\"datum\":\"" + selectedDate + "\"}";
                    var values = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "json", tosend }
                    };

                    var content = new FormUrlEncodedContent(values);
                    HttpResponseMessage response = null;
                    try { response = await client.PostAsync("http://menetrendek.hu/uj_menetrend/hu/ajax_response_gen.php", content); }
                    catch (Exception)
                    {
                        var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                        dialog.Commands.Add(new UICommand("OK", (command) => { Frame.GoBack(); }));
                        dialog.CancelCommandIndex = 0;
                        dialog.DefaultCommandIndex = 0;
                        await dialog.ShowAsync();
                        return;
                    }

                    var codes = await response.Content.ReadAsStringAsync();
                    codes = Regex.Unescape(codes);
                    if (codes.Length > 0) // process data - get names and IDs for towns/stops
                    {
                        codes = codes.Substring(1, codes.Length - 2);

                        Regex regex = new Regex("(?<={)[^}]+?(?=})", RegexOptions.None);
                        foreach (Match match in regex.Matches(codes))
                        {
                            string toParse = match.ToString();
                            Dictionary<string, string> megallo = new Dictionary<string, string>();
                            megallo.Add("Nev", getproperty(toParse, "lsname"));
                            megallo.Add("VarosID", getproperty(toParse, "settlement_id"));
                            megallo.Add("MegalloID", getproperty(toParse, "ls_id"));
                            if (sender.Name == "From")
                                megallok1.Add(megallo);
                            else
                                megallok2.Add(megallo);
                        }

                        List<string> suggestions = new List<string>();
                        if (sender.Name == "From")
                        {
                            searchterm1 = input;
                            foreach (Dictionary<string, string> megallo in megallok1)
                            {
                                string temp;
                                megallo.TryGetValue("Nev", out temp);
                                if (!temp.Contains(" vá.") && !temp.Contains(" vmh."))
                                    suggestions.Add(temp);
                            }
                        }
                        else
                        {
                            searchterm2 = input;
                            foreach (Dictionary<string, string> megallo in megallok2)
                            {
                                string temp;
                                megallo.TryGetValue("Nev", out temp);
                                if (!temp.Contains(" vá.") && !temp.Contains(" vmh."))
                                    suggestions.Add(temp);
                            }
                        }
                        sender.ItemsSource = suggestions;
                    }
                }
            }
        }

        private void SuggestboxFocused(object sender, RoutedEventArgs e)
        {
            if (((AutoSuggestBox)sender).Text == "" && ((AutoSuggestBox)sender).Items.Count > 0)
                ((AutoSuggestBox)sender).IsSuggestionListOpen = true;
        }

        private string getproperty(string text, string key)
        {
            Match match = Regex.Match(text, "(?<=" + key + "\":(\")?)[^\"]*(?=(,\"|\",\"))");
            return match.Value;
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