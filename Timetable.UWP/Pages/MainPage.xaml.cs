using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.ViewManagement;
using System.Net.Http;
using Windows.UI.Popups;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using Windows.UI;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Hosting;
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Geolocation;
using Windows.System.Profile;
using Windows.Networking.Connectivity;

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
            resourceLoader = ResourceLoader.GetForCurrentView();

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

            Loaded += async (s, ev) =>
            {
                if (localSettings.Values["version"] == null)
                    localSettings.Values["version"] = App.VERSION;
                if ((string)localSettings.Values["version"] != App.VERSION && (bool)roamingSettings.Values["showlog"])
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
            };

            if (localSettings.Values["cleanstorage"] != null && (bool)localSettings.Values["cleanstorage"] == true)
            {
                localSettings.Values["cleanstorage"] = false;
                Windows.Storage.StorageFile datafile = await Windows.Storage.ApplicationData.Current.RoamingFolder.GetFileAsync("linedata");
                await datafile.DeleteAsync();
            }

            if (Microsoft.Services.Store.Engagement.StoreServicesFeedbackLauncher.IsSupported())
                AppbarFeedback.Visibility = Visibility.Visible;

            if (localSettings.Values["location"] == null && AnalyticsInfo.VersionInfo.DeviceFamily != "Windows.Xbox")
            {
                var dialog = new MessageDialog(resourceLoader.GetString("InitialLocation"));
                dialog.Commands.Add(new UICommand(resourceLoader.GetString("Yes"), async (command) =>
                {
                    var accessStatus = await Geolocator.RequestAccessAsync();
                    if (accessStatus != GeolocationAccessStatus.Allowed)
                        localSettings.Values["location"] = false;
                    else
                        localSettings.Values["location"] = true;
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
