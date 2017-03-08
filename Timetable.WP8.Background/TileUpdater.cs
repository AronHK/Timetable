using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;

namespace Timetable
{
    public sealed partial class TileUpdater : IBackgroundTask
    {
        private XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            return Utilities.TileData.getXML(name, num, fromtime, from, totime, to, showtomorrow);
        }
    }
}
