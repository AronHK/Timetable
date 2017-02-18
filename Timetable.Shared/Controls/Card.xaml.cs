#if !BACKGROUND
using System;
using Windows.ApplicationModel.Resources;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Timetable
{
    /// <summary>
    /// Card to be filled with line data.
    /// </summary>
    public partial class Card : ListViewItem
    {
        public Card()
        {
            this.InitializeComponent();
            parentLine = null;
        }

        //todo: (Line line, int day, int i)
        public Card(Line line, int i)
        {
            this.InitializeComponent();
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

#if WINDOWS_UWP
            this.Background = Resources["SystemControlBackgroundAccentBrush"] as SolidColorBrush;
#elif WINDOWS_PHONE_APP
            Background = Resources["PhoneAccentBrush"] as SolidColorBrush;
#endif

            parentLine = line;
            LineName = line.Name;
            busIndex = i;

            if (line.Buses.Count > 0 && i >= 0)
            {
                string temp;
                line.Buses[i].TryGetValue("vonalnev", out temp);
                LineNumber = temp.Split('|')[0];
                line.Buses[i].TryGetValue("indulasi_hely", out temp);
                From = temp;
                line.Buses[i].TryGetValue("erkezesi_hely", out temp);
                To = temp;
                line.Buses[i].TryGetValue("indulasi_ido", out temp);
                StartTime = temp;
                line.Buses[i].TryGetValue("erkezesi_ido", out temp);
                EndTime = temp;
                line.Buses[i].TryGetValue("osszido", out temp);
                TravelTime = resourceLoader.GetString("CardTime") + temp;
                line.Buses[i].TryGetValue("ossztav", out temp);
                Distance = temp;
                switch ((String)roamingSettings.Values["price"])
                {
                    case "100%":
                        line.Buses[i].TryGetValue("fare", out temp);
                        Price = temp.Split('|')[0] + " Ft";
                        break;
                    case "-50%":
                        line.Buses[i].TryGetValue("fare_50_percent", out temp);
                        Price = temp.Split('|')[0] + " Ft";
                        break;
                    case "-90%":
                        line.Buses[i].TryGetValue("fare_90_percent", out temp);
                        Price = temp.Split('|')[0] + " Ft";
                        break;
                    case "Ingyen":
                        line.Buses[i].TryGetValue("extra", out temp);
                        Price = (string.IsNullOrEmpty(temp)) ? "0 Ft" : temp.Split('|')[0] + " Ft";
                        break;
                }

                if (LineNumber == "")
                {
                    name.SetValue(Canvas.LeftProperty, 3);
                    price.SetValue(Canvas.LeftProperty, 3);
                    def1.SetValue(Canvas.LeftProperty, 53);
                }
            }
            if (parentLine.LastUpdated != DateTime.ParseExact("1000.01.01", "yyyy.MM.dd", System.Globalization.CultureInfo.InvariantCulture) && parentLine.LastUpdated < DateTime.Today.Date)
            {
                name.SetValue(Canvas.LeftProperty, 5);
                def1.Visibility = Visibility.Collapsed;
                def2.Visibility = Visibility.Collapsed;
                def3.Visibility = Visibility.Collapsed;
                def4.Visibility = Visibility.Collapsed;
                num.Visibility = Visibility.Collapsed;
                price.Visibility = Visibility.Collapsed;
                dist.Visibility = Visibility.Collapsed;
                from.Visibility = Visibility.Collapsed;
                start.Visibility = Visibility.Collapsed;
                to.Visibility = Visibility.Collapsed;
                end.Visibility = Visibility.Collapsed;
                error2.Visibility = Visibility.Visible;
            }
            else if (line.Buses.Count == 0 || i < 0)
            {
                name.SetValue(Canvas.LeftProperty, 5);
                def1.Visibility = Visibility.Collapsed;
                def2.Visibility = Visibility.Collapsed;
                def3.Visibility = Visibility.Collapsed;
                def4.Visibility = Visibility.Collapsed;
                error.Visibility = Visibility.Visible;
            }
        }

        private Line parentLine;
        private int busIndex;
        public Line ParentLine
        {
            get { return parentLine; }
            set { parentLine = value; }
        }
        public int BusIndex
        {
            get { return busIndex; }
            set { busIndex = value; }
        }

        public void ChangeSize(int size)
        {
            this.Width = (size < 300) ? 300 : size;
            from.Width = this.Width - 95;
            to.Width = this.Width - 95;
            name.Width = this.Width - 105;
        }

        public void ShowUpdateIndicator()
        {
            if (progress.Visibility == Visibility.Visible)
                progress.Visibility = Visibility.Collapsed;
            else
                progress.Visibility = Visibility.Visible;
        }

        /*public Brush CardBackground
        {
            get { return (Brush)GetValue(CardBackgroundProperty); }
            set { SetValue(CardBackgroundProperty, value); }
        }
        public static readonly DependencyProperty CardBackgroundProperty = DependencyProperty.Register("CardBackground", typeof(Brush), typeof(Card), null);*/

        public String LineName
        {
            get { return (String)GetValue(LineNameProperty); }
            set { SetValue(LineNameProperty, value); }
        }
        public static readonly DependencyProperty LineNameProperty = DependencyProperty.Register("LineName", typeof(String), typeof(Card), null);

        public String LineNumber
        {
            get { return (String)GetValue(LineNumberProperty); }
            set { SetValue(LineNumberProperty, value); }
        }
        public static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register("LineNumber", typeof(String), typeof(Card), null);

        public String Price
        {
            get { return (String)GetValue(PriceProperty); }
            set { SetValue(PriceProperty, value); }
        }
        public static readonly DependencyProperty PriceProperty = DependencyProperty.Register("Price", typeof(String), typeof(Card), null);

        public String From
        {
            get { return (String)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }
        public static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(String), typeof(Card), null);

        public String To
        {
            get { return (String)GetValue(ToProperty); }
            set { SetValue(ToProperty, value); }
        }
        public static readonly DependencyProperty ToProperty = DependencyProperty.Register("To", typeof(String), typeof(Card), null);

        public String StartTime
        {
            get { return (String)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }
        public static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register("StartTime", typeof(String), typeof(Card), null);

        public String EndTime
        {
            get { return (String)GetValue(EndTimeProperty); }
            set { SetValue(EndTimeProperty, value); }
        }
        public static readonly DependencyProperty EndTimeProperty = DependencyProperty.Register("EndTime", typeof(String), typeof(Card), null);

        public String TravelTime
        {
            get { return (String)GetValue(TravelTimeProperty); }
            set { SetValue(TravelTimeProperty, value); }
        }
        public static readonly DependencyProperty TravelTimeProperty = DependencyProperty.Register("TravelTime", typeof(String), typeof(Card), null);

        public String TimeLeft
        {
            get { return (String)GetValue(TimeLeftProperty); }
            set { SetValue(TimeLeftProperty, value); }
        }
        public static readonly DependencyProperty TimeLeftProperty = DependencyProperty.Register("TimeLeft", typeof(String), typeof(Card), null);

        public String Distance
        {
            get { return (String)GetValue(DistanceProperty); }
            set { SetValue(DistanceProperty, value); }
        }
        public static readonly DependencyProperty DistanceProperty = DependencyProperty.Register("Distance", typeof(String), typeof(Card), null);
    }
}
#endif