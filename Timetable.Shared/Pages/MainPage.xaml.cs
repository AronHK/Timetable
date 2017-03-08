#if !BACKGROUND
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
using Windows.Foundation.Metadata;
using Windows.UI.Xaml.Navigation;
using Windows.Devices.Geolocation;
using Windows.System.Profile;
using Windows.Networking.Connectivity;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Timetable
{
    /// <summary>
    /// Start Page, saved lines.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private IList<Line> savedLines;
        private IReadOnlyList<SecondaryTile> tiles;
        private ResourceLoader resourceLoader;
        private Utilities.LineSerializer lineSerializer;

        private async Task getSavedData(bool update, int toupdate = -1)
        {
            // LOAD SAVED DATA
            lineSerializer = new Utilities.LineSerializer(ResourceLoader.GetForViewIndependentUse());
            savedLines = await lineSerializer.readLines();
            //LineList.ItemsSource = null;
            LineList.Items.Clear();

            //DISPLAY DATA
            if (savedLines.Count == 0) // no data   
            {
                error.Visibility = Visibility.Visible;
                title.Visibility = Visibility.Collapsed;
                inprogress.IsActive = false;
                inprogressbg.Visibility = Visibility.Collapsed;
                inprogresstext.Visibility = Visibility.Collapsed;
                //error.Focus(FocusState.Programmatic);
#if WINDOWS_UWP
                if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Xbox")
                {
                    Loaded += (s, e) =>
                    {
                        Frame.Navigate(typeof(Search), null, new EntranceNavigationTransitionInfo());
                        SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
                    };
                }
#endif
            }
            else                       // create cards
            {
                error.Visibility = Visibility.Collapsed;
                title.Visibility = Visibility.Visible;

                inprogress.IsActive = true;
                inprogressbg.Visibility = Visibility.Visible;
                AppbarUpdate.IsEnabled = false;

                /*var gridVisual = ElementCompositionPreview.GetElementVisual(this.myGrid);
                var compositor = gridVisual.Compositor;
                this.effectVisual = compositor.CreateSpriteVisual();
                GaussianBlurEffect blurEffect = new GaussianBlurEffect()
                {
                    BorderMode = EffectBorderMode.Hard, // NB: default mode here isn't supported yet.
                    Source = new CompositionEffectSourceParameter("source")
                };
                var effectFactory = compositor.CreateEffectFactory(blurEffect);
                var effectBrush = effectFactory.CreateBrush();
                effectBrush.SetSourceParameter("source", compositor.CreateBackdropBrush());
                this.effectVisual.Brush = effectBrush;
                ElementCompositionPreview.SetElementChildVisual(this.myGrid, this.effectVisual);*/

                string tomorrow;
                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

                //List<Card> toAdd = new List<Card>();
                for (int i = 0; i < savedLines.Count; i++) // for each line
                {
                    int toDisplay = -1;
                    tomorrow = "";
                    for (int k = 0; k < 2; k++) // try next day as well if there are no more Buses today
                    {
                        toDisplay = -1;
                        DateTime updateday = DateTime.Today.Date.AddDays(k);
                        var cost = NetworkInformation.GetInternetConnectionProfile().GetConnectionCost().NetworkCostType;
                        if (update || toupdate == i || (((bool)roamingSettings.Values["alwaysupdate"] || cost == NetworkCostType.Unrestricted) && savedLines[i].LastUpdated < updateday)) // update if we haven't updated yet today
                        {
                            inprogresstext.Visibility = Visibility.Visible;
                            savedLines[i].Buses = null;
                            try
                            {
                                await savedLines[i].updateOn(updateday);
                                savedLines[i].LastUpdated = updateday;
                                await lineSerializer.saveLine(savedLines[i]);
                            }
                            catch (HttpRequestException)
                            {
                                inprogress.IsActive = false;
                                inprogresstext.Visibility = Visibility.Collapsed;
                                AppbarUpdate.IsEnabled = true;
                                var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                                dialog.Commands.Add(new UICommand("OK"));
                                dialog.CancelCommandIndex = 0;
                                dialog.DefaultCommandIndex = 0;
                                await dialog.ShowAsync();
                                return;
                            }

                        }
                        for (int j = 0; j < savedLines[i].Buses.Count && savedLines[i].LastUpdated >= DateTime.Today; j++) // get first bus after current time (for each line)
                        {
                            string start, name, from, to;
                            savedLines[i].Buses[j].TryGetValue("indulasi_ido", out start);
                            savedLines[i].Buses[j].TryGetValue("vonalnev", out name);
                            savedLines[i].Buses[j].TryGetValue("indulasi_hely", out from);
                            savedLines[i].Buses[j].TryGetValue("erkezesi_hely", out to);
                            if ((TimeSpan.Parse(start).TotalSeconds >= DateTime.Now.TimeOfDay.TotalSeconds || savedLines[i].LastUpdated > DateTime.Today) && !(!(bool)roamingSettings.Values["canchange"] && name.Split('|')[0] == " ∙∙∙"))
                            {
                                if (!((bool)roamingSettings.Values["exact"] && ((savedLines[i].From != from && savedLines[i].From.Contains(",")) || (savedLines[i].To != to && savedLines[i].To.Contains(",")))))
                                {
                                    toDisplay = j;
                                    break;
                                }
                            }
                        }
                        if (savedLines[i].LastUpdated > DateTime.Today && toDisplay != -1)
                            tomorrow = resourceLoader.GetString("CardTomorrowLong");
                        if (toDisplay > -1)
                            break;
                        if (savedLines[i].LastUpdated > DateTime.Today)
                            k++;
                        /*if (!(bool)roamingSettings.Values["alwaysupdate"] && cost != NetworkCostType.Unrestricted && (toupdate != i || !update))
                        {
                            if (k == 0 && toDisplay == -1 && savedLines[i].LastUpdated <= DateTime.Today)
                                savedLines[i].LastUpdated = DateTime.Today.AddDays(-1);
                            k++;
                        }*/
                    }
                    //if (toDisplay == -1)
                    //    savedLines[i].LastUpdated = DateTime.ParseExact("1000.01.01", "yyyy.MM.dd", System.Globalization.CultureInfo.InvariantCulture);
                    Card newcard = new Card(savedLines[i], toDisplay);    // create card for next bus
                    newcard.LineName = tomorrow + newcard.LineName;

                    if (toDisplay != -1 || savedLines[i].LastUpdated < DateTime.Today)
                    {
                        int num = i;

                        newcard.Tapped += (s, e) => OpenLine(newcard, num);
                        //newcard.KeyUp += LineList_KeyUp;
                    }

                    // Context menu
                    tiles = await SecondaryTile.FindAllForPackageAsync();
#if WINDOWS_UWP
                    if (!ApiInformation.IsPropertyPresent("Windows.UI.Xaml.UIElement", "ContextFlyout"))
#endif
                    {
                        newcard.RightTapped += (s, e) =>
                        {
                            var contextMenu = CreateContextMenu(newcard);
                            var point = e.GetPosition((Card)s);
                            contextMenu.ShowAt(newcard/*, point*/);
                        };
                    }
#if WINDOWS_UWP
                    else
                        newcard.ContextFlyout = CreateContextMenu(newcard);
#endif

                    //toAdd.Add(newcard);
                    LineList.Items.Add(newcard);
                }
                LineList.SelectedIndex = 0;
                inprogresstext.Visibility = Visibility.Collapsed;
                //LineList.ItemsSource = toAdd;

                inprogress.IsActive = false;
                inprogressbg.Visibility = Visibility.Collapsed;
                inprogresstext.Visibility = Visibility.Collapsed;
                AppbarUpdate.IsEnabled = true;
            }

            WindowResized(null, null);
        }

        private MenuFlyout CreateContextMenu(Card card)
        {
            MenuFlyout contextMenu = new MenuFlyout();
            MenuFlyoutItem item1 = new MenuFlyoutItem();
            MenuFlyoutItem item2 = new MenuFlyoutItem();
            MenuFlyoutItem item3 = new MenuFlyoutItem();

            item1.Text = resourceLoader.GetString("Delete");
            item3.Text = resourceLoader.GetString("Rename");
#if WINDOWS_UWP
            if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                item2.Text = resourceLoader.GetString("PinMenu");
            else
#endif
                item2.Text = resourceLoader.GetString("PinScreen");

            bool remove = false;
            foreach (var tile in tiles)
            {
                string[] tileData = tile.TileId.Split('-');
                string[] tileData2 = tile.Arguments.Split('|');
                if (tileData[1] == card.ParentLine.FromsID && tileData[2] == card.ParentLine.FromlsID && tileData[3] == card.ParentLine.TosID && tileData[4] == card.ParentLine.TolsID)
                {
#if WINDOWS_UWP
                    if (!ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                        item2.Text = resourceLoader.GetString("UnpinMenu");
                    else
#endif
                        item2.Text = resourceLoader.GetString("UnpinScreen");
                    remove = true;
                    break;
                }
            }

            item1.Click += (s, ev) => DeleteLine(s, ev, card);
            item2.Click += (s, ev) => PinLine(s, ev, card, remove);
            item3.Click += (s, ev) => RenameLine(s, ev, card);

            contextMenu.Items.Add(item3);
            contextMenu.Items.Add(item1);
#if WINDOWS_UWP
            if (ApiInformation.IsMethodPresent("Windows.UI.Notifications.TileUpdater", "AddToSchedule"))
#endif
                contextMenu.Items.Add(item2);

            return contextMenu;
        }

        // rename line
        private async void RenameLine(object s, RoutedEventArgs ev, Card sender)
        {
            int error = 0;

            var dialog = new ContentDialog() { Title = resourceLoader.GetString("Linename") };
            TextBox namebox = new TextBox();
            namebox.Text = sender.ParentLine.Name;
            namebox.Height = 32;
            namebox.SelectionStart = namebox.Text.Length;
            namebox.KeyDown += async (sndr, args) =>
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
                        sender.ParentLine.Name = namebox.Text.Trim();
                        await lineSerializer.saveLine(sender.ParentLine);
                        await getSavedData(false);
                    }
                }
            };
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
                    sender.ParentLine.Name = namebox.Text.Trim();
                    await lineSerializer.saveLine(sender.ParentLine);
                    await getSavedData(false);
                }
            };

            var result = await dialog.ShowAsync();
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

        // open line
        private async void OpenLine(Card sender, int i)
        {
            if (sender.ParentLine.LastUpdated >= DateTime.Today.Date)
            {
                Frame.Navigate(typeof(Results), sender.ParentLine);
#if WINDOWS_UWP
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
#endif
            }
            else
            {
                sender.ShowUpdateIndicator();
                try
                {
                    //await sender.ParentLine.updateOn(DateTime.Today.Date.ToString("yyyy-MM-dd"));
                    //sender.ParentLine.lastUpdated = DateTime.Today;
                    //await LineSerializer.writeLines(savedLines);
                    await getSavedData(false, i);
                }
                catch (HttpRequestException)
                {
                    sender.ShowUpdateIndicator();
                    var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                    dialog.Commands.Add(new UICommand("OK"));
                    dialog.CancelCommandIndex = 0;
                    dialog.DefaultCommandIndex = 0;
                    await dialog.ShowAsync();
                }
            }
        }

        // pin to start
        private async void PinLine(object sender, RoutedEventArgs e, Card card, bool remove)
        {
            string tileID = $@"MenetrendApp-{card.ParentLine.FromsID}-{card.ParentLine.FromlsID}-{card.ParentLine.TosID}-{card.ParentLine.TolsID}";
            SecondaryTile tile = new SecondaryTile(tileID, card.ParentLine.Name, $@"{card.ParentLine.From}|{card.ParentLine.To}|{card.ParentLine.Name}", new Uri("ms-appx:///Assets/Wide310x150Secondary.scale-200.png"), TileSize.Wide310x150);

            if (remove)
                try { await tile.RequestDeleteAsync(); } catch (Exception) { }
            else
            {
                var visual = tile.VisualElements;
                visual.ShowNameOnSquare150x150Logo = true;
                visual.ShowNameOnWide310x150Logo = true;
                visual.Square150x150Logo = new Uri("ms-appx:///Assets/Square150x150Secondary.scale-200.png");
                visual.Wide310x150Logo = new Uri("ms-appx:///Assets/Wide310x150Secondary.scale-200.png");
                visual.Square71x71Logo = new Uri("ms-appx:///Assets/Square70x70Secondary.scale-100.png");

                await tile.RequestCreateAsync();

#if WINDOWS_PHONE_APP
                XmlDocument tileXml = Utilities.TileData.getXML("", card.LineNumber, card.StartTime, card.From, card.EndTime, card.To, false);
                TileNotification notif = new TileNotification(tileXml);
                TileUpdateManager.CreateTileUpdaterForSecondaryTile(tileID).Update(notif);
#elif WINDOWS_UWP
                await App.trigger.RequestAsync(); // run tile updater
#endif
            }
        }

        private async void DeleteLine(object sender, RoutedEventArgs e, Card toDelete)
        {
            var dialog = new MessageDialog(resourceLoader.GetString("DeleteVerify"));

            dialog.Commands.Add(new UICommand(resourceLoader.GetString("Yes"), async (command) =>
            {
                PinLine(this, null, toDelete, true);
                await lineSerializer.removeLine(toDelete.ParentLine);
                await getSavedData(false);
            }));
            dialog.Commands.Add(new UICommand(resourceLoader.GetString("No")));

            dialog.CancelCommandIndex = 1;
            dialog.DefaultCommandIndex = 1;

            await dialog.ShowAsync();
        }

        private async void Update(object sender, RoutedEventArgs e)
        {
            await getSavedData(true);
        }
    }
}
#endif