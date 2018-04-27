#if !BACKGROUND
using System;
using System.Collections.Generic;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.Devices.Geolocation;
using Windows.Foundation.Metadata;
using Windows.Graphics.Display;
using Windows.System.Profile;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace Timetable
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            this.Suspending += OnSuspending;

#if WINDOWS_UWP
            if (ApiInformation.IsPropertyPresent("Windows.UI.Xaml.Controls.Control", "RequiresPointer"))
                this.RequiresPointerMode = ApplicationRequiresPointerMode.WhenRequested;
#elif WINDOWS_PHONE_APP
            Windows.Phone.UI.Input.HardwareButtons.BackPressed += HardwareButtons_BackPressed;
#endif

            Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            Windows.Storage.ApplicationDataContainer localSettings = Windows.Storage.ApplicationData.Current.LocalSettings;
            if (roamingSettings.Values["exact"] == null)
                roamingSettings.Values["exact"] = false;
            if (roamingSettings.Values["canchange"] == null)
                roamingSettings.Values["canchange"] = true;
            if (roamingSettings.Values["price"] == null)
                roamingSettings.Values["price"] = "100%";
            if (roamingSettings.Values["wait"] == null)
                roamingSettings.Values["wait"] = "20";
            if (roamingSettings.Values["change"] == null)
                roamingSettings.Values["change"] = "3";
            if (roamingSettings.Values["walk"] == null)
                roamingSettings.Values["walk"] = "5";
            if (roamingSettings.Values["reminder"] == null)
                roamingSettings.Values["reminder"] = "15";
            if (localSettings.Values["theme"] == null)
                localSettings.Values["theme"] = 0;
            if (localSettings.Values["alwaysupdate"] == null)
            {
                if (roamingSettings.Values["alwaysupdate"] != null)
                    localSettings.Values["alwaysupdate"] = roamingSettings.Values["alwaysupdate"];
                else
                    localSettings.Values["alwaysupdate"] = true;
            }
            if (roamingSettings.Values["sort"] == null)
                roamingSettings.Values["sort"] = 1;
            if (roamingSettings.Values["showhome"] == null)
                roamingSettings.Values["showhome"] = true;
            if (roamingSettings.Values["showlog"] == null)
                roamingSettings.Values["showlog"] = true;
            if (roamingSettings.Values["usagelog"] == null)
                roamingSettings.Values["usagelog"] = "0|0";
            if (localSettings.Values["frequency"] == null)
                localSettings.Values["frequency"] = (uint)60;
            if (localSettings.Values["frequency"] is String)
                localSettings.Values["frequency"] = (uint)60;
            if (localSettings.Values["history1"] == null)
                localSettings.Values["history1"] = "";
            if (localSettings.Values["history2"] == null)
                localSettings.Values["history2"] = "";
            if (localSettings.Values["history3"] == null)
                localSettings.Values["history3"] = "";
            if (localSettings.Values["history4"] == null)
                localSettings.Values["history4"] = "";
            if (localSettings.Values["lastlocation"] == null)
                localSettings.Values["lastlocation"] = "";
            if (localSettings.Values["lastupdate"] == null)
                localSettings.Values["lastupdate"] = DateTime.Today.AddDays(-1).Date.ToString();

            if ((int)localSettings.Values["theme"] == 1)
                RequestedTheme = ApplicationTheme.Dark;
            if ((int)localSettings.Values["theme"] == 2)
                RequestedTheme = ApplicationTheme.Light;

            InitializeBackgroundTasks((uint)localSettings.Values["frequency"], (string)localSettings.Values["version"]);
        }

        private static async void InitializeBackgroundTasks(uint frequency, string version)
        {
            if (version != VERSION)
            {
                BackgroundExecutionManager.RemoveAccess();
                //await BackgroundExecutionManager.RequestAccessAsync();
            }

            bool exists = false, exists2 = false;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (version != VERSION)
                    task.Value.Unregister(true);
                else
                {
                    if ((task.Value.Name != "ScheduledSecondaryTileUpdater" || exists2) && (task.Value.Name != "ScheduledTileUpdater" || exists))
                        task.Value.Unregister(true);
                    if (task.Value.Name == "ScheduledTileUpdater")
                        exists = true;
                    if (task.Value.Name == "ScheduledSecondaryTileUpdater")
                        exists2 = true;
                }
            }

            if (frequency != 0 && !exists) // automatically recurring activation of primary tile updater
            {
                var access = await BackgroundExecutionManager.RequestAccessAsync();

#if WINDOWS_UWP
                if (access != BackgroundAccessStatus.DeniedBySystemPolicy && access != BackgroundAccessStatus.DeniedByUser)
#elif WINDOWS_PHONE_APP
                if (access != BackgroundAccessStatus.Denied)
#endif
                {
                    BackgroundTaskBuilder builder2 = new BackgroundTaskBuilder();
                    builder2.Name = "ScheduledTileUpdater";
                    builder2.TaskEntryPoint = typeof(TileUpdater).FullName;
                    builder2.IsNetworkRequested = true;
                    TimeTrigger trigger = new TimeTrigger(frequency, false);
                    builder2.SetTrigger(trigger);
                    builder2.Register();
                }
            }

            if (frequency != 0 && !exists2) // automatically recurring activation of secondary tile updater
            {
                var access = await BackgroundExecutionManager.RequestAccessAsync();
#if WINDOWS_UWP
                if (access != BackgroundAccessStatus.DeniedBySystemPolicy && access != BackgroundAccessStatus.DeniedByUser)
#elif WINDOWS_PHONE_APP
                if (access != BackgroundAccessStatus.Denied)
#endif
                {
                    BackgroundTaskBuilder builder4 = new BackgroundTaskBuilder();
                    builder4.Name = "ScheduledSecondaryTileUpdater";
                    builder4.TaskEntryPoint = typeof(SecondaryTileUpdater).FullName;
                    builder4.IsNetworkRequested = true;
                    TimeTrigger trigger = new TimeTrigger(frequency, false);
                    builder4.SetTrigger(trigger);
                    builder4.Register();
                }
            }

