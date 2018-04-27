using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Popups;
using Windows.UI.Xaml.Controls;

namespace Timetable.Utilities
{
    static class Autocomplete
    {
        public static async Task<List<Dictionary<string, string>>> GetSuggestions(string input, string selectedDate)
        {
            ResourceLoader resourceLoader = ResourceLoader.GetForViewIndependentUse();
            List<Dictionary<string, string>> stops = new List<Dictionary<string, string>>();

            using (var client = new HttpClient()) // get new suggestions
            {
                string tosend = "{\"query\":\"get_stations2_json\",\"fieldvalue\":\"" + input + "\",\"datum\":\"" + selectedDate + "\"}";
                var values = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "json", tosend }
                    };

                var content = new FormUrlEncodedContent(values);
                HttpResponseMessage response = null;
                try
                {
                    response = await client.PostAsync("http://menetrendek.hu/uj_menetrend/hu/ajax_response_gen.php", content);
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        throw new HttpRequestException();
                }
                catch (Exception)
                {
                    var dialog = new MessageDialog(resourceLoader.GetString("NetworkError"), resourceLoader.GetString("NetworkErrorTitle"));
                    dialog.Commands.Add(new UICommand("OK", (command) =>
                    {
                        //if (Frame.CanGoBack)
                        //    Frame.GoBack();
                    }));
                    dialog.CancelCommandIndex = 0;
                    dialog.DefaultCommandIndex = 0;
                    try { await dialog.ShowAsync(); } catch (UnauthorizedAccessException) { }
                    return new List<Dictionary<string, string>>();
                }

                var codes = await response.Content.ReadAsStringAsync();
                codes = Regex.Unescape(codes);
                if (codes.Length > 0) // process data - get names and IDs for towns/stops
                {
                    codes = codes.Substring(1, codes.Length - 2);

                    Regex regex = new Regex("(?<={)[^}]+?(?=})", RegexOptions.None);
                    foreach (Match match in regex.Matches(codes))
                    {
                        string toParse = match.ToString();
                        Dictionary<string, string> stop = new Dictionary<string, string>();
                        stop.Add("Nev", getproperty(toParse, "lsname"));
                        stop.Add("VarosID", getproperty(toParse, "settlement_id"));
                        stop.Add("MegalloID", getproperty(toParse, "ls_id"));
                        stops.Add(stop);
                    }
                }
                return stops;
            }
        }

        private static string getproperty(string text, string key)
        {
            Match match = Regex.Match(text, "(?<=" + key + "\":(\")?)[^\"]*(?=(,\"|\",\"))");
            return match.Value;
        }
    }
}
