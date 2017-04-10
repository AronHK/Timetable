using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI.Popups;

namespace Timetable.Utilities
{
    public sealed class LineSerializer
    {
        private StorageFolder roamingFolder;
        private StorageFolder localFolder;
        private DataContractSerializer serializer;
        private ResourceLoader resourceLoader;

        public LineSerializer(ResourceLoader resourceLoader)
        {
            roamingFolder = ApplicationData.Current.RoamingFolder;
            localFolder = ApplicationData.Current.LocalFolder;
            serializer = new DataContractSerializer(typeof(List<Line>));
            this.resourceLoader = resourceLoader;
        }

        private async Task showError(string msg)
        {
            var dialog = new MessageDialog(msg, resourceLoader.GetString("FileerrorHeader"));
            dialog.Commands.Add(new UICommand("OK"));
            dialog.CancelCommandIndex = 0;
            dialog.DefaultCommandIndex = 0;
            await dialog.ShowAsync();
        }

        /// <summary>Read all lines from the file</summary>
        public IAsyncOperation<IList<Line>> readLines()
        {
            return DoReadLines().AsAsyncOperation();
        }

        private async Task<IList<Line>> DoReadLines()
        {
            Stream linedata_read = null;
            bool legacy = true;
            try { linedata_read = await roamingFolder.OpenStreamForReadAsync("linedata"); } catch (FileNotFoundException) { legacy = false; }

            if (legacy)
                return await LegacyReadLines(linedata_read);
            else
            {
                IList<Line> readLines = new List<Line>();
                IList<string> lines = await readLineData();
                foreach (var line in lines)
                    readLines.Add(await DoOpenLine(line));

                return readLines;
            }
        }

        private async Task<IList<string>> readLineData()
        {
            try
            {
                StorageFile datafile = await roamingFolder.GetFileAsync("linelist");
                IList<string> lines = await FileIO.ReadLinesAsync(datafile);
                List<string> toDelete = new List<string>();
                lines.Remove("");
                foreach(string line in lines)
                {
                    if (line.Contains("##"))
                        toDelete.Add(line);
                    int dividers = 0;
                    foreach (char character in line)
                    {
                        if (character == '#')
                            dividers++;
                    }
                    if (dividers != 6)
                        toDelete.Add(line);
                }
                if (toDelete.Count > 0)
                {
                    foreach (string line in toDelete)
                        lines.Remove(line);
                    await FileIO.WriteLinesAsync(datafile, lines);
                }
                return lines;
            }
            catch (FileNotFoundException) { return new List<string>(); }
        }

        private async Task<IList<Line>> LegacyReadLines(Stream linedata_read)
        {
            List<Line> savedLines = new List<Line>();
            StorageFile datafile;
            try { datafile = await roamingFolder.GetFileAsync("linedata"); }
            catch (FileNotFoundException)
            {
                await showError(resourceLoader.GetString("FileerrorRestore"));
                return savedLines;
            }
            if (datafile != null)
            {
                string olddata = await FileIO.ReadTextAsync(datafile);

                Regex name = new Regex("(?<=<Name>).+?(?=<\\/Name>)", RegexOptions.None);
                Regex from = new Regex("(?<=<from>).+?(?=<\\/from>)", RegexOptions.None);
                Regex fromlsid = new Regex("(?<=<fromlsID>).+?(?=<\\/fromlsID>)", RegexOptions.None);
                Regex fromsid = new Regex("(?<=<fromsID>).+?(?=<\\/fromsID>)", RegexOptions.None);
                Regex to = new Regex("(?<=<to>).+?(?=<\\/to>)", RegexOptions.None);
                Regex tolsID = new Regex("(?<=<tolsID>).+?(?=<\\/tolsID>)", RegexOptions.None);
                Regex tosID = new Regex("(?<=<tosID>).+?(?=<\\/tosID>)", RegexOptions.None);

                Match m_name = name.Match(olddata);
                Match m_from = from.Match(olddata);
                Match m_fromlsid = fromlsid.Match(olddata);
                Match m_fromsid = fromsid.Match(olddata);
                Match m_to = to.Match(olddata);
                Match m_tolsID = tolsID.Match(olddata);
                Match m_tosID = tosID.Match(olddata);

                while (m_from.Success)
                {
                    var line = await restoreLine($"asd#{m_fromsid}#{m_fromlsid}#{m_tosID}#{m_tolsID}#{m_from}#{m_to}", m_name.Value);
                    savedLines.Add(line);

                    m_name = m_name.NextMatch();
                    m_from = m_from.NextMatch();
                    m_fromlsid = m_fromlsid.NextMatch();
                    m_fromsid = m_fromsid.NextMatch();
                    m_to = m_to.NextMatch();
                    m_tolsID = m_tolsID.NextMatch();
                    m_tosID = m_tosID.NextMatch();
                }
            }

            await datafile.DeleteAsync();
            return savedLines;
        }

        /// <summary>Read all saved lines that start at the sepcified location</summary>
        public IAsyncOperation<IList<Line>> readLinesFrom(string location)
        {
            return DoReadLinesFrom(location).AsAsyncOperation();
        }

        private async Task<IList<Line>> DoReadLinesFrom(string location)
        {
            IList<string> linedatas = await readLineData();
            IList<Line> lines = new List<Line>();
            foreach (var line in linedatas)
            {
                string[] linedata = line.Split('#');
                if (linedata[5].Contains(location))
                    lines.Add(await DoOpenLine(line));
            }
            return lines;
        }

        /// <summary>Do we have save data for the specific line?</summary>
        public IAsyncOperation<bool> LineExists(Line line)
        {
            return DoLineExists(line).AsAsyncOperation();
        }

