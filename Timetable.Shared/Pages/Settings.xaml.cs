#if !BACKGROUND
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
using System.Collections.Generic;

namespace Timetable
{
    /// <summary>
    /// Settings page.
    /// </summary>
    public sealed partial class Settings : Page
    {
        ApplicationDataContainer localSettings;
        ApplicationDataContainer roamingSettings;
        ResourceLoader resourceLoader;
        List<Dictionary<string, string>> stops;

        public Settings()
        {
            this.InitializeComponent();
            resourceLoader = ResourceLoader.GetForCurrentView();
            localSettings = ApplicationData.Current.LocalSettings;
            roamingSettings = ApplicationData.Current.RoamingSettings;

#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView")) // PC
            {
                title.Foreground = new SolidColorBrush(Colors.White);
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.BackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.ForegroundColor = Colors.White;
                //scroller.Width = Window.Current.Bounds.Width - 10;
                Window.Current.Activated += WindowActivated;
                Window.Current.SizeChanged += WindowResized;
            }
            
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
#endif
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.White;
            }
            
            WindowResized(this, null);

#if WINDOWS_UWP
            if (Windows.System.Profile.AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                mainpanel.Margin = new Thickness(48, 10, 48, 0); 
                tileupdate.IsEnabled = false;
                note6.Text = resourceLoader.GetString("XboxError");
            }
            else
#endif
            {
                switch ((uint)localSettings.Values["frequency"])
                {
                    case 15: tileupdate.SelectedIndex = 0; break;
                    case 30: tileupdate.SelectedIndex = 1; break;
                    case 40: tileupdate.SelectedIndex = 2; break;
                    case 60: tileupdate.SelectedIndex = 3; break;
                    case 120: tileupdate.SelectedIndex = 4; break;
                    case 300: tileupdate.SelectedIndex = 5; break;
                    case 0: tileupdate.SelectedIndex = 6;
                            mins2.Visibility = Visibility.Collapsed;
                            break;
                }
            }

            if (localSettings.Values["location"] == null)
                LocationEnabled.IsOn = false;
            else
                LocationEnabled.IsOn = (bool)localSettings.Values["location"];

            Theme.SelectedIndex = (int)localSettings.Values["theme"];
            Exactres.IsOn = (bool)roamingSettings.Values["exact"];
            AlwaysUpdate.IsOn = (bool)localSettings.Values["alwaysupdate"];
            Linechange.IsOn = (bool)roamingSettings.Values["canchange"];
            Showlog.IsOn = (bool)roamingSettings.Values["showlog"];
            homeupdatetoggle.IsOn = (bool)roamingSettings.Values["showhome"];
            if (roamingSettings.Values["homename"] != null)
#if WINDOWS_UWP
            {
                //homebutton.Content = roamingSettings.Values["homename"];
                TextBlock content = new TextBlock();
                content.Text = (string)roamingSettings.Values["homename"];
                content.TextTrimming = TextTrimming.CharacterEllipsis;
                content.Margin = new Thickness(0);
                homebutton.Content = content;
            }
#elif WINDOWS_PHONE_APP
            {
                homebutton.Text = (string)roamingSettings.Values["homename"];
                homebutton.TextTrimming = TextTrimming.CharacterEllipsis;
            }
#endif


            switch ((String)roamingSettings.Values["price"])
            {
                case "100%": Price.SelectedIndex = 0; break;
                case "-90%": Price.SelectedIndex = 2; break;
                case "-50%": Price.SelectedIndex = 1; break;
                case "Ingyen": Price.SelectedIndex = 3; break;
            }

            Sort.SelectedIndex = (int)roamingSettings.Values["sort"] - 1;
            Changes.Text = (String)roamingSettings.Values["change"];
            Walk.Text = (String)roamingSettings.Values["walk"];
            Wait.Text = (String)roamingSettings.Values["wait"];

            Changes.IsEnabled = Linechange.IsOn;
            Walk.IsEnabled = Linechange.IsOn;
            Wait.IsEnabled = Linechange.IsOn;
#if WINDOWS_UWP
            version.Content += App.VERSION;
#elif WINDOWS_PHONE_APP
            version.Text += App.VERSION;
#endif
        }

