using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Windows.System;
using Windows.UI;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.StartScreen;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=391641

namespace Timetable
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
            resourceLoader = ResourceLoader.GetForCurrentView();

            if (Application.Current.RequestedTheme == ApplicationTheme.Light)
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.Black;
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
                    popup.FullSizeDesired = true;
                    await popup.ShowAsync();
                }
            };

            if (localSettings.Values["cleanstorage"] != null && (bool)localSettings.Values["cleanstorage"] == true)
            {
                localSettings.Values["cleanstorage"] = false;
                Windows.Storage.StorageFile datafile = await Windows.Storage.ApplicationData.Current.RoamingFolder.GetFileAsync("linedata");
                await datafile.DeleteAsync();
            }
            
            if (localSettings.Values["location"] == null)
            {
                Geolocator geolocator = new Geolocator();
                Geoposition pos = null;
                bool error = false;
                try { pos = await geolocator.GetGeopositionAsync(); } catch (Exception) { error = true; }
                if (error)
                {
                    var dialog = new MessageDialog(resourceLoader.GetString("InitialLocationError"));
                    dialog.Commands.Add(new UICommand("OK", (command) =>
                    {
                        localSettings.Values["location"] = false;
                    }));
                    dialog.Commands.Add(new UICommand(resourceLoader.GetString("Settings"), async (command) =>
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri("ms-settings-location:"));
                    }));
                    dialog.CancelCommandIndex = 0;
                    dialog.DefaultCommandIndex = 1;
                    await dialog.ShowAsync();
                }
                else
                    localSettings.Values["location"] = true;
            }
            await getSavedData(false);
        }

        private void WindowResized(ApplicationView sender, object args)
        {
            double width = ApplicationView.GetForCurrentView().VisibleBounds.Width;
            int newsize = 400;

            if (width < 832 && width > 625)
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
        }

        private void StartSearch(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Search));
        }

        private void GotoSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }

        private async void OpenRate(object sender, RoutedEventArgs e)
        {
            await Launcher.LaunchUriAsync(new Uri("ms-windows-store:reviewapp?appid=" + Windows.ApplicationModel.Package.Current.Id.Name));
        }
    }
}
