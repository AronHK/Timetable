using Windows.Data.Xml.Dom;
using Windows.ApplicationModel.Resources;

namespace Timetable
{
    static class TileData
    {
        public static XmlDocument getXML(string name, string num, string fromtime, string from, string totime, string to, bool showtomorrow)
        {
            ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();

            string tomorrow = "";
            string tomorrowlong = "";
            string tomorrowshort = "";
            if (showtomorrow)
            {
                tomorrow = resourceLoader.GetString("CardTomorrow");
                tomorrowshort = resourceLoader.GetString("CardTomorrowShort");
                tomorrowlong = resourceLoader.GetString("CardTomorrowLong");
            }
            string widenum = "";
            string titleweight = "2";
            if (num == " ∙∙∙")
                titleweight = "3";
            if (num != "")
                widenum = $@"<subgroup hint-weight='1'> <text hint-style='subheader'>{num}</text></subgroup>";
            string fromshort = from;
            if (from.Contains(","))
                fromshort = from.Split(',')[1].Trim();

            string content = $@"
                <tile>
                    <visual branding='none'>
                        <binding template='TileSmall'>
                            <text hint-style='base'>{fromtime}</text>
                            <text hint-style='captionSubtle'>{tomorrowshort}{name}</text>
                            <text hint-style='captionSubtle'>{fromshort}</text>
                        </binding>
                        <binding template='TileMedium' hint-textStacking='center'>
                            <group>
                                <subgroup>
                                    <text hint-style='titleNumeral'>{num}</text>
                                </subgroup>
                                <subgroup>
                                    <text hint-style='captionSubtle'>{tomorrow}</text>
                                </subgroup>
                            </group>
                            <text hint-style='bodySubtle'>{name}</text>
                            <text hint-style='caption'>{fromtime} {from}</text>
                            <text hint-style='caption'>{totime} {to}</text>
                        </binding>
                        <binding template='TileWide'
                            hint-lockDetailedStatus1='{num} {name}'
                            hint-lockDetailedStatus2='{fromtime} {from}'
                            hint-lockDetailedStatus3='{totime} {to}'>
                            <group>
                                {widenum}
                                <subgroup hint-weight='{titleweight}'>
                                    <text hint-style='captionSubtle'>{tomorrowlong}</text>
                                    <text hint-style='baseSubtle'>{name}</text>
                                </subgroup>
                            </group>
                            <group>
                                <subgroup hint-weight='1'>
                                    <text hint-style='base'>{fromtime} </text>
                                </subgroup>
                                <subgroup hint-weight='3'>
                                    <text hint-style='body'>{from}</text>
                                </subgroup>
                            </group>
                            <group>
                                <subgroup hint-weight='1'>
                                    <text hint-style='base'>{totime} </text>
                                </subgroup>
                                <subgroup hint-weight='3'>
                                    <text hint-style='body'>{to}</text>
                                </subgroup>
                            </group>
                        </binding>
                    </visual>
                </tile>";

            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(content);
            return xmlDoc;
        }
    }
}