        private async Task<bool> DoLineExists(Line lineTocCheck)
        {
            IList<Line> readLines = new List<Line>();
            try
            {
                StorageFile datafile = await roamingFolder.GetFileAsync("linelist");

                var lines = await FileIO.ReadLinesAsync(datafile);
                foreach (var line in lines)
                {
                    if (line.Equals(lineTocCheck))
                        return true;
                }
                return false;
            }
            catch (FileNotFoundException) { return false; }
        }

        /// <summary>Read a Line from storage</summary>
        public IAsyncOperation<Line> openLine(string code)
        {
            //string[] lineData = code.Split('#');
            return DoOpenLine(code).AsAsyncOperation();
        }

        public IAsyncOperation<Line> openLine(string FromsID, string FromlsID, string TosID, string TolsID)
        {
            return DoOpenLine($@"MenetrendApp#{FromsID}#{FromlsID}#{TosID}#{TolsID}").AsAsyncOperation();
        }

        public IAsyncOperation<Line> openLine(string FromsID, string FromlsID, string TosID, string TolsID, string from, string to)
        {
            return DoOpenLine($@"MenetrendApp#{FromsID}#{FromlsID}#{TosID}#{TolsID}", from, to).AsAsyncOperation();
        }

        private async Task<Line> DoOpenLine(string code, string from = "", string to = "")
        {
            IList<string> lines = await readLineData();
            Stream linedata_read = null;
            Line savedLine = new Line();
            string fullname = code;

            if (from == "" || to == "")
            {
                string[] split = code.Split('#');
                if (split.Length < 6)
                {
                    foreach (string current in lines)
                    {
                        if (current.Contains(code))
                        {
                            fullname = current;
                            break;
                        }
                    }
                }
                else
                    code = $@"{split[0]}#{split[1]}#{split[2]}#{split[3]}#{split[4]}";
            }
            else
                fullname = code + $"#{from}#{to}";

            try
            {
                try { linedata_read = await localFolder.OpenStreamForReadAsync(code); }
                catch (FileNotFoundException)
                {
                    if (lines.Contains(fullname))
                    {
                        try { linedata_read.Dispose(); } catch (Exception) { }
                        return await restoreLine(fullname);
                    }
                }

                if (linedata_read != null)
                {
                    try
                    {
                        savedLine = (Line)serializer.ReadObject(linedata_read);
                        if (savedLine == null)
                            savedLine = new Line();
                    }
                    catch (System.Xml.XmlException)
                    {
                        return await restoreLine(fullname);
                    }
                }
                else
                {
                    try { linedata_read.Dispose(); } catch (Exception) { }
                    return await restoreLine(fullname);
                }
                try { linedata_read.Dispose(); } catch (Exception) { }
            }
            catch (UnauthorizedAccessException) { await showError(resourceLoader.GetString("FileerrorTry")); }
            catch (SerializationException)
            {
                StorageFile datafile = await localFolder.GetFileAsync(code);
                await datafile.DeleteAsync();
                return await restoreLine(fullname);
            }
            return savedLine;
        }

        private async Task<Line> restoreLine(string filename, string linename = "")
        {
            string[] lineData = filename.Split('#');
            Line lineToRestore = new Line(lineData[1], lineData[3], lineData[2], lineData[4], lineData[5], lineData[6]);
            lineToRestore.Name = linename;
            try
            {
                await lineToRestore.updateOn();
                await DoSaveLine(lineToRestore);
            }
            catch (System.Net.Http.HttpRequestException) { }
            return lineToRestore;
        }

        /// <summary>Update/save the specified line</summary>
        public IAsyncAction saveLine(Line toSave)
        {
            return DoSaveLine(toSave).AsAsyncAction();
        }

        private async Task DoSaveLine(Line toSave)
        {
            IList<string> lines = await readLineData();
            StorageFile datafile = await roamingFolder.CreateFileAsync("linelist", CreationCollisionOption.OpenIfExists);
            if (!lines.Contains($@"MenetrendApp#{toSave.FromsID}#{toSave.FromlsID}#{toSave.TosID}#{toSave.TolsID}#{toSave.From}#{toSave.To}"))
                await FileIO.AppendTextAsync(datafile, $"MenetrendApp#{toSave.FromsID}#{toSave.FromlsID}#{toSave.TosID}#{toSave.TolsID}#{toSave.From}#{toSave.To}\r\n");

            try
            {
                Stream linedata_write = await localFolder.OpenStreamForWriteAsync($@"MenetrendApp#{toSave.FromsID}#{toSave.FromlsID}#{toSave.TosID}#{toSave.TolsID}", CreationCollisionOption.ReplaceExisting);
                serializer.WriteObject(linedata_write, toSave);
                await linedata_write.FlushAsync();
                linedata_write.Dispose();
            }
            catch (UnauthorizedAccessException) { await showError(resourceLoader.GetString("FileerrorTry")); }
        }

        /// <summary>Remove the specified line</summary>
        public IAsyncAction removeLine(Line toRemove)
        {
            return DoRemoveLine(toRemove).AsAsyncAction();
        }

        private async Task DoRemoveLine(Line toRemove)
        {
            IList<string> lines = await readLineData();
            lines.Remove($@"MenetrendApp#{toRemove.FromsID}#{toRemove.FromlsID}#{toRemove.TosID}#{toRemove.TolsID}#{toRemove.From}#{toRemove.To}");

            StorageFile datafile = await roamingFolder.GetFileAsync("linelist");
            await FileIO.WriteLinesAsync(datafile, lines);

            StorageFile linefile = await localFolder.GetFileAsync($@"MenetrendApp#{toRemove.FromsID}#{toRemove.FromlsID}#{toRemove.TosID}#{toRemove.TolsID}");
            await linefile.DeleteAsync();
        }
    }
}
