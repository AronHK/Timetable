using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Windows.Networking.Connectivity;
using Windows.Services.Maps;
using Windows.Storage;
using Windows.UI.Notifications;

namespace Timetable
{
    public sealed partial class TileUpdater : IBackgroundTask
    {
        private async Task<string> GetLocation()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            string town = null;

            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                Geolocator geolocator = new Geolocator();
                geolocator.DesiredAccuracyInMeters = 1500;
                Geoposition pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
                BasicGeoposition basicpos = new BasicGeoposition();
                basicpos.Latitude = pos.Coordinate.Point.Position.Latitude;
                basicpos.Longitude = pos.Coordinate.Point.Position.Longitude; ;
                Geopoint point = new Geopoint(basicpos);

                MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(point);
                try { town = result.Locations[0].Address.Town; }
                catch (ArgumentOutOfRangeException) { town = null; }
            }

            return town;
        }

        private XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            return TileData.getXML(name, num, fromtime, from, totime, to, showtomorrow);
        }
    }
}