#if WINDOWS_UWP
            // for manual activation of secondary tile update
            BackgroundTaskBuilder builder = new BackgroundTaskBuilder();
            builder.Name = "RunSecondaryTileUpdater";
            builder.TaskEntryPoint = typeof(SecondaryTileUpdater).FullName;
            trigger = new ApplicationTrigger();
            builder.SetTrigger(trigger);
            try { builder.Register(); } catch (Exception) { }
#endif
        }

        /// <summary>
        /// Invoked when the application is launched normally by the end user.  Other entry points
        /// will be used such as when the application is launched to open a specific file.
        /// </summary>
        /// <param name="e">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs e)
        {
            /*if (System.Diagnostics.Debugger.IsAttached)
            {
                this.DebugSettings.EnableFrameRateCounter = true;
            }*/
            Frame rootFrame = Window.Current.Content as Frame;

            // title bar customization
#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) // mobile
#endif
            {
                var statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.White;
                statusBar.BackgroundOpacity = 0;
            }
#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView")) // PC
            {
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = (Color)Current.Resources["SystemAccentColor"];
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.BackgroundColor = (Color)Current.Resources["SystemAccentColor"];
                titleBar.ForegroundColor = Colors.White;
            }

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseCoreWindow);
            else
                ApplicationView.GetForCurrentView().SetDesiredBoundsMode(ApplicationViewBoundsMode.UseVisible);
#endif

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

#if WINDOWS_UWP
                rootFrame.NavigationFailed += OnNavigationFailed;
#elif WINDOWS_PHONE_APP
                // TODO: change this value to a cache size that is appropriate for your application
                rootFrame.CacheSize = 1;

                // Set the default language
                rootFrame.Language = Windows.Globalization.ApplicationLanguages.Languages[0];
#endif

                if (e.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    //TODO: Load state from previously suspended application
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;

                rootFrame.SizeChanged += RootFrame_SizeChanged;
            }

            var lineSerializer = new Utilities.LineSerializer(Windows.ApplicationModel.Resources.ResourceLoader.GetForViewIndependentUse());
