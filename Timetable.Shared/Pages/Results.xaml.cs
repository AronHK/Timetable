#if !BACKGROUND
using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Globalization;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Net.Http;
using Windows.UI.StartScreen;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Profile;
using Windows.UI.Xaml.Media.Animation;
using Timetable.Utilities;

namespace Timetable
{
    /// <summary>
    /// Search results.
    /// </summary>
    public sealed partial class Results : Page
    {
        private Line line;
        private Windows.Storage.ApplicationDataContainer roamingSettings;
        private string[] timedata;
        private ResourceLoader resourceLoader;
        private Card toShare;
        private LineSerializer lineSerializer;

        public Results()
        {
            this.InitializeComponent();
            DataTransferManager.GetForCurrentView().DataRequested += DataRequested;
            ApplicationView.GetForCurrentView().VisibleBoundsChanged += WindowResized;
            roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
            resourceLoader = ResourceLoader.GetForCurrentView();
            lineSerializer = new LineSerializer(ResourceLoader.GetForViewIndependentUse());
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
#endif
            {
                StatusBar statusBar = StatusBar.GetForCurrentView();
                statusBar.ForegroundColor = Colors.White;
            }

#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.ApplicationView")) // PC
            {
                title.Foreground = new SolidColorBrush(Colors.White);
                title2.Foreground = new SolidColorBrush(Colors.White);
                var titleBar = ApplicationView.GetForCurrentView().TitleBar;
                titleBar.ButtonBackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.BackgroundColor = (Color)Application.Current.Resources["SystemAccentColor"];
                titleBar.ForegroundColor = Colors.White;
                Window.Current.Activated += WindowActivated;
            }

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                Appbar.Visibility = Visibility.Collapsed;
                Appbar2.Visibility = Visibility.Collapsed;
                XboxPanel.Visibility = Visibility.Visible;
                mainpanel.Margin = new Thickness(48, 10, 48, 27);
                LineList.Margin = new Thickness(0, 0, 0, 42);
            }
#endif

            if (e.Parameter is string[])    // create line from search data and display it [entry point from SEARCH]
            {
#if WINDOWS_UWP
                XboxSave.Visibility = Visibility.Visible;
#endif
                if (Frame.BackStack.Count > 1)
                    Frame.BackStack.RemoveAt(Frame.BackStack.Count - 1);
                string[] results = (string[])e.Parameter;
                timedata = new string[] { results[6], results[7], results[8] };
                line = new Line(results[0], results[1], results[2], results[3], results[4], results[5]);
                try { await line.updateOn(timedata[0], timedata[1], timedata[2]); }
                catch (HttpRequestException)
                {
                    var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                    dialog.Commands.Add(new UICommand("OK", (command) => { Frame.GoBack(); }));
                    dialog.CancelCommandIndex = 0;
                    dialog.DefaultCommandIndex = 0;
                    try { await dialog.ShowAsync(); } catch (UnauthorizedAccessException) { }
                    return;
                }
            }
            else if (e.Parameter is Line)   // [entry point from MAIN PAGE, TILE or TOAST]
            {
                line = (Line)e.Parameter;
                title.Text = line.Name == "" ? resourceLoader.GetString("ResultTitleSaved") + resourceLoader.GetString("Unnamed") : resourceLoader.GetString("ResultTitleSaved") + line.Name;

                AppbarSave.Visibility = Visibility.Collapsed;
                AppbarUnsave.Visibility = Visibility.Visible;
                AppbarPin.Visibility = Visibility.Visible;
                AppbarRename.Visibility = Visibility.Visible;
#if WINDOWS_UWP
                AppbarSave2.Visibility = Visibility.Collapsed;
                AppbarUnsave2.Visibility = Visibility.Visible;
                AppbarPin2.Visibility = Visibility.Visible;
                AppbarRename2.Visibility = Visibility.Visible;
                XboxUpdate.Visibility = Visibility.Visible;

                title2.Text = line.Name == "" ? resourceLoader.GetString("ResultTitleSaved") + resourceLoader.GetString("Unnamed") : resourceLoader.GetString("ResultTitleSaved") + line.Name;
#endif
                // is the line pinned?
                var tiles = await SecondaryTile.FindAllForPackageAsync();
                foreach (var tile in tiles)
                {
                    string[] tileData = tile.TileId.Split('-');
                    string[] tileData2 = tile.Arguments.Split('|');
                    if (tileData[1] == line.FromsID && tileData[2] == line.FromlsID && tileData[3] == line.TosID && tileData[4] == line.TolsID)
                    {
                        AppbarPin.Visibility = Visibility.Collapsed;
                        AppbarUnPin.Visibility = Visibility.Visible;
#if WINDOWS_UWP
                        AppbarPin2.Visibility = Visibility.Collapsed;
                        AppbarUnPin2.Visibility = Visibility.Visible;
#endif
                    }
                }
            }

#if WINDOWS_UWP
            if (!ApiInformation.IsMethodPresent("Windows.UI.Notifications.TileUpdater", "AddToSchedule"))
            {
                AppbarPin.Visibility = Visibility.Collapsed;
                AppbarPin2.Visibility = Visibility.Collapsed;
            }
#endif
            if (await lineSerializer.LineExists(line))
            {
                AppbarSave.IsEnabled = false;
#if WINDOWS_UWP
                AppbarSave2.IsEnabled = false;
#endif
            }

