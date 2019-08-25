#if BACKGROUND
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Runtime.Serialization;
using System.Net.Http;
using Windows.Foundation;
using Windows.Data.Json;

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
            this.lastUpdated = DateTime.Today;
            this.buses = new List<IDictionary<string, string>>();
            this.error = false;
        }

        public Line()
        {
            error = false;
        }

        //get schedules for a specidic day
        private async Task DoUpdateOn(string date, string hour, string minute, bool forcelimited = false)
        {
            buses = new List<IDictionary<string, string>>();
            error = false;
            string responseString;
            string napszak = forcelimited ? "3" : "0";

            using (var client = new HttpClient())               // query the server for the bus-line based on LineData
            {
                Windows.Storage.ApplicationDataContainer roamingSettings = Windows.Storage.ApplicationData.Current.RoamingSettings;

                var content = new StringContent(
                    "{\"func\":\"getRoutes\",\"params\":{\"datum\":\"" + date +
                    "\",\"erk_stype\":\"megallo\",\"ext_settings\":\"block\",\"filtering\":0,\"helyi\":\"No\",\"honnan\":\"" + from +
                    "\",\"honnan_eovx\":\"0\",\"honnan_eovy\":\"0\",\"honnan_ls_id\":" + FromlsID + ",\"honnan_settlement_id\":\"" + FromsID +
                    "\",\"honnan_site_code\":0,\"honnan_zoom\":0,\"hour\":\"" + hour + "\",\"hova\":\"" + to +
                    "\",\"hova_eovx\":\"0\",\"hova_eovy\":\"0\",\"hova_ls_id\":" + TolsID + ",\"hova_settlement_id\":" + TosID +
                    ",\"hova_site_code\":0,\"hova_zoom\":0,\"ind_stype\":\"megallo\",\"keresztul_stype\":\"megallo\",\"maxatszallas\":\"" + (String)roamingSettings.Values["change"] +
                    "\",\"maxvar\":\"" + (String)roamingSettings.Values["wait"] + "\",\"maxwalk\":\"" + (String)roamingSettings.Values["walk"] +
                    "\",\"min\":\"" + minute + "\",\"napszak\":" + napszak +
                    ",\"naptipus\":0,\"odavissza\":0,\"preferencia\":\"1\",\"rendezes\":\"1\",\"submitted\":1,\"talalatok\":1,\"target\":0,\"utirany\":\"oda\",\"var\":\"0\"}}"
                , System.Text.Encoding.UTF8, "application/json");
                
                var response = await client.PostAsync("http://menetrendek.hu/menetrend/interface/index.php", content);
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new HttpRequestException();
                responseString = await response.Content.ReadAsStringAsync();
            }

            string raw = Regex.Unescape(responseString);            // process recieved data
            JsonObject json = JsonObject.Parse(raw);

            string status = "";
            try { status = json.GetNamedString("status"); }
            catch (Exception) { status = "error"; }

            if (status == "success")
            {
                //try
                {
                    JsonObject results = json.GetNamedObject("results");
                    JsonObject talalatok = results.GetNamedObject("talalatok");
                    int t = 1;
                    while (t > 0)
                    {
                        JsonObject talalat = null;
                        try { talalat = talalatok.GetNamedObject(t.ToString()); }
                        catch (Exception) { t = 0; break; }
                        Dictionary<string, string> jarat = new Dictionary<string, string>();
                        jarat.Add("indulasi_hely", talalat.GetObject().GetNamedString("indulasi_hely"));
                        jarat.Add("erkezesi_hely", talalat.GetObject().GetNamedString("erkezesi_hely"));
                        jarat.Add("indulasi_ido", talalat.GetObject().GetNamedString("indulasi_ido"));
                        jarat.Add("erkezesi_ido", talalat.GetObject().GetNamedString("erkezesi_ido"));
                        jarat.Add("osszido", talalat.GetObject().GetNamedString("osszido"));
                        jarat.Add("ossztav", talalat.GetObject().GetNamedString("ossztav"));
                        JsonObject jaratinfok = talalat.GetObject().GetNamedObject("jaratinfok");
                        JsonArray nativedata = talalat.GetObject().GetNamedArray("nativeData");
                        int fare_total = 0, fare50_total = 0, fare90_total = 0, extra_total = 0, j = 0;
                        string fare = "", fare50 = "", fare90 = "", extra = "", num = "";
                        while (j > -1)
                        {
                            JsonObject jaratinfo = null;
                            try { jaratinfo = jaratinfok.GetObject().GetNamedObject(j.ToString()); }
                            catch (Exception)
                            {
                                if (j == 1)
                                    num = num.Substring(1) + num;
                                j = -1;
                                break;
                            }
                            fare += "|" + jaratinfo.GetObject().GetNamedNumber("fare").ToString();
                            fare50 += "|" + jaratinfo.GetObject().GetNamedNumber("fare_50_percent").ToString();
                            fare90 += "|" + jaratinfo.GetObject().GetNamedNumber("fare_90_percent").ToString();
                            extra += "|" + jaratinfo.GetObject().GetNamedNumber("additional_ticket_price").ToString();
                            if (j == 1)
                                num = " ∙∙∙" + num;
                            //num += "|" + jaratinfo.GetObject().GetNamedString("vonalnev").ToString();
                            string domain = nativedata[j].GetObject().GetNamedString("Domain_code").ToString();
                            num += "|" + (domain.Length < 4 ? domain : "");
                            fare_total += (int)jaratinfo.GetObject().GetNamedNumber("fare");
                            fare50_total += (int)jaratinfo.GetObject().GetNamedNumber("fare_50_percent");
                            fare90_total += (int)jaratinfo.GetObject().GetNamedNumber("fare_90_percent");
                            extra_total += (int)jaratinfo.GetObject().GetNamedNumber("additional_ticket_price");
                            j++;
                        }
                        jarat.Add("fare", fare_total + fare);
                        jarat.Add("fare_50_percent", fare50_total + fare50);
                        jarat.Add("fare_90_percent", fare90_total + fare90);
                        jarat.Add("extra", extra_total + extra);
                        jarat.Add("vonalnev", num);
                        jarat.Add("details", talalat.GetObject().GetNamedObject("kifejtes_postjson").Stringify().Replace(Convert.ToChar(0x0).ToString(), ""));
                        Buses.Add(jarat);
                        t++;
                    }
                }
                //catch (Exception) { }
            }
            else
                error = true;
        }

        public IAsyncAction updateOn()
        {
            return DoUpdateOn(DateTime.Today.ToString("yyyy-MM-dd"), DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString()).AsAsyncAction();
        }

        public IAsyncAction updateOn(bool forcelimited)
        {
            return DoUpdateOn(DateTime.Today.ToString("yyyy-MM-dd"), DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString(), forcelimited).AsAsyncAction();
        }

        [Windows.Foundation.Metadata.DefaultOverload]
        public IAsyncAction updateOn(DateTimeOffset date)
        {
            return DoUpdateOn(date.Date.ToString("yyyy-MM-dd"), DateTime.Now.Hour.ToString(), DateTime.Now.Minute.ToString()).AsAsyncAction();
        }

        public IAsyncAction updateOn(DateTimeOffset date, TimeSpan time)
        {
            return DoUpdateOn(date.Date.ToString("yyyy-MM-dd"), time.Hours.ToString(), time.Minutes.ToString()).AsAsyncAction();
        }

        public IAsyncAction updateOn(string date, string hour, string minute)
        {
            return DoUpdateOn(date, hour, minute).AsAsyncAction();
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
#endif