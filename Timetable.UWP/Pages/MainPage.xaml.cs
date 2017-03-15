using System;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.ViewManagement;
using Windows.UI.Popups;
using Windows.UI;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Animation;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Navigation;
using Windows.System.Profile;
using Microsoft.Services.Store.Engagement;
using Windows.UI.StartScreen;

namespace Timetable
{
    /// <summary>
    /// Start Page, saved lines.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Enabled;
            resourceLoader = ResourceLoader.GetForCurrentView();
            
            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                Appbar.Visibility = Visibility.Collapsed;
                XboxPanel.Visibility = Visibility.Visible;
                mainpanel.Margin = new Thickness(48, 27, 48, 27);
                LineList.Margin = new Thickness(0, 5, 0, 42);
            }

            //Window.Current.SizeChanged += WindowResized;
            ApplicationView.GetForCurrentView().VisibleBoundsChanged += WindowResized;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar") && Application.Current.RequestedTheme == ApplicationTheme.Light)
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.Black;
            }
            else
            {
                ApplicationView.GetForCurrentView().SetPreferredMinSize(new Size(500, 730));
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    titleBar.BackgroundColor = Colors.Black;
                    titleBar.ButtonBackgroundColor = Colors.Black;
                    titleBar.ButtonForegroundColor = Colors.White;
                }
                else
                {
                    titleBar.BackgroundColor = Colors.White;
                    titleBar.ForegroundColor = Colors.Black;
                    titleBar.ButtonBackgroundColor = Colors.White;
                    titleBar.ButtonForegroundColor = Colors.Black;
                }
            }

            Loaded += async (s, ev) =>
            {
                var logger = StoreServicesCustomEventLogger.GetDefault();
                string[] temp = ((string)roamingSettings.Values["usagelog"]).Split('|');
                int usage, lastday;
                int.TryParse(temp[1], out usage);
                int.TryParse(temp[0], out lastday);

                if (lastday != DateTime.Today.Day)
                {
                    usage++;
                    roamingSettings.Values["usagelog"] = DateTime.Today.Day + "|" + usage;
                    switch (usage)
                    {
                        case 20:
                            logger.Log("engagement 20"); // ~1 month once per day
                            break;
                        case 60:
                            logger.Log("engagement 60"); // ~3 months
                            break;
                        case 100:
                            logger.Log("engagement 100"); // ~5 months
                            break;
                        case 140:
                            logger.Log("engagement 140"); // ~7 months
                            break;
                        case 240:
                            logger.Log("engagement 240"); // ~1 year
                            break;
                    }
                }

                if (localSettings.Values["version"] == null)
                    localSettings.Values["version"] = App.VERSION;
                if ((string)localSettings.Values["version"] != App.VERSION)
                {
                    logger.Log(App.VERSION);

                    if ((bool)roamingSettings.Values["showlog"])
                    {
                        localSettings.Values["version"] = App.VERSION;
                        var popup = new ContentDialog();
                        popup.Content = new ChangelogWindow(Window.Current.Bounds.Width, Window.Current.Bounds.Height);
                        popup.PrimaryButtonText = resourceLoader.GetString("Closebutton");
                        popup.IsPrimaryButtonEnabled = true;

                        if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                            popup.FullSizeDesired = true;
                        else
                        {
                            popup.MinHeight = ActualHeight * 0.7;
                            popup.MaxHeight = ActualHeight * 0.7;
                            popup.MinWidth = 440;
                            popup.MaxWidth = 440;
                        }

                        await popup.ShowAsync();
                    }
                }
            };

            if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
                AppbarFeedback.Visibility = Visibility.Visible;

            if (localSettings.Values["location"] == null && AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Xbox")
            {
                var dialog = new MessageDialog(resourceLoader.GetString("InitialLocation"));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("Yes"), async (command) =>
                {
                    if (await Utilities.LocationFinder.IsLocationAllowed())
                        localSettings.Values["location"] = true;
                    else
                        localSettings.Values["location"] = false;
                }));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("No"), (command) =>
                {
                    localSettings.Values["location"] = false;
                }));
                dialog.CancelCommandIndex = 1;
                dialog.DefaultCommandIndex = 0;
                await dialog.ShowAsync();
            }

            await getSavedData(false);
        }

        //private void WindowResized(object sender, WindowSizeChangedEventArgs e)
        private void WindowResized(ApplicationView sender, object args)
        {
            double width = Window.Current.Bounds.Width;
            int newsize = 400;

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && width < 832 && width > 625)
            {
                foreach (Card card in LineList.Items)
                {
                    LineList.Margin = new Thickness(0, 1, 0, 0);
                    newsize = ((int)width - 32) / 2;
                    card.ChangeSize(newsize);
                }
            }
            else
            {
                foreach (Card card in LineList.Items)
                    card.ChangeSize(400);
            }

            if (width > 420)
            {
                int tilenum = (int)width - 20;
                tilenum /= newsize + 6;
                int margin = ((int)width - tilenum * (newsize + 6)) / 2 - 10;
                LineList.Margin = new Thickness(margin, 1, 0, 0);
            }
            else
                LineList.Margin = new Thickness(0, 1, 0, 0);
        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Search), null, new SuppressNavigationTransitionInfo());
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private void GotoSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings), null, new SuppressNavigationTransitionInfo());
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private async void UpdateJumplist()
        {
            var jumpList = await Windows.UI.StartScreen.JumpList.LoadCurrentAsync();
            jumpList.Items.Clear();

            var item = JumpListItem.CreateWithArguments("opensearch", resourceLoader.GetString("SearchText"));
            item.Logo = new Uri("ms-appx:///Assets/searchicon.png");
            jumpList.Items.Add(item);

            foreach (Line line in savedLines)
            {
                string name = line.Name;
                if (name.Trim() == "")
                    name = resourceLoader.GetString("Unnamed");
                var item2 = JumpListItem.CreateWithArguments($"{line.FromsID}-{line.FromlsID}-{line.TosID}-{line.TolsID}", line.Name);
                item2.Logo = new Uri("ms-appx:///Assets/BadgeLogo.scale-100.png");
                item2.Description = $"{line.From} - {line.To}";
                item2.GroupName = resourceLoader.GetString("SavedLines");
                jumpList.Items.Add(item2);
            }

            await jumpList.SaveAsync();
        }

        private async void Rate(object sender, RoutedEventArgs e)
        {
            await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-windows-store://review/?ProductId=9NBLGGH4T6B1"));
        }

        private async void Feedback(object sender, RoutedEventArgs e)
        {
            var launcher = Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.GetDefault();
            await launcher.LaunchAsync();
        }

        private async void KeyupEvent(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.GamepadX)
                await getSavedData(true);

            if (e.Key == Windows.System.VirtualKey.GamepadY)
            {
                Frame.Navigate(typeof(Search), null, new EntranceNavigationTransitionInfo());
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }

            if (e.Key == Windows.System.VirtualKey.GamepadView)
            {
                Frame.Navigate(typeof(Settings), null, new EntranceNavigationTransitionInfo());
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
        }

        private void LineList_KeyUp(object sender, KeyRoutedEventArgs e)
        {
            //if (e.Key == Windows.System.VirtualKey.GamepadA)
            //    Newcard_Tapped(sender, null);

            //if (e.Key == Windows.System.VirtualKey.F3)
            //    ((Card)sender).ContextFlyout.ShowAt((Card)sender);
        }

        private void itemclick(object sender, ItemClickEventArgs e)
        {
            OpenLine((Card)LineList.SelectedItem, LineList.SelectedIndex);
        }
    }
}