            if (line.Error)
            {
                var dialog = new MessageDialog(resourceLoader.GetString("NotfoundError"));
                dialog.Commands.Add(new UICommand("OK", (command) => { Frame.GoBack(); }));
                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 0;
                await dialog.ShowAsync();
            }
            else
                sortLines((int)roamingSettings.Values["sort"]);

            WindowResized(null, null);

            if ((int)roamingSettings.Values["sort"] == 1)
                SortingOption1.IsChecked = true;
            if ((int)roamingSettings.Values["sort"] == 2)
                SortingOption2.IsChecked = true;
            if ((int)roamingSettings.Values["sort"] == 3)
                SortingOption3.IsChecked = true;
            if ((int)roamingSettings.Values["sort"] == 4)
                SortingOption4.IsChecked = true;
            inprogress.IsActive = false;
            AppbarUpdate.IsEnabled = true;
            AppbarSave.IsEnabled = true;
            AppbarSort.IsEnabled = true;
            AppbarUnsave.IsEnabled = true;
#if WINDOWS_UWP
            if ((int)roamingSettings.Values["sort"] == 1)
                SortingOption21.IsChecked = true;
            if ((int)roamingSettings.Values["sort"] == 2)
                SortingOption22.IsChecked = true;
            if ((int)roamingSettings.Values["sort"] == 3)
                SortingOption23.IsChecked = true;
            if ((int)roamingSettings.Values["sort"] == 4)
                SortingOption24.IsChecked = true;
            AppbarUpdate2.IsEnabled = true;
            AppbarSave2.IsEnabled = true;
            AppbarSort2.IsEnabled = true;
            AppbarUnsave2.IsEnabled = true;
#endif
        }

        public void Sort(object sender, RoutedEventArgs e)
        {
            SortingOption1.IsChecked = false;
            SortingOption2.IsChecked = false;
            SortingOption3.IsChecked = false;
            SortingOption4.IsChecked = false;
#if WINDOWS_UWP
            SortingOption21.IsChecked = false;
            SortingOption22.IsChecked = false;
            SortingOption23.IsChecked = false;
            SortingOption24.IsChecked = false;
#endif
            ((ToggleMenuFlyoutItem)sender).IsChecked = true;

            LineList.Items.Clear();
            sortLines(Int32.Parse((string)((ToggleMenuFlyoutItem)sender).Tag));
#if WINDOWS_UWP
            sortmode = Int32.Parse((string)((ToggleMenuFlyoutItem)sender).Tag);
#endif
        }

