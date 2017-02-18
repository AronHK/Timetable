using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Net.Http;
using Windows.Foundation;

namespace Timetable
{
    [DataContract]
    public sealed class Line// : IEquatable<Line>
    {
        [DataMember]
        private string name;
        [DataMember]
        private DateTimeOffset lastUpdated;
        [DataMember]
        private IList<IDictionary<string, string>> buses; //todo: List<List<Dictionary<string, string>>> Buses // 0 - weekday, 1 - saturday, 2 - sunday
        [DataMember]
        private string fromsID;
        [DataMember]
        private string fromlsID;
        [DataMember]
        private string tosID;
        [DataMember]
        private string tolsID;
        [DataMember]
        private string from;
        [DataMember]
        private string to;
        private bool error;

        public string Name
        {
            get { return name; }
            set { name = value; }
        }
        public DateTimeOffset LastUpdated
        {
            get { return lastUpdated; }
            set { lastUpdated = value; }
        }
        public IList<IDictionary<string, string>> Buses
        {
            get { return buses; }
            set { buses = value; }
        }
        public string FromsID
        {
            get { return fromsID; }
            set { fromsID = value; }
        }
        public string TosID
        {
            get { return tosID; }
            set { tosID = value; }
        }
        public string FromlsID
        {
            get { return fromlsID; }
            set { fromlsID = value; }
        }
        public string TolsID
        {
            get { return tolsID; }
            set { tolsID = value; }
        }
        public string From
        {
            get { return from; }
            set { from = value; }
        }
        public string To
        {
            get { return to; }
            set { to = value; }
        }
        public bool Error
        {
            get { return error; }
            set { error = value; }
        }

        public Line(string FromsID, string TosID, string FromlsID, string TolsID, string from, string to)
        {
            Name = "";
            this.FromsID = FromsID;
            this.TosID = TosID;
            this.FromlsID = FromlsID;
            this.TolsID = TolsID;
            this.from = from;
            this.to = to;
            lastUpdated = DateTime.Today;
            buses = new List<IDictionary<string, string>>();
        }

        /*public Line(LineData data)
        {
            Name = data.Name;
            FromsID = data.FromsID;
            TosID = data.TosID;
            FromlsID = data.FromlsID;
            TolsID = data.TolsID;
            from = data.from;
            to = data.to;
            lastUpdated = data.lastUpdated;
            Buses = data.Buses;
            fromTile = false;
        }*/

        // unused // store fresh data about schedules on different days
        /*public async Task update()
        {
            hiba = false;
            int today = (int)DateTime.Today.DayOfWeek;
            if (today >= 1 && today <= 5) // when today is weekday
            {
                Task a = updateOn(0, DateTime.Today.Date.ToString("yyyy-MM-dd")); //weekday
                Task b = updateOn(1, DateTime.Today.AddDays(6 - today).Date.ToString("yyyy-MM-dd")); //saturday
                Task c = updateOn(2, DateTime.Today.AddDays(7 - today).Date.ToString("yyyy-MM-dd")); //sunday
                await Task.WhenAll(a, b, c);
            }
            else if (today == 6) // when today is saturday
            {
                Task a = updateOn(1, DateTime.Today.Date.ToString("yyyy-MM-dd"));
                Task b = updateOn(0, DateTime.Today.AddDays(2).Date.ToString("yyyy-MM-dd"));
                Task c = updateOn(2, DateTime.Today.AddDays(1).Date.ToString("yyyy-MM-dd"));
                await Task.WhenAll(a, b, c);
            }
            else // when today is sunday
            {
                Task a = updateOn(2, DateTime.Today.Date.ToString("yyyy-MM-dd"));
                Task b = updateOn(0, DateTime.Today.AddDays(1).Date.ToString("yyyy-MM-dd"));
                Task c = updateOn(1, DateTime.Today.AddDays(6).Date.ToString("yyyy-MM-dd"));
                await Task.WhenAll(a, b, c);
            }
        }*/

