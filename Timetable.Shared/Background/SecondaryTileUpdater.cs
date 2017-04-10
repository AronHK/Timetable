#if BACKGROUND
using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.Networking.Connectivity;
using Windows.Storage;
using Windows.UI.Notifications;
using Windows.UI.StartScreen;

namespace Timetable
{
    public sealed partial class SecondaryTileUpdater : IBackgroundTask
    {
        ResourceLoader resourceLoader;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            resourceLoader = ResourceLoader.GetForViewIndependentUse();
            Utilities.LineSerializer lineSerializer = new Utilities.LineSerializer(resourceLoader);
            //IList<Line> savedLines = await lineSerializer.readLines();

            var profile = NetworkInformation.GetInternetConnectionProfile();
            NetworkCostType cost = NetworkCostType.Unknown;
            if (profile != null)
                cost = profile.GetConnectionCost().NetworkCostType;

            // get secondary tiles
            var tiles = await SecondaryTile.FindAllForPackageAsync();
            if (tiles.Count != 0)
            {
                foreach (SecondaryTile tile in tiles) // get data from each tile
                {
                    string[] tileData = tile.TileId.Split('-');
                    string[] tileData2 = tile.Arguments.Split('|');
                    Line line = await lineSerializer.openLine(tileData[1], tileData[2], tileData[3], tileData[4], tileData2[0], tileData2[1]);


                    if (((bool)localSettings.Values["alwaysupdate"] || cost == NetworkCostType.Unrestricted) && line.LastUpdated < DateTime.Today.Date) // update only if necessary
                    {
                        try { await line.updateOn(); }
                        catch (System.Net.Http.HttpRequestException) { return; }
                        line.LastUpdated = DateTime.Today.Date;

                        await lineSerializer.saveLine(line);
                    }

                    Windows.UI.Notifications.TileUpdater updatemngr = TileUpdateManager.CreateTileUpdaterForSecondaryTile(tile.TileId);
                    if (updatemngr.GetScheduledTileNotifications().Count == 0 || (updatemngr.GetScheduledTileNotifications().Count > 0 && updatemngr.GetScheduledTileNotifications()[0].DeliveryTime < DateTime.Today)) // if scheduled updates are outdated or nonexistent
                    {
                        updatemngr.Clear();

                        if (!line.Error)
                        {
                            if (line.Buses.Count > 0)
                            {
                                string prevfromtime = "";
                                bool first = true;
                                for (int i = 0; i < line.Buses.Count; i++)   // get Buses from the line
                                {
                                    string fromtime, num, from, to;
                                    line.Buses[i].TryGetValue("vonalnev", out num);
                                    line.Buses[i].TryGetValue("indulasi_ido", out fromtime);
                                    line.Buses[i].TryGetValue("indulasi_hely", out from);
                                    line.Buses[i].TryGetValue("erkezesi_hely", out to);
                                    num = num.Split('|')[0];

                                    if (DateTime.ParseExact(fromtime, "HH:mm", CultureInfo.InvariantCulture) >= DateTime.Now && !(!(bool)roamingSettings.Values["canchange"] && num == " ∙∙∙")
                                        && !((bool)roamingSettings.Values["exact"] && ((line.From != from && line.From.Contains(",")) || (line.To != to && line.To.Contains(",")))))
                                    {
                                        //found = true;
                                        string name, totime;
#if WINDOWS_UWP
                                        name = line.Name;
#elif WINDOWS_PHONE_APP
                                        name = "";
#endif
                                        line.Buses[i].TryGetValue("erkezesi_ido", out totime);

                                        XmlDocument xmlDoc = getXML(name, num, fromtime, from, totime, to, false);

                                        DateTime showUpdateAt;
                                        if (first)
                                        {
                                            showUpdateAt = DateTime.Now.AddSeconds(1);
                                            first = false;
                                        }
                                        else
                                            showUpdateAt = DateTime.Parse(prevfromtime).AddSeconds(30);

                                        ScheduledTileNotification scheduledUpdate = new ScheduledTileNotification(xmlDoc, new DateTimeOffset(showUpdateAt));
                                        scheduledUpdate.ExpirationTime = new DateTimeOffset(DateTime.Today.AddDays(1).AddHours(1));
                                        updatemngr.AddToSchedule(scheduledUpdate);

                                        prevfromtime = fromtime;
                                    }
                                }
                            }
                            if (updatemngr.GetScheduledTileNotifications().Count == 0) // if there are no Buses today
                            {
                                try { await line.updateOn(DateTime.Today.AddDays(1)); }
                                catch (System.Net.Http.HttpRequestException) { return; }

                                if (!line.Error && line.Buses.Count > 0)
                                {
                                    int i = 0;
                                    string num, from, to;
                                    do
                                    {
                                        string fromtime, name, totime;
#if WINDOWS_UWP
                                        name = line.Name;
#elif WINDOWS_PHONE_APP
                                        name = "";
#endif
                                        line.Buses[i].TryGetValue("erkezesi_ido", out totime);
                                        line.Buses[i].TryGetValue("indulasi_ido", out fromtime);
                                        line.Buses[i].TryGetValue("vonalnev", out num);
                                        line.Buses[i].TryGetValue("indulasi_hely", out from);
                                        line.Buses[i].TryGetValue("erkezesi_hely", out to);
                                        num = num.Split('|')[0];

                                        if (!(!(bool)roamingSettings.Values["canchange"] && num == " ∙∙∙") && !((bool)roamingSettings.Values["exact"] && ((line.From != from && line.From.Contains(",")) || (line.To != to && line.To.Contains(",")))))
                                        {
                                            XmlDocument xmlDoc = getXML(name, num, fromtime, from, totime, to, true);
                                            DateTime showUpdateAt = DateTime.Now.AddSeconds(1);

                                            ScheduledTileNotification scheduledUpdate = new ScheduledTileNotification(xmlDoc, new DateTimeOffset(showUpdateAt));
                                            scheduledUpdate.ExpirationTime = new DateTimeOffset(DateTime.Today.AddDays(1).AddHours(1));
                                            updatemngr.AddToSchedule(scheduledUpdate);
                                        }

                                        i++;
                                    } while (i < line.Buses.Count && ((!(bool)roamingSettings.Values["canchange"] && num == " ∙∙∙") || ((bool)roamingSettings.Values["exact"] && ((line.From != from && line.From.Contains(",")) || (line.To != to && line.To.Contains(","))))));
                                }
                            }
                        }
                    }
                }
            }
            deferral.Complete();
        }
    }
}
#endif