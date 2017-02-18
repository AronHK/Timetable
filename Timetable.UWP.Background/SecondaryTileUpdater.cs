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
        private XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            return TileData.getXML(name, num, fromtime, from, totime, to, showtomorrow);
        }
    }
}