        private async void Save(object sender, RoutedEventArgs e)
        {
            if (await lineSerializer.LineExists(line))   // is line unique?
            {
                var conflict = new MessageDialog(resourceLoader.GetString("SaveError"), resourceLoader.GetString("Error"));
                conflict.Commands.Add(new UICommand("OK"));
                conflict.DefaultCommandIndex = 0;
                await conflict.ShowAsync();
                return;
            }

            int error = 1;         // is the entered name allowed?

            var dialog = new ContentDialog() { Title = resourceLoader.GetString("Linename") };
            //StackPanel panel = new StackPanel();
            TextBox name = new TextBox();
            name.Margin = new Thickness(0, 10, 0, 0);
            name.Height = 32;
            name.KeyUp += async (sndr, args) =>
            {
                if (args.Key == Windows.System.VirtualKey.Enter)
                {
                    AppbarSave.IsEnabled = false;
#if WINDOWS_UWP
                    AppbarSave2.IsEnabled = false;
#endif

                    if (name.Text.Trim() == "")
                        dialog.Title = resourceLoader.GetString("LinenameError");
                    else if (name.Text.Trim().Length > 30)
                        dialog.Title = resourceLoader.GetString("LinenameLengthError");
                    else
                    {
                        dialog.Hide();
                        error = 0;
                        line.Name = name.Text.Trim();
                        await lineSerializer.saveLine(line);
#if WINDOWS_UWP
                        XboxSave.Visibility = Visibility.Collapsed;
                        XIcon.Visibility = Visibility.Collapsed;
#endif
                    }
                }
            };
            //panel.Children.Add(name);
            dialog.Content = name;
            dialog.PrimaryButtonText = "OK";
            dialog.PrimaryButtonClick += async delegate
            {
                AppbarSave.IsEnabled = false;
#if WINDOWS_UWP
                AppbarSave2.IsEnabled = false;
#endif

                if (name.Text.Trim() == "")
                    error = 1;
                else if (name.Text.Trim().Length > 30)
                    error = 2;
                else
                {
                    error = 0;
                    line.Name = name.Text.Trim();
                    await lineSerializer.saveLine(line);
#if WINDOWS_UWP
                    XboxSave.Visibility = Visibility.Collapsed;
                    XIcon.Visibility = Visibility.Collapsed;
#endif
                }
            };
            dialog.Opened += delegate { name.Focus(FocusState.Programmatic); };

            while (error != 0)
            {
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.None && error != 0)
                {
                    AppbarSave.IsEnabled = true;
#if WINDOWS_UWP
                    AppbarSave2.IsEnabled = true;
#endif
                    error = 0;
                }
                if (error == 1)
                    dialog.Title = resourceLoader.GetString("LinenameError");
                if (error == 2)
                    dialog.Title = resourceLoader.GetString("LinenameLengthError");
            }
        }

        private void sortLines(int s)
        {
            List<Card> toDisplay = new List<Card>();
            error.Visibility = Visibility.Collapsed;

            if (line.LastUpdated < DateTime.Today)
                Update(this, null);

            for (int i = 0; i < line.Buses.Count; i++)
            {
                string name, from, to;
                line.Buses[i].TryGetValue("vonalnev", out name);
                line.Buses[i].TryGetValue("indulasi_hely", out from);
                line.Buses[i].TryGetValue("erkezesi_hely", out to);
                if (!(!(bool)roamingSettings.Values["canchange"] && name.Split('|')[0] == " ∙∙∙"))
                {
                    if (!((bool)roamingSettings.Values["exact"] && ((line.From != from && line.From.Contains(",")) || (line.To != to && line.To.Contains(",")))))
                    {
                        Card newcard = new Card(line, i);
                        newcard.LineName = "";

#if WINDOWS_UWP
                        if (!ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ContextFlyout"))
#endif
                        {
                            newcard.RightTapped += (sender, e) =>
                            {
                                MenuFlyout contextMenu = CreateContextMenu(newcard);
                                var point = e.GetPosition((Card)sender);
                                contextMenu.ShowAt(newcard/*, point*/);
                            };
                        }
#if WINDOWS_UWP
                        else
                            newcard.ContextFlyout = CreateContextMenu(newcard);
#endif

                        newcard.Tapped += CardClicked;
                        toDisplay.Add(newcard);
                    }
                }
            }

            DateTime jumpto;
            if (timedata != null)
                jumpto = DateTime.Parse($"{DateTime.Today.Year}-{DateTime.Today.Month}-{DateTime.Today.Day} {timedata[1]}:{timedata[2]}");
            else
                jumpto = DateTime.Now;
            int pos = -1;
            int linenum = toDisplay.Count;
            for (int i = 0; i < linenum; i++)
            {
                if (s == 1)                             // DEFAULT SORTING
                {
                    LineList.Items.Add(toDisplay[i]);
                    if (DateTime.ParseExact(toDisplay[i].StartTime, "HH:mm", CultureInfo.InvariantCulture) <= jumpto) // times before now, show the next one
                        pos = i;
                }
                else
                {
                    Card min = toDisplay[0];            // CUSTOM SORTING
                    for (int j = 0; j < toDisplay.Count; j++)
                    {
                        string travelcurrent = toDisplay[j].TravelTime.Split(' ')[2];
                        string travelmin = min.TravelTime.Split(' ')[2];
                        if (s == 2 && TimeSpan.Parse(toDisplay[j].StartTime) + TimeSpan.Parse(travelcurrent) < TimeSpan.Parse(min.StartTime) + TimeSpan.Parse(travelmin))
                            min = toDisplay[j];
                        if (s == 3 && TimeSpan.Parse(travelcurrent) < TimeSpan.Parse(travelmin))
                            min = toDisplay[j];
                        if (s == 4 && Double.Parse(toDisplay[j].Distance.Split(' ')[0], CultureInfo.InvariantCulture) < Double.Parse(min.Distance.Split(' ')[0], CultureInfo.InvariantCulture))
                            min = toDisplay[j];
                    }
                    LineList.Items.Add(min);
                    if (DateTime.ParseExact(min.StartTime, "HH:mm", CultureInfo.InvariantCulture) <= jumpto)
                        pos = i;
                    toDisplay.Remove(min);
                }
            }
            
            if (line.LastUpdated <= DateTime.Now)
            {
                if ((s == 1 || s == 2) && LineList.Items.Count > 0)
                {
                    LineList.SelectedIndex = (pos == LineList.Items.Count - 1 ? pos : pos + 1);
                    if (pos != linenum - 1)
                        pos++;
                    LineList.ScrollIntoView(LineList.Items[LineList.Items.Count - 1]);
                    LineList.ScrollIntoView(LineList.Items[pos]);
                    LineList.UpdateLayout();
#if WINDOWS_PHONE_APP
                    current = (Card)LineList.Items[pos];
#endif
                }
            }

            if (LineList.Items.Count == 0)
                error.Visibility = Visibility.Visible;

            WindowResized(null, null);
        }

        private MenuFlyout CreateContextMenu(Card card)
        {
            MenuFlyout contextMenu = new MenuFlyout();
            MenuFlyoutItem item1 = new MenuFlyoutItem();
            MenuFlyoutItem item3 = new MenuFlyoutItem();
#if WINDOWS_UWP
            MenuFlyoutItem item2 = new MenuFlyoutItem();
            MenuFlyoutItem item4 = new MenuFlyoutItem();
            MenuFlyoutItem item5 = new MenuFlyoutItem();
#endif

            item1.Text = resourceLoader.GetString("SetupReminder");
            item3.Text = resourceLoader.GetString("Share");
#if WINDOWS_UWP
            item2.Text = resourceLoader.GetString("Copy");
            item4.Text = resourceLoader.GetString("DeleteLine");
            item5.Text = resourceLoader.GetString("RenameLine");
#endif

            item1.Click += (sender, ev) => SetupReminder(sender, ev, card);
            item3.Click += (sender, ev) => ShareLine(sender, ev, card);
#if WINDOWS_UWP
            item2.Click += (sender, ev) => CopyLine(sender, ev, card);
            item4.Click += (sender, ev) => Unsave(sender, ev);
            item5.Click += (sender, ev) => Rename(sender, ev);

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
            {
                contextMenu.Items.Add(item4);
                contextMenu.Items.Add(item5);
                contextMenu.Items.Add(new MenuFlyoutSeparator());
            }
            if (ApiInformation.IsTypePresent("Windows.UI.Notifications.ToastNotificationManager"))
#endif
                contextMenu.Items.Add(item1);
#if WINDOWS_UWP
            contextMenu.Items.Add(item2);
#endif
            contextMenu.Items.Add(item3);

            return contextMenu;
        }

        private async void CardClicked(object sender, TappedRoutedEventArgs e)
        {
            StatusBar statusBar = null;
            ContentDialog popup = new ContentDialog();
#if WINDOWS_PHONE_APP
            popup.Title = resourceLoader.GetString("DetailsTitleString");
#endif
            popup.Content = new Details((Card)sender, ActualHeight);
            popup.PrimaryButtonText = resourceLoader.GetString("Closepanel");
            popup.IsPrimaryButtonEnabled = true;
#if WINDOWS_UWP
            
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
#endif
            {
                statusBar = StatusBar.GetForCurrentView();
                popup.FullSizeDesired = true;
#if WINDOWS_UWP
                titlebg.Fill = Resources["SystemAltHighColor"] as SolidColorBrush;
#endif
                if (Application.Current.RequestedTheme == ApplicationTheme.Light)
                    statusBar.ForegroundColor = Colors.Black;
            }
#if WINDOWS_UWP
            else
            {
                popup.MinHeight = ActualHeight * 0.7;
                popup.MaxHeight = ActualHeight * 0.7;
                popup.MinWidth = 460;
                popup.MaxWidth = 460;
            }
#endif

            await popup.ShowAsync();

#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
#endif
            {
                statusBar.ForegroundColor = Colors.White;
#if WINDOWS_UWP
                titlebg.Fill = Resources["SystemControlBackgroundAccentBrush"] as SolidColorBrush;
#endif
            }
        }

        private void ShareLine(object s, RoutedEventArgs ev, Card sender)
        {
            DataTransferManager manager = DataTransferManager.GetForCurrentView();
            toShare = sender;
            DataTransferManager.ShowShareUI();
        }

        private void DataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataRequest request = args.Request;
            request.Data.Properties.Title = resourceLoader.GetString("AppName");
            request.Data.Properties.Description = resourceLoader.GetString("Share");
            if (toShare.LineNumber != "")
            {
                request.Data.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat($@"<p style='font-family:Calibri,Sans-Serif'><span style='font-size:120%'><b>{toShare.LineNumber}</b></span><br /><b>{toShare.StartTime}</b> {toShare.From}<br /><b>{toShare.EndTime}</b> {toShare.To}</p>"));
                request.Data.SetText($@"{toShare.LineNumber.Trim()}, {toShare.StartTime} {toShare.From} - {toShare.EndTime} {toShare.To}");
            }
            else
            {
                request.Data.SetHtmlFormat(HtmlFormatHelper.CreateHtmlFormat($@"<p style='font-family:Calibri,Sans-Serif'><b>{toShare.StartTime}</b> {toShare.From}<br /><b>{toShare.EndTime}</b> {toShare.To}</p>"));
                request.Data.SetText($@"{toShare.StartTime} {toShare.From} - {toShare.EndTime} {toShare.To}");
            }
        }

        public async void SetupReminder(object s, RoutedEventArgs ev, Card sender)
        {
            int time = 0;
            bool repeat = false;
            var dialog = new ContentDialog() { Title = resourceLoader.GetString("SetupReminder") };

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            TextBlock label2 = new TextBlock();
            label2.Text = resourceLoader.GetString("Reminder2");
            label2.Margin = new Thickness(10, 7, 0, 0);
            TextBox namebox = new TextBox();
            namebox.Text = (string)roamingSettings.Values["reminder"];
            namebox.Width = 20;
            namebox.Height = 32;
            namebox.SelectionStart = namebox.Text.Length;
            namebox.KeyDown += (sndr, arg) =>
            {
                if (arg.Key == Windows.System.VirtualKey.Enter)
                {
                    repeat = true;
                    try { time = Int32.Parse(namebox.Text); } catch (FormatException) { time = 0; }
                    dialog.Hide();
                }
            };
            InputScope scope = new InputScope();
            InputScopeName scopename = new InputScopeName();
            scopename.NameValue = InputScopeNameValue.Number;
            scope.Names.Add(scopename);
            namebox.InputScope = scope;

            panel.Children.Add(namebox);
            panel.Children.Add(label2);
            dialog.Content = panel;

            dialog.PrimaryButtonText = "OK";
            dialog.PrimaryButtonClick += delegate
            {
                try { time = Int32.Parse(namebox.Text); } catch (FormatException) { time = 0; }
            };
            dialog.Opened += delegate { namebox.Focus(FocusState.Programmatic); };

            while (time == 0)
            {
                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.None && !repeat)
                {
                    time = -1;
                    return;
                }
                repeat = false;
                dialog.Title = resourceLoader.GetString("BadInput");
                if (DateTime.Parse(sender.StartTime).AddMinutes(-time) <= DateTime.Now)
                {
                    time = 0;
                    dialog.Title = resourceLoader.GetString("EarlyError");
                }
                else if (time <= 0)
                {
                    time = 0;
                    dialog.Title = resourceLoader.GetString("NumError");
                }
            }

            roamingSettings.Values["reminder"] = namebox.Text;

            string args = $@"{line.FromsID}-{line.FromlsID}-{line.TosID}-{line.TolsID}";