        //get schedules for a specidic day
        private async Task updateOn(int id, string date, bool allday, string hour, string minute)
        {
            buses = new List<IDictionary<string, string>>();

            string szak; // download specific or general data?
            if (allday)
                szak = "0";
            else
                szak = "3";

            string responseString;
            using (var client = new HttpClient())               // query the server for the bus-line based on LineData
            {
                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;
                var values = new Dictionary<string, string>
                {
                    { "datum", date },
                    { "erk_stype", "megallo" },
                    { "ext_settings", "block" },
                    { "filtering", "0" },
                    { "helyi", "No" },
                    { "honnan", from },
                    { "honnan_eovx", "0" },
                    { "honnan_eovy", "0" },
                    { "honnan_ls_id", FromlsID },
                    { "honnan_settlement_id", FromsID },
                    { "honnan_site_code", "0" },
                    { "honnan_zoom", "0" },
                    { "hour", hour },
                    { "hova", to },
                    { "hova_eovx", "0" },
                    { "hova_eovy", "0" },
                    { "hova_ls_id", TolsID },
                    { "hova_settlement_id", TosID },
                    { "hova_zoom", "0" },
                    { "ind_stype", "megallo" },
                    { "keresztul_stype", "megallo" },
                    { "maxatszallas", (String)roamingSettings.Values["change"] },
                    { "maxvar", (String)roamingSettings.Values["wait"] },
                    { "maxwalk", (String)roamingSettings.Values["walk"] },
                    { "min", minute },
                    { "napszak", szak },
                    { "naptipus", "0" },
                    { "odavissza", "0" },
                    { "preferencia", "1" },
                    { "rendezes", "1" },
                    { "submitted", "1" },
                    { "talalatok", "1" },
                    { "target", "0" },
                    { "utirany", "oda" },
                    { "var", "0" }
                };

                var content = new FormUrlEncodedContent(values);

                var response = await client.PostAsync("http://menetrendek.hu/uj_menetrend/hu/talalatok_json.php", content);
                responseString = await response.Content.ReadAsStringAsync();
            }

            string raw = Regex.Unescape(responseString);            // process recieved data
            string[] data = Regex.Split(raw, "\"talalatok\":{");

            if (data.Length < 2)
                error = true;
            else
            {
                Regex regex = new Regex("(?<={).+?(?=}}}})", RegexOptions.None);
                foreach (Match match in regex.Matches(data[1]))
                {
                    string toParse = match.ToString();
                    Dictionary<string, string> jarat = new Dictionary<string, string>();
                    jarat.Add("indulasi_hely", getproperty(toParse, "indulasi_hely"));
                    jarat.Add("erkezesi_hely", getproperty(toParse, "erkezesi_hely"));
                    jarat.Add("indulasi_ido", getproperty(toParse, "indulasi_ido"));
                    jarat.Add("erkezesi_ido", getproperty(toParse, "erkezesi_ido"));
                    jarat.Add("osszido", getproperty(toParse, "osszido"));
                    int runcount = Int32.Parse(getproperty(toParse, "runcount"));
                    string num = getproperty(toParse, "vonalnev", runcount);
                    num = num.Substring(num.IndexOf('|'));
                    num = (runcount == 1) ? num.Substring(1) + num : " ∙∙∙" + num;
                    jarat.Add("vonalnev", num);
                    jarat.Add("ossztav", getproperty(toParse, "ossztav"));
                    jarat.Add("fare", getproperty(toParse, "fare", runcount));
                    jarat.Add("fare_50_percent", getproperty(toParse, "fare_50_percent", runcount));
                    jarat.Add("fare_90_percent", getproperty(toParse, "fare_90_percent", runcount));
                    jarat.Add("extra", getproperty(toParse, "additional_ticket_price", runcount));
                    string det = getdetails(toParse).Replace(Convert.ToChar(0x0).ToString(), "");
                    jarat.Add("details", det);
                    Buses.Add(jarat); //Buses[id].Add(jarat)
                }

            }
        }

        public IAsyncAction updateOn(string date)
        {
            //await updateOn(0, date, true, DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString());
            return this.updateOn(0, date, true, DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString()).AsAsyncAction();
        }

        public IAsyncAction updateOn(string date, string hour, string minute)
        {
            //await updateOn(0, date, false, hour, minute);
            return this.updateOn(0, date, false, hour, minute).AsAsyncAction();
        }

        private string getproperty(string text, string key)
        {
            Match match = Regex.Match(text, "(?<=" + key + "\":(\")?)[^\"]*(?=(,\"|\",\"))");
            return match.Value;
        }

        private string getproperty(string text, string key, int j)
        {
            Match match = Regex.Match(text, "(?<=\"" + key + "\":(\")?)[^\"]*(?=(,\"|\",\"))");
            string result = "";
            int p = 0;
            for (int i = 0; i < j; i++)
            {
                result += "|" + match.Value;
                p += string.IsNullOrEmpty(match.Value) ? 0 : Int32.Parse(match.Value);
                match = match.NextMatch();
            }

            result = p.ToString() + result;
            return result;
        }

        private string getdetails(string text)
        {
            Match match = Regex.Match(text, "(?<=kifejtes_postjson\":{).*");
            return match.Value;
        }

        public sealed override bool Equals(object obj)
        {
            if (obj is Line)
                return EqualsLine((Line)obj);
            else
                return false;
        }

        public sealed override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + Int32.Parse(FromsID);
            hash = hash * 23 + Int32.Parse(FromlsID);
            hash = hash * 23 + Int32.Parse(TosID);
            hash = hash * 23 + Int32.Parse(TolsID);
            return hash;
        }

        private bool EqualsLine(Line other)
        {
            if (FromsID != other.FromsID)
                return false;
            if (FromlsID != other.FromlsID)
                return false;
            if (TosID != other.TosID)
                return false;
            if (TolsID != other.TolsID)
                return false;
            /*if (from != other.from)
                return false;
            if (to != other.to)
                return false;*/

            return true;
        }
    }

    public sealed class BusDetail
    {
        private int type; // 1 - START, 2 - END, 3 - WALK
        private string time;
        private string place;
        private string linenum;
        private string distance;
        private string price;
        private string duration;
        private string extra;
        private bool lowfloor;
        private string organisation;

        public int Type
        {
            get { return type; }
            set { type = value; }
        }
        public string Time
        {
            get { return time; }
            set { time = value; }
        }
        public string Place
        {
            get { return place; }
            set { place = value; }
        }
        public string Linenum
        {
            get { return linenum; }
            set { linenum = value; }
        }
        public string Distance
        {
            get { return distance; }
            set { distance = value; }
        }
        public string Price
        {
            get { return price; }
            set { price = value; }
        }
        public string Duration
        {
            get { return duration; }
            set { duration = value; }
        }
        public string Extra
        {
            get { return extra; }
            set { extra = value; }
        }
        public bool Lowfloor
        {
            get { return lowfloor; }
            set { lowfloor = value; }
        }
        public string Organisation
        {
            get { return organisation; }
            set { organisation = value; }
        }
    }
}
