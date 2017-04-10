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

namespace Timetable
{
    public sealed partial class TileUpdater : IBackgroundTask
    {
        ResourceLoader resourceLoader;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;

            resourceLoader = ResourceLoader.GetForViewIndependentUse();
            
            //main tile
            Windows.UI.Notifications.TileUpdater mainmngr = TileUpdateManager.CreateTileUpdaterForApplication();

            string town = null;

            if ((bool?)localSettings.Values["location"] != false)
                town = await Utilities.LocationFinder.GetLocation();
           
            foreach (var tile in mainmngr.GetScheduledTileNotifications())
                mainmngr.RemoveFromSchedule(tile);
            mainmngr.Clear();

            var profile = NetworkInformation.GetInternetConnectionProfile();
            NetworkCostType cost = NetworkCostType.Unknown;
            if (profile != null)
                cost = profile.GetConnectionCost().NetworkCostType;

            if (town != null)
            {
                Utilities.LineSerializer lineSerializer = new Utilities.LineSerializer(resourceLoader);
                IList<Line> linesfromhere = await lineSerializer.readLinesFrom(town);
                List<int> busindices = new List<int>();

                // update lines
                foreach (var line in linesfromhere)
                {
                    if (((bool)localSettings.Values["alwaysupdate"] || cost == NetworkCostType.Unrestricted) && line.LastUpdated < DateTime.Today.Date)
                    {
                        try { await line.updateOn(DateTime.Today); }
                        catch (System.Net.Http.HttpRequestException) { return; }
                        line.LastUpdated = DateTime.Today.Date;

                        await lineSerializer.saveLine(line);
                    }

                    busindices.Add(0);
                }

                bool done = false;
                int minind;
                string prevfromtime = "";
                bool first = true;
                
                // go through all possible lines
                while (!done)
                {
                    DateTime mintime = DateTime.Today.AddDays(1);
                    minind = -1;

                    for (int j = 0; j < linesfromhere.Count; j++) // find next time
                    {
                        if (linesfromhere[j].Buses.Count < 1 && j < linesfromhere.Count - 1)
                            j++;
                        else if (linesfromhere[j].Buses.Count < 1 && j == linesfromhere.Count - 1)
                            break;

                        int i = busindices[j];
                        if (i != -1 && i < linesfromhere[j].Buses.Count)
                        {
                            string fromtime, num, from, to;
                            linesfromhere[j].Buses[i].TryGetValue("vonalnev", out num);
                            linesfromhere[j].Buses[i].TryGetValue("indulasi_ido", out fromtime);
                            linesfromhere[j].Buses[i].TryGetValue("indulasi_hely", out from);
                            linesfromhere[j].Buses[i].TryGetValue("erkezesi_hely", out to);
                            num = num.Split('|')[0];

                            var fromtime_date = DateTime.ParseExact(fromtime, "HH:mm", CultureInfo.InvariantCulture);

                            if (fromtime_date >= DateTime.Now && !(!(bool)roamingSettings.Values["canchange"] && num == " ∙∙∙") &&
                                !((bool)roamingSettings.Values["exact"] && ((linesfromhere[j].From != from && linesfromhere[j].From.Contains(",")) || (linesfromhere[j].To != to && linesfromhere[j].To.Contains(",")))))
                            {
                                if (fromtime_date < mintime)
                                {
                                    minind = j;
                                    mintime = fromtime_date;
                                }
                            }
                            else if (linesfromhere[j].Buses.Count - 1 > busindices[j])
                            {
                                busindices[j]++;
                                j--;
                            }
                            else
                                busindices[j] = -1;
                        }
                    }

                    if (minind != -1)
                    {
                        int i = busindices[minind];
                        string fromtime, num, from, to, name, totime;
                        name = linesfromhere[minind].Name;
                        linesfromhere[minind].Buses[i].TryGetValue("erkezesi_ido", out totime);
                        linesfromhere[minind].Buses[i].TryGetValue("vonalnev", out num);
                        linesfromhere[minind].Buses[i].TryGetValue("indulasi_ido", out fromtime);
                        linesfromhere[minind].Buses[i].TryGetValue("indulasi_hely", out from);
                        linesfromhere[minind].Buses[i].TryGetValue("erkezesi_hely", out to);
                        num = num.Split('|')[0];

                        XmlDocument xmlDoc = getXML(name, num, fromtime, from, totime, to, false);

                        DateTime showUpdateAt;
                        if (first)
                        {
                            showUpdateAt = DateTime.Now.AddSeconds(5);
                            first = false;
                        }
                        else
                            showUpdateAt = DateTime.Parse(prevfromtime).AddSeconds(30);

                        ScheduledTileNotification scheduledUpdate = new ScheduledTileNotification(xmlDoc, new DateTimeOffset(showUpdateAt));
                        scheduledUpdate.ExpirationTime = new DateTimeOffset(DateTime.Today.AddDays(1).AddHours(1));
                        mainmngr.AddToSchedule(scheduledUpdate);

                        prevfromtime = fromtime;
                        if (busindices[minind] != -1 && linesfromhere[minind].Buses.Count - 1 > busindices[minind])
                            busindices[minind]++;
                        else
                            busindices[minind] = -1;
                    }
                    
                    done = true;
                    for (int i = 0; i < busindices.Count; i++)
                    {
                        if (busindices[i] == linesfromhere[i].Buses.Count)
                            busindices[i] = -1;
                        if (busindices[i] != -1)
                            done = false;
                    }
                }
            }

            deferral.Complete();
        }
    }
}
#endif