#if WINDOWS_UWP
            string reminder = resourceLoader.GetString("Reminder");
            string content = $@"
                <toast launch='{args}' scenario='reminder'>
                    <visual>
                        <binding template='ToastGeneric'>
                            <text> {reminder + sender.ParentLine.Name} </text>
                            <text> {sender.StartTime} {sender.From}</text>
                            <text> {sender.EndTime} {sender.To}</text>
                        </binding>
                    </visual>
                    <actions>
                        <action content='' activationType='system' arguments='dismiss' />
                    </actions>
                </toast>";
#elif WINDOWS_PHONE_APP
            string content = $@"
                <toast launch='{args}'>
                    <visual>
                        <binding template='ToastText02'>
                            <text id='1'> {sender.ParentLine.Name} </text>
                            <text id='2'> {sender.StartTime} {sender.From}</text>
                        </binding>
                    </visual>
                </toast>";
#endif
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content);
            ScheduledToastNotification toast = new ScheduledToastNotification(xmlDoc, DateTime.Parse(sender.StartTime).AddMinutes(-time));
            ToastNotificationManager.CreateToastNotifier().AddToSchedule(toast);
        }

        private async void Rename(object sender, RoutedEventArgs e)
        {
            int error = 0;

            var dialog = new ContentDialog() { Title = resourceLoader.GetString("Linename") };
            TextBox namebox = new TextBox();
            //StackPanel panel = new StackPanel();
            namebox.Text = line.Name;
            namebox.Height = 32;
            namebox.SelectionStart = namebox.Text.Length;
            namebox.KeyUp += async (sndr, args) =>
            {
                if (args.Key == Windows.System.VirtualKey.Enter)
                {
                    if (namebox.Text.Trim() == "")
                        dialog.Title = resourceLoader.GetString("LinenameError");
                    else if (namebox.Text.Trim().Length > 30)
                        dialog.Title = resourceLoader.GetString("LinenameLengthError");
                    else
                    {
                        dialog.Hide();
                        error = 0;
                        line.Name = namebox.Text.Trim();
                        title.Text = line.Name == "" ? resourceLoader.GetString("ResultTitleSaved") + resourceLoader.GetString("Unnamed") : resourceLoader.GetString("ResultTitleSaved") + line.Name;
#if WINDOWS_UWP
                        title2.Text = line.Name == "" ? resourceLoader.GetString("ResultTitleSaved") + resourceLoader.GetString("Unnamed") : resourceLoader.GetString("ResultTitleSaved") + line.Name;
#endif
                        await lineSerializer.saveLine(line);
                    }
                }
            };
            //panel.Children.Add(namebox);
            dialog.Content = namebox;
            dialog.PrimaryButtonText = "OK";
            dialog.PrimaryButtonClick += async delegate
            {
                if (namebox.Text.Trim() == "")
                    error = 1;
                else if (namebox.Text.Trim().Length > 30)
                    error = 2;
                else
                {
                    error = 0;
                    line.Name = namebox.Text.Trim();
                    title.Text = line.Name == "" ? resourceLoader.GetString("ResultTitleSaved") + resourceLoader.GetString("Unnamed") : resourceLoader.GetString("ResultTitleSaved") + line.Name;
#if WINDOWS_UWP
                    title2.Text = line.Name == "" ? resourceLoader.GetString("ResultTitleSaved") + resourceLoader.GetString("Unnamed") : resourceLoader.GetString("ResultTitleSaved") + line.Name;
#endif
                    await lineSerializer.saveLine(line);
                }
            };
            dialog.Opened += delegate { namebox.Focus(FocusState.Programmatic); };

            var result = await dialog.ShowAsync();
            namebox.Focus(FocusState.Keyboard);
            if (result == ContentDialogResult.None)
                error = 0;
            while (error != 0)
            {
                if (error == 1)
                    dialog.Title = resourceLoader.GetString("LinenameError");
                if (error == 2)
                    dialog.Title = resourceLoader.GetString("LinenameLengthError");
                result = await dialog.ShowAsync();
                if (result == ContentDialogResult.None)
                    error = 0;
            }
        }

        private async void Unsave(object sender, RoutedEventArgs e)
        {
            var dialog = new MessageDialog(resourceLoader.GetString("DeleteVerify"));

            dialog.Commands.Add(new UICommand(resourceLoader.GetString("Yes"), async (command) =>
            {
                AppbarUnsave.Visibility = Visibility.Collapsed;
                AppbarPin.Visibility = Visibility.Collapsed;
                AppbarRename.Visibility = Visibility.Collapsed;
                AppbarSave.Visibility = Visibility.Visible;
#if WINDOWS_UWP
                AppbarUnsave2.Visibility = Visibility.Collapsed;
                AppbarPin2.Visibility = Visibility.Collapsed;
                AppbarRename2.Visibility = Visibility.Collapsed;
                AppbarSave2.Visibility = Visibility.Visible;
#endif
                UnPin(this, null);
                await lineSerializer.removeLine(line);
                Frame.GoBack();
            }));
            dialog.Commands.Add(new UICommand(resourceLoader.GetString("No")));

            dialog.CancelCommandIndex = 1;
            dialog.DefaultCommandIndex = 1;

            await dialog.ShowAsync();
        }

        private async void Update(object sender, RoutedEventArgs e)
        {
            inprogress.IsActive = true;
            AppbarUpdate.IsEnabled = false;
            AppbarSave.IsEnabled = false;
            AppbarSort.IsEnabled = false;
            AppbarUnsave.IsEnabled = false;
#if WINDOWS_UWP
            AppbarUpdate2.IsEnabled = false;
            AppbarSave2.IsEnabled = false;
            AppbarSort2.IsEnabled = false;
            AppbarUnsave2.IsEnabled = false;
#endif
            LineList.Items.Clear();
            try
            {
                if (timedata != null)
                    await line.updateOn(timedata[0], timedata[1], timedata[2]);
                else
                {
                    await line.updateOn(DateTime.Today);
                    line.LastUpdated = DateTime.Today;
                    await lineSerializer.saveLine(line);
                }
            }
            catch (HttpRequestException)
            {
                inprogress.IsActive = false;
                AppbarUpdate.IsEnabled = true;
                AppbarUnsave.IsEnabled = true;
#if WINDOWS_UWP
                AppbarUpdate2.IsEnabled = true;
                AppbarUnsave2.IsEnabled = true;
#endif
                var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                dialog.Commands.Add(new UICommand("OK", (command) => { Frame.GoBack(); }));
                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 0;
                try { await dialog.ShowAsync(); } catch (UnauthorizedAccessException) { }
                return;
            }
            if (line.Error)
            {
                var dialog = new MessageDialog(resourceLoader.GetString("NotfoundError"));
                dialog.Commands.Add(new UICommand("OK", (command) => { Frame.GoBack(); }));
                dialog.CancelCommandIndex = 0;
                dialog.DefaultCommandIndex = 0;
                try { await dialog.ShowAsync(); } catch (UnauthorizedAccessException) { }
            }
            else
                sortLines((int)roamingSettings.Values["sort"]);

            inprogress.IsActive = false;
            AppbarUpdate.IsEnabled = true;
            AppbarSave.IsEnabled = true;
            AppbarSort.IsEnabled = true;
            AppbarUnsave.IsEnabled = true;
#if WINDOWS_UWP
            AppbarUpdate2.IsEnabled = true;
            AppbarSave2.IsEnabled = true;
            AppbarSort2.IsEnabled = true;
            AppbarUnsave2.IsEnabled = true;
#endif
        }

        private async void Pin(object sender, RoutedEventArgs e)
        {
            string tileID = $@"MenetrendApp-{line.FromsID}-{line.FromlsID}-{line.TosID}-{line.TolsID}";
            SecondaryTile tile = new SecondaryTile(tileID, line.Name, $@"{line.From}|{line.To}|{line.Name}", new Uri("ms-appx:///Assets/Wide310x150Secondary.scale-200.png"), TileSize.Wide310x150);

            var visual = tile.VisualElements;
            visual.ShowNameOnSquare150x150Logo = true;
            visual.ShowNameOnWide310x150Logo = true;
            visual.Square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Secondary.scale-200.png");
            visual.Wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Secondary.scale-200.png");
            visual.Square71x71Logo = new Uri("ms-appx:///Assets/Square70x70Secondary.scale-100.png");

            await tile.RequestCreateAsync();
            AppbarPin.Visibility = Visibility.Collapsed;
            AppbarUnPin.Visibility = Visibility.Visible;
#if WINDOWS_UWP
            AppbarPin2.Visibility = Visibility.Collapsed;
            AppbarUnPin2.Visibility = Visibility.Visible;

            await App.trigger.RequestAsync(); // run tile updater
#elif WINDOWS_PHONE_APP
            XmlDocument tileXml = Utilities.TileData.getXML("", current.LineNumber, current.StartTime, current.From, current.EndTime, current.To, false);
            TileNotification notif = new TileNotification(tileXml);
            TileUpdateManager.CreateTileUpdaterForSecondaryTile(tileID).Update(notif);
#endif
        }

        private async void UnPin(object sender, RoutedEventArgs e)
        {
            string tileID = $@"MenetrendApp-{line.FromsID}-{line.FromlsID}-{line.TosID}-{line.TolsID}";
            SecondaryTile tile = new SecondaryTile(tileID);
            try
            {
                await tile.RequestDeleteAsync();
                AppbarPin.Visibility = Visibility.Visible;
                AppbarUnPin.Visibility = Visibility.Collapsed;
#if WINDOWS_UWP
                AppbarPin2.Visibility = Visibility.Visible;
                AppbarUnPin2.Visibility = Visibility.Collapsed;
#endif
            }
            catch (Exception) { }
        }
    }
}
#endif