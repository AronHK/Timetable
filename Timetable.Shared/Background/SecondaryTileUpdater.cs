#if BACKGROUND
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
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
            LineSerializer lineSerializer = new LineSerializer(resourceLoader);
            IList<Line> savedLines = await lineSerializer.readLines();
            
            var cost = NetworkInformation.GetInternetConnectionProfile().GetConnectionCost().NetworkCostType;

            // get secondary tiles
            var tiles = await SecondaryTile.FindAllForPackageAsync();
            if (tiles.Count != 0)
            {
                foreach (SecondaryTile tile in tiles) // get data from each tile
                {
                    string[] tileData = tile.TileId.Split('-');
                    string[] tileData2 = tile.Arguments.Split('|');

                    foreach (Line line in savedLines) // find and update matching line
                    {
                        if (tileData[1] == line.FromsID && tileData[2] == line.FromlsID && tileData[3] == line.TosID && tileData[4] == line.TolsID)
                        {
                            if (((bool)roamingSettings.Values["alwaysupdate"] || cost == NetworkCostType.Unrestricted) && line.LastUpdated < DateTime.Today.Date) // update only if necessary
                            {
                                try { await line.updateOn(DateTime.Today.Date.ToString("yyyy-MM-dd")); }
                                catch (System.Net.Http.HttpRequestException) { return; }
                                line.LastUpdated = DateTime.Today.Date;

                                await lineSerializer.writeLines(savedLines);
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
                                                name = line.Name;
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
                                                scheduledUpdate.ExpirationTime = DateTime.Today.AddDays(1).AddHours(3);
                                                updatemngr.AddToSchedule(scheduledUpdate);

                                                prevfromtime = fromtime;
                                            }
                                        }
                                    }
                                    if (updatemngr.GetScheduledTileNotifications().Count == 0) // if there are no Buses today
                                    {
                                        try { await line.updateOn(DateTime.Today.AddDays(1).Date.ToString("yyyy-MM-dd")); }
                                        catch (System.Net.Http.HttpRequestException) { return; }

                                        if (!line.Error && line.Buses.Count > 0)
                                        {
                                            int i = 0;
                                            string num, from, to;
                                            do
                                            {
                                                string fromtime, name, totime;
                                                name = line.Name;
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
                                                    scheduledUpdate.ExpirationTime = DateTime.Today.AddDays(1).AddHours(3);
                                                    updatemngr.AddToSchedule(scheduledUpdate);
                                                }

                                                i++;
                                            } while ((!(bool)roamingSettings.Values["canchange"] && num == " ∙∙∙") || ((bool)roamingSettings.Values["exact"] && ((line.From != from && line.From.Contains(",")) || (line.To != to && line.To.Contains(",")))));
                                        }
                                    }
                                }
                            }

                            break;
                        }
                    }
                }
            }
            deferral.Complete();
        }
    }
}
#endif