        private void WindowResized(object sender, WindowSizeChangedEventArgs e)
        {
            double width = Window.Current.Bounds.Width;
            if (width > 920)
                width = 920;
            locationlabel.Width = width / 1.6;
#if WINDOWS_UWP
            if (width < 350)
            {
                mins1.Text = resourceLoader.GetString("Mins");
                mins2.Text = resourceLoader.GetString("Mins");
                Showloglabel.Text = resourceLoader.GetString("ShowlogShort");
            }
            if (width > 400)
                changeslabel.Text = resourceLoader.GetString("ChangesLong");

            homebutton.MaxWidth = width - 100;
#endif
        }

        private void Exactres_Toggled(object sender, RoutedEventArgs e)
        {
            roamingSettings.Values["exact"] = Exactres.IsOn;
            if (Exactres.IsOn)
            {
                note4.Visibility = Visibility.Collapsed;
                note5.Visibility = Visibility.Visible;
            }
            else
            {
                note4.Visibility = Visibility.Visible;
                note5.Visibility = Visibility.Collapsed;
            }
        }

        private void PriceChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Price.SelectedIndex != 3)
                roamingSettings.Values["price"] = (String)((ComboBoxItem)Price.SelectedItem).Content;
            else
                roamingSettings.Values["price"] = "Ingyen";
        }

        private void SortChanged(object sender, SelectionChangedEventArgs e)
        {
            roamingSettings.Values["sort"] = Sort.SelectedIndex + 1;
        }

        private void Linechange_Toggled(object sender, RoutedEventArgs e)
        {
            roamingSettings.Values["canchange"] = Linechange.IsOn;
            Changes.IsEnabled = Linechange.IsOn;
            Walk.IsEnabled = Linechange.IsOn;
            Wait.IsEnabled = Linechange.IsOn;
        }

        private void Changes_TextChanged(object sender, TextChangedEventArgs e)
        {
            roamingSettings.Values["change"] = Changes.Text;
        }

        private void Wait_TextChanged(object sender, TextChangedEventArgs e)
        {
            roamingSettings.Values["wait"] = Wait.Text;
        }

        private void Walk_TextChanged(object sender, TextChangedEventArgs e)
        {
            roamingSettings.Values["walk"] = Walk.Text;
        }


        private async void ThemeChanged(object sender, SelectionChangedEventArgs e)
        {
            localSettings.Values["theme"] = Theme.SelectedIndex;
            if (Theme.IsDropDownOpen)
            {
                var dialog = new MessageDialog(resourceLoader.GetString("RestartMessage"));
                dialog.Commands.Add(new UICommand("OK"));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("Close"), (command) => { Windows.ApplicationModel.Core.CoreApplication.Exit(); }));
                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 1;
                await dialog.ShowAsync();
            }
        }

        private async void FrequencyChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tileupdate.IsDropDownOpen)
            {
                tileupdate.IsEnabled = false;
                uint frequency;
                try { frequency = uint.Parse((string)((ComboBoxItem)tileupdate.SelectedItem).Content); }
                catch (FormatException) { frequency = 0; }
                localSettings.Values["frequency"] = frequency;

                foreach (var task in BackgroundTaskRegistration.AllTasks)
                {
                    if (task.Value.Name == "ScheduledTileUpdater")
                        task.Value.Unregister(true);
                    if (task.Value.Name == "ScheduledSecondaryTileUpdater")
                        task.Value.Unregister(true);
                }

                if (frequency != 0)
                {
                    mins2.Visibility = Visibility.Visible;
#if WINDOWS_UWP
                    await App.trigger.RequestAsync();
#endif
                    var access = await BackgroundExecutionManager.RequestAccessAsync();
#if WINDOWS_PHONE_APP
                    if (access != BackgroundAccessStatus.Denied)
#elif WINDOWS_UWP
                    if (access != BackgroundAccessStatus.DeniedBySystemPolicy && access != BackgroundAccessStatus.DeniedByUser)
#endif
                    {
                        BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
                        builder.Name = "ScheduledTileUpdater";
                        builder.TaskEntryPoint = "Timetable.TileUpdater";
                        builder.IsNetworkRequested = true;
                        TimeTrigger timetrigger = new TimeTrigger(frequency, false);
                        builder.SetTrigger(timetrigger);
                        builder.Register();
                    }

                    var access2 = await BackgroundExecutionManager.RequestAccessAsync();
#if WINDOWS_PHONE_APP
                    if (access2 != BackgroundAccessStatus.Denied)
#elif WINDOWS_UWP
                    if (access2 != BackgroundAccessStatus.DeniedBySystemPolicy && access2 != BackgroundAccessStatus.DeniedByUser)
#endif
                    {
                        BackgroundTaskBuilder builder2 = new BackgroundTaskBuilder();
                        builder2.Name = "ScheduledSecondaryTileUpdater";
                        builder2.TaskEntryPoint = "Timetable.SecondaryTileUpdater";
                        builder2.IsNetworkRequested = true;
                        TimeTrigger timetrigger2 = new TimeTrigger(frequency, false);
                        builder2.SetTrigger(timetrigger2);
                        builder2.Register();
                    }
                    await System.Threading.Tasks.Task.Delay(2000);
                }
                else
                    mins2.Visibility = Visibility.Collapsed;

                tileupdate.IsEnabled = true;
            }
        }

        private async void OpenChangelog(object sender, RoutedEventArgs e)
        {
            var popup = new ContentDialog();
            popup.Content = new ChangelogWindow(ActualWidth, ActualHeight);
            popup.PrimaryButtonText = resourceLoader.GetString("Closebutton");
            popup.IsPrimaryButtonEnabled = true;
#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
#endif
            {
                popup.FullSizeDesired = true;

#if WINDOWS_UWP
                titlebg.Fill = Resources["SystemAltHighColor"] as SolidColorBrush;
#endif
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                {
                    var statusBar = StatusBar.GetForCurrentView();
                    statusBar.ForegroundColor = Colors.Black;
                }
            }
#if WINDOWS_UWP
            else
            {
                popup.MinHeight = ActualHeight * 0.7;
                popup.MaxHeight = ActualHeight * 0.7;
                popup.MinWidth = 440;
                popup.MaxWidth = 440;
            }
#endif

            await popup.ShowAsync();

#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
#endif
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.White;
#if WINDOWS_UWP
                titlebg.Fill = Resources["SystemControlBackgroundAccentBrush"] as SolidColorBrush;
#endif
            }
        }

        private void AlwaysUpdate_Toggled(object sender, RoutedEventArgs e)
        {
            localSettings.Values["alwaysupdate"] = AlwaysUpdate.IsOn;
        }

        private void Showlog_Toggled(object sender, RoutedEventArgs e)
        {
            roamingSettings.Values["showlog"] = Showlog.IsOn;
        }

        private async void hometextbox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                string input = sender.Text;
                stops = await Utilities.Autocomplete.GetSuggestions(input, "2020-01-01");

                List<string> suggestions = new List<string>();
                foreach (Dictionary<string, string> stop in stops)
                {
                    string temp;
                    stop.TryGetValue("Nev", out temp);
                    if (!temp.Contains(" vá.") && !temp.Contains(" vmh."))
                        suggestions.Add(temp);
                }
                sender.ItemsSource = suggestions;
            }
        }

        private void hometextbox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            int stopindex = -1;
            for (int i = 0; i < stops.Count; i++)
            {
                string tocompare;
                stops[i].TryGetValue("Nev", out tocompare);
                if (tocompare == args.SelectedItem.ToString())
                    stopindex = i;
            }

            string temp;
            stops[stopindex].TryGetValue("MegalloID", out temp);
            roamingSettings.Values["homelsid"] = temp;
            stops[stopindex].TryGetValue("VarosID", out temp);
            roamingSettings.Values["homesid"] = temp;
            roamingSettings.Values["homename"] = args.SelectedItem.ToString();

#if WINDOWS_UWP
            homebutton.Content = args.SelectedItem.ToString();
#elif WINDOWS_PHONE_APP
            homebutton.Text = args.SelectedItem.ToString();
#endif
            hometextbox.Visibility = Visibility.Collapsed;
            homelabel.Visibility = Visibility.Visible;
            homebutton.Visibility = Visibility.Visible;
        }

        private void hometextbox_LostFocus(object sender, RoutedEventArgs e)
        {
            hometextbox.Visibility = Visibility.Collapsed;
            homelabel.Visibility = Visibility.Visible;
            homebutton.Visibility = Visibility.Visible;
        }

        private void Home_Toggled(object sender, RoutedEventArgs e)
        {
            roamingSettings.Values["showhome"] = homeupdatetoggle.IsOn;
        }
    }
}
#endif