#if WINDOWS_UWP
            if (e.PrelaunchActivated == false)
#endif
            {
                if (e.Arguments == "opensearch")
                {
                    rootFrame.Navigate(typeof(Search));
                    rootFrame.BackStack.Clear();
                    rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
#if WINDOWS_UWP
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
#endif
                }
                else
                {
                    string[] tileData = e.TileId.Split('-'); // handle secondary tile
                    string[] tileData2 = e.Arguments.Split('|');
                    if (tileData[0] == "MenetrendApp")
                    {
                        Line line = await lineSerializer.openLine(tileData[1], tileData[2], tileData[3], tileData[4], tileData2[0], tileData2[1]);
                        if (!line.Error)
                        {
                            rootFrame.Navigate(typeof(Results), line);
                            rootFrame.BackStack.Clear();
                            rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
#if WINDOWS_UWP
                            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
#endif
                        }
                        else
                            rootFrame.Navigate(typeof(MainPage));
                    }
                    else
                    {
                        string[] lineData = e.Arguments.Split('-'); //uwp jumplist, wp8 notification
                        if (lineData.Length == 4)
                        {
                            Line line = await lineSerializer.openLine(lineData[0], lineData[1], lineData[2], lineData[3]);
                            if (!line.Error)
                            {
                                rootFrame.Navigate(typeof(Results), line);
                                rootFrame.BackStack.Clear();
                                rootFrame.BackStack.Add(new PageStackEntry(typeof(MainPage), null, null));
#if WINDOWS_UWP
                                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
#endif
                            }
                            else
                                rootFrame.Navigate(typeof(MainPage));
                        }
                    }
                }

                if (rootFrame.Content == null)
                {
#if WINDOWS_PHONE_APP
                    // Removes the turnstile navigation for startup.
                    if (rootFrame.ContentTransitions != null)
                    {
                        transitions = new Windows.UI.Xaml.Media.Animation.TransitionCollection();
                        foreach (var c in rootFrame.ContentTransitions)
                        {
                            transitions.Add(c);
                        }
                    }

                    rootFrame.ContentTransitions = null;
                    rootFrame.Navigated += this.RootFrame_FirstNavigated;       
#endif
                    // When the navigation stack isn't restored navigate to the first page,
                    // configuring the new page by passing required information as a navigation
                    // parameter
                    rootFrame.Navigate(typeof(MainPage), e.Arguments);
                }
                // Ensure the current window is active
                Window.Current.Activate();
            }

#if WINDOWS_UWP
            SystemNavigationManager snm = SystemNavigationManager.GetForCurrentView();
            snm.BackRequested += (sender, backReqEvArgs) =>
            {
                if (rootFrame.CanGoBack)
                {
                    backReqEvArgs.Handled = true;
                    rootFrame.GoBack();
                }
                snm.AppViewBackButtonVisibility = rootFrame.CanGoBack ?
                AppViewBackButtonVisibility.Visible :
                AppViewBackButtonVisibility.Collapsed;
            };
#endif
        }

        private async void RootFrame_SizeChanged(object sender, SizeChangedEventArgs e)
        {
#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar")) // mobile
#endif
            {
                var statusBar = StatusBar.GetForCurrentView();
                if (Windows.Graphics.Display.DisplayInformation.GetForCurrentView().CurrentOrientation == Windows.Graphics.Display.DisplayOrientations.Portrait)
                    await statusBar.ShowAsync();
                else
                    await statusBar.HideAsync();
            }
        }
        
        /// <summary>
        /// Invoked when application execution is being suspended.  Application state is saved
        /// without knowing whether the application will be terminated or resumed with the contents
        /// of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private void OnSuspending(object sender, SuspendingEventArgs e)
        {
            var deferral = e.SuspendingOperation.GetDeferral();
            //TODO: Save application state and stop any background activity
            deferral.Complete();
        }
    }
}
#endif