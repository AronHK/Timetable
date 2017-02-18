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

namespace Timetable
{
    public sealed class LineSerializer
    {
        private StorageFolder roamingFolder;
        private DataContractSerializer serializer;
        private ResourceLoader resourceLoader;

        public LineSerializer(ResourceLoader resourceLoader)
        {
            roamingFolder = ApplicationData.Current.RoamingFolder;
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

        /// <summary>Append a Line to the end of the file</summary>
        public IAsyncAction saveLine(Line line)
        {
            return DoSaveLine(line).AsAsyncAction();
        }

        private async Task DoSaveLine(Line line)
        {
            try
            {
                IList<Line> toSave = await readLines();
                toSave.Add(line);
                await writeLines(toSave);
            }
            catch (UnauthorizedAccessException) { await showError(resourceLoader.GetString("FileerrorTry")); }
        }

        /// <summary>Remove a Line from the file</summary>
        public IAsyncAction removeLine(Line line)
        {
            return DoRemoveLine(line).AsAsyncAction();
        }

        private async Task DoRemoveLine(Line line)
        {
            try
            {
                IList<Line> toSave = await readLines();
                toSave.Remove(line);
                await writeLines(toSave);
            }
            catch (UnauthorizedAccessException) { await showError(resourceLoader.GetString("FileerrorTry")); }
        }

        /// <summary>Write all Lines into the file</summary>
        public IAsyncAction writeLines(IList<Line> toSave)
        {
            return DoWriteLines(toSave).AsAsyncAction();
        }

        private async Task DoWriteLines(IList<Line> toSave)
        {
            try
            {
                Stream linedata_write = await roamingFolder.OpenStreamForWriteAsync("linedata", CreationCollisionOption.ReplaceExisting);
                serializer.WriteObject(linedata_write, toSave);
                await linedata_write.FlushAsync();
                linedata_write.Dispose();
            }
            catch (UnauthorizedAccessException) { await showError(resourceLoader.GetString("FileerrorTry")); }
        }

        /// <summary>Read all lines from the file</summary>
        public IAsyncOperation<IList<Line>> readLines()
        {
            return DoReadLines().AsAsyncOperation();
        }

        private async Task<IList<Line>> DoReadLines()
        {

            List<Line> savedLines = new List<Line>(); ;
            Stream linedata_read = null;
            try
            {
                try { linedata_read = await roamingFolder.OpenStreamForReadAsync("linedata"); }
                catch (FileNotFoundException) { linedata_read = null; }
                finally
                {
                    if (linedata_read != null)
                    {
                        try
                        {
                            savedLines = (List<Line>)serializer.ReadObject(linedata_read);
                            if (savedLines == null)
                                savedLines = new List<Line>();
                        }
                        catch (System.Xml.XmlException) { savedLines = new List<Line>(); }
                    }
                    else
                        savedLines = new List<Line>();
                    try { linedata_read.Dispose(); } catch (Exception) { }
                }


            }
            catch (UnauthorizedAccessException) { await showError(resourceLoader.GetString("FileerrorTry")); }
            catch (SerializationException)
            {
                StorageFile datafile = await roamingFolder.GetFileAsync("linedata");
                String filecontent = await FileIO.ReadTextAsync(datafile);
                if (filecontent.Contains("</Line>"))
                {
                    Match match = Regex.Match(filecontent, ".*<\\/Line>");
                    filecontent = match.Groups[0].Value + "</ArrayOfLine>";
                    await FileIO.WriteTextAsync(datafile, filecontent);
                }
                else
                    await datafile.DeleteAsync();
                await FileIO.WriteTextAsync(datafile, filecontent);
                await showError(resourceLoader.GetString("FileerrorRestore"));
                return await readLines();
            }

            return savedLines;
        }
    }
}
