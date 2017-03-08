#if !BACKGROUND
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Foundation.Metadata;
using Windows.ApplicationModel.Resources;
using System.Text.RegularExpressions;
using System.Net.Http;
using System.Collections.Generic;
using Windows.UI.Popups;
using System;
using Windows.Storage;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace Timetable
{
    /// <summary>
    /// Search results.
    /// </summary>
    public sealed partial class Details : UserControl
    {
        private Card card;

        private async void Do()
        {
            //base.OnNavigatedTo(e);
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            Line line = card.ParentLine;
            int i = card.BusIndex;

            using (var client = new HttpClient()) // get new suggestions
            {
                string fieldvalue, price, price90, price50;
                line.Buses[i].TryGetValue("details", out fieldvalue);
                line.Buses[i].TryGetValue("fare", out price);
                line.Buses[i].TryGetValue("fare_90_percent", out price90);
                line.Buses[i].TryGetValue("fare_50_percent", out price50);
                string tosend = "{\"query\":\"jarat_kifejtes_text_json\",\"start_ls_id\":" + line.FromlsID + ",\"start_ls_name\":\"" + line.From + "\",\"stop_ls_id\":" + line.TolsID + ",\"stop_ls_name\":\"" + line.To + "\",\"fieldvalue\":{" + fieldvalue + "}}}}";
                tosend = tosend.Replace(' ', '+');
                var values = new Dictionary<string, string>
                {
                    { "json", tosend }
                };

                var content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = null;
                try { response = await client.PostAsync("http://menetrendek.hu/uj_menetrend/hu/ajax_response_gen.php", content); }
                catch (HttpRequestException)
                {
                    var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                    dialog.Commands.Add(new UICommand("OK", (command) => { }));
                    dialog.CancelCommandIndex = 0;
                    dialog.DefaultCommandIndex = 0;
                    //await dialog.ShowAsync();
                    return;
                }

                var result = await response.Content.ReadAsStringAsync();
                result = Regex.Unescape(result);
                string[] data = Regex.Split(result, "\"\\d+\":{");

                int n = 0;
                for (int j = 1; j < data.Length; j++)
                {
                    //Regex regex = new Regex("(?<={)[^}]+?(?=})", RegexOptions.Compiled);
                    //foreach (Match match in regex.Matches(data[j]))
                    //{
                    BusDetail detail = new BusDetail();
                    string toParse = data[j]; //match.ToString();
                    if (getproperty(toParse, "muvelet") == "indulás")
                        continue;
                    else if (getproperty(toParse, "muvelet") == "felszállás")
                    {
                        detail.Type = 1;
                        n++;
                    }
                    else if (getproperty(toParse, "muvelet") == "átszállás" || getproperty(data[j == data.Length - 1 ? 0 : j + 1], "muvelet") == "érkezés")
                        detail.Type = 3;
                    else
                        detail.Type = 2;
                    detail.Place = getproperty(toParse, "allomas").Replace('+', ' ');
                    detail.Time = getproperty(toParse, "idopont");

                    if (getproperty(toParse, "utazasi_tavolsag") != "")
                        detail.Distance = getproperty(toParse, "utazasi_tavolsag") + " km";
                    if (getproperty(toParse, "tavolsag") != "")
                        detail.Distance = (getproperty(toParse, "tavolsag") == "0") ? "" : getproperty(toParse, "tavolsag") + " m";

                    if (getproperty(toParse, "muvelet") == "felszállás")
                    {
                        string extra;
                        line.Buses[i].TryGetValue("extra", out extra);
                        detail.Extra = extra.Split('|')[n];

                        string displayprice;
                        switch ((String)roamingSettings.Values["price"])
                        {
                            case "-50%":
                                line.Buses[i].TryGetValue("fare_50_percent", out displayprice);
                                displayprice = displayprice.Split('|')[n] + " Ft";
                                break;
                            case "-90%":
                                line.Buses[i].TryGetValue("fare_90_percent", out displayprice);
                                displayprice = displayprice.Split('|')[n] + " Ft";
                                break;
                            case "Ingyen":
                                displayprice = (string.IsNullOrEmpty(extra)) ? "0 Ft" : extra.Split('|')[n] + " Ft";
                                break;
                            case "100%":
                            default:
                                line.Buses[i].TryGetValue("fare", out displayprice);
                                displayprice = displayprice.Split('|')[n] + " Ft";
                                break;
                        }
                        detail.Price = displayprice;

                        string num;
                        line.Buses[i].TryGetValue("vonalnev", out num);
                        detail.Linenum = num.Split('|')[n].Trim();
                        if (!string.IsNullOrEmpty(detail.Linenum))
                            detail.Linenum += ", ";
                        string info = getproperty(toParse, "vegallomasok");
                        info = info.Replace("felől", "▶️");
                        info = info.Replace(" felé", "");
                        info = info.Replace("Helyből indul", "▶️");
                        detail.Linenum += info;

                        detail.Lowfloor = getproperty(toParse, "alacsonypadlos") == "1";

                        detail.Organisation = getproperty(toParse, "tarsasag");
                    }
                    if (getproperty(toParse, "muvelet") == "átszállás")
                    {
                        Match match = Regex.Match(toParse, "\\d+(?= perc g)");
                        if (match.Value != "")
                            detail.Duration = match.Value.TrimStart('0') + resourceLoader.GetString("Mins");
                    }

                    DetailItem item = new DetailItem(detail);
                    if (LineList.Items.Count == 0)
                        item.Margin = new Thickness(0, 30, 0, 0);
                    LineList.Items.Add(item);
                    //}
                }

                inprogress.IsActive = false;
                inprogress.Visibility = Visibility.Collapsed;
                listscroller.Visibility = Visibility.Visible;
            }


            /*string details = Regex.Unescape((string)e.Parameter);
            Regex regex = new Regex("(?<={).+?(?=}}}})", RegexOptions.Compiled);
            foreach (Match match in regex.Matches(details))
            {
                string toParse = match.ToString();
                Dictionary<string, string> jarat = new Dictionary<string, string>();
                jarat.Add("indulasi_hely", getproperty(toParse, "indulasi_hely"));
                jarat.Add("erkezesi_hely", getproperty(toParse, "erkezesi_hely"));
                jarat.Add("indulasi_ido", getproperty(toParse, "indulasi_ido"));
                jarat.Add("erkezesi_ido", getproperty(toParse, "erkezesi_ido"));
                jarat.Add("osszido", getproperty(toParse, "osszido"));
                if (getproperty(toParse, "runcount") != "1")
                    jarat.Add("vonalnev", " ∙∙∙");
                else
                    jarat.Add("vonalnev", getproperty(toParse, "vonalnev"));
                jarat.Add("ossztav", getproperty(toParse, "ossztav"));
                jarat.Add("fare", getproperty(toParse, "fare"));
                jarat.Add("fare_50_percent", getproperty(toParse, "fare_50_percent"));
                jarat.Add("fare_90_percent", getproperty(toParse, "fare_90_percent").TrimEnd('}'));
                jarat.Add("details", getproperty(toParse, "kifejtes_postjson"));
                Buses.Add(jarat); //Buses[id].Add(jarat)
            }*/
        }

        public Details(Card card, double height)
        {
            this.InitializeComponent();
            this.card = card;
#if WINDOWS_UWP
            if (ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
                listscroller.Height = height - 120;
            else
            {
                listscroller.Height = height * 0.5;
                listscroller.Margin = new Thickness(10, 0, -10, 0);
            }
#endif
            Do();
        }

        private string getproperty(string text, string key)
        {
            Match match = Regex.Match(text, "(?<=\"" + key + "\":(\")?)[^\"]*(?=(}}|,\"|\",\"))");
            return match.Value;
        }
    }
}
#endif