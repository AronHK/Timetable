using System;
using System.Threading.Tasks;
using Windows.Devices.Geolocation;
using Windows.Foundation;
using Windows.Services.Maps;

namespace Timetable.Utilities
{
    public static class LocationFinder
    {
        public static IAsyncOperation<string> GetLocation()
        {
            return DoGetLocation().AsAsyncOperation();
        }

        public static IAsyncOperation<bool> IsLocationAllowed()
        {
            return DoIsLocationAllowed().AsAsyncOperation();
        }

        private static async Task<string> DoGetLocation()
        {
            string town = null;

            try
            {
                MapService.ServiceToken = "sTHkEbHCyiH_iqm85_CC9A";
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
            catch (Exception) { return null; }

            return town;
        }

        private static async Task<bool> DoIsLocationAllowed()
        {
            Geolocator geolocator = new Geolocator();
            bool allowed = true;

            try { Geoposition pos = await geolocator.GetGeopositionAsync(); }
            catch (Exception) { allowed = false; }

            return allowed;
        }
    }
}
