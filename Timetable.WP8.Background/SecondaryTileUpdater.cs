using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Timetable
{
    public sealed partial class SecondaryTileUpdater : IBackgroundTask
    {
        private XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            return TileData.getXML(name, num, fromtime, from, totime, to, showtomorrow);
        }
    }
}
