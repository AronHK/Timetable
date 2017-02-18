using Windows.ApplicationModel.Resources;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace Timetable
{
    static class TileData
    {
        public static XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();
            int baseindex = 1;
            string tomorrow = " ";
            if (showtomorrow)
            {
                baseindex = 2;
                tomorrow = resourceLoader.GetString("CardTomorrowLong");
            }

            XmlDocument tileXml = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWide310x150Text01);
            XmlNodeList tileTextAttributes = tileXml.GetElementsByTagName("text");
            tileTextAttributes[0].AppendChild(tileXml.CreateTextNode(num + " " + name));
            tileTextAttributes[1].AppendChild(tileXml.CreateTextNode(tomorrow));
            if (showtomorrow)
                tileTextAttributes[2].AppendChild(tileXml.CreateTextNode(" "));
            tileTextAttributes[baseindex + 1].AppendChild(tileXml.CreateTextNode(fromtime + " " + from));
            tileTextAttributes[baseindex + 2].AppendChild(tileXml.CreateTextNode(totime + " " + to));

            XmlDocument tileXml2 = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquare150x150Text01);
            XmlNodeList tileTextAttributes2 = tileXml2.GetElementsByTagName("text");
            tileTextAttributes2[0].AppendChild(tileXml2.CreateTextNode(num + " " + name));
            tileTextAttributes2[baseindex].AppendChild(tileXml2.CreateTextNode(tomorrow));
            tileTextAttributes2[baseindex + 1].AppendChild(tileXml2.CreateTextNode(fromtime + " " + from));
            tileTextAttributes2[baseindex + 2].AppendChild(tileXml2.CreateTextNode(totime + " " + to));

            IXmlNode node = tileXml.ImportNode(tileXml2.GetElementsByTagName("binding").Item(0), true);
            tileXml.GetElementsByTagName("visual").Item(0).AppendChild(node);

            return tileXml;
        }
    }
}
