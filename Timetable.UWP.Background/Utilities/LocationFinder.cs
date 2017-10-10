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
            var accessStatus = await Geolocator.RequestAccessAsync();
            string town = null;

            if (accessStatus == GeolocationAccessStatus.Allowed)
            {
                try
                {
                    MapService.ServiceToken = "lxJiVzjPvY4QkfSEgGkJ~N1FxApXmFRQDiReZBeDlfg~AsJ7th7S8_VQhANRLPT4G0IJCk-T_sUZzRgcRFzl5XjkYrEOvJg5jwUpdXv75xEA";
                    Geolocator geolocator = new Geolocator();
                    geolocator.DesiredAccuracyInMeters = 1500;
                    Geoposition pos = await geolocator.GetGeopositionAsync(TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(1));
                    BasicGeoposition basicpos = new BasicGeoposition();
                    basicpos.Latitude = pos.Coordinate.Point.Position.Latitude;
                    basicpos.Longitude = pos.Coordinate.Point.Position.Longitude; ;
                    Geopoint point = new Geopoint(basicpos);

                    MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync(point);
                    town = result.Locations[0].Address.Town;
                }
                catch (Exception) { town = null; }
            }

            return town;
        }

        private static async Task<bool> DoIsLocationAllowed()
        {
            var accessStatus = await Geolocator.RequestAccessAsync();
            return accessStatus == GeolocationAccessStatus.Allowed;
        }
    }
}
