using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;
using Windows.UI.Notifications;

namespace Timetable
{
    public sealed partial class TileUpdater : IBackgroundTask
    {
        private async Task<string> GetLocation()
        {
            string town = null;

            try
            {
                Geolocator geolocator = new Geolocator();
                Geoposition pos = await geolocator.GetGeopositionAsync();
                BasicGeoposition basicpos = new BasicGeoposition();
                basicpos.Latitude = pos.Coordinate.Point.Position.Latitude;
                basicpos.Longitude = pos.Coordinate.Point.Position.Longitude; ;
                Geopoint point = new Geopoint(basicpos);

                MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(point);
                try { town = result.Locations[0].Address.Town; }
                catch (ArgumentOutOfRangeException) { town = null; }
            }
            catch (Exception) { return null; }

            return town;
        }

        private XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            return TileData.getXML(name, num, fromtime, from, totime, to, showtomorrow);
        }
    }
}
