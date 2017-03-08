using System;
using Windows.ApplicationModel.Resources;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;

namespace Timetable
{
    /// <summary>
    /// Card to be filled with line data.
    /// </summary>
    public sealed partial class DetailItem : UserControl
    {
        public DetailItem()
        {
            this.InitializeComponent();
        }

        //todo: (Line line, int day, int i)
        internal DetailItem(BusDetail bus)
        {
            this.InitializeComponent();
            ResourceLoader resourceLoader = ResourceLoader.GetForCurrentView();

#if WINDOWS_UWP
            rect.Fill = Resources["SystemControlBackgroundAccentBrush"] as SolidColorBrush;
            def2.Fill = Resources["SystemControlBackgroundAccentBrush"] as SolidColorBrush;
#elif WINDOWS_PHONE_APP
            rect.Fill = Resources["PhoneAccentBrush"] as SolidColorBrush;
            def2.Fill = Resources["PhoneAccentBrush"] as SolidColorBrush;
#endif

            LineNumber = bus.Linenum;
            StartTime = bus.Time;
            From = bus.Place;
            TravelTime = bus.Duration;
            Distance = bus.Distance;
            Price = bus.Price;

            if (LineNumber == "")
                price.SetValue(Canvas.TopProperty, 18);

            if (bus.Type == 1) // bus
            {
                Org = resourceLoader.GetString("Organization") + bus.Organisation;
                canvas.Height = 77;
                rect.Height = 69;
                dist.SetValue(Canvas.TopProperty, 39);

                double width = Window.Current.Bounds.Width;
#if WINDOWS_UWP
                if (width < 370)
                {
                    ticket.SetValue(Canvas.LeftProperty, 150);
                    price.SetValue(Canvas.LeftProperty, 103);
                    num.SetValue(Canvas.LeftProperty, 103);
                    org.SetValue(Canvas.LeftProperty, 103);
                    pricenote.SetValue(Canvas.LeftProperty, 168);
                    chair.SetValue(Canvas.LeftProperty, 305);
                    pricenote.Text = resourceLoader.GetString("SupTicketBeginning") + bus.Extra + resourceLoader.GetString("SupTicketShort");
                    if (width < 330)
                    {
                        pricenote.FontSize = 10;
                        chair.SetValue(Canvas.LeftProperty, 290);
                    }
                }
                else
                    pricenote.Text = resourceLoader.GetString("SupTicketBeginning") + bus.Extra + resourceLoader.GetString("SupTicket");
#elif WINDOWS_PHONE_APP
                if (width < 410)
                    pricenote.Text = resourceLoader.GetString("SupTicketBeginning") + bus.Extra + resourceLoader.GetString("SupTicketShort");
                else
                    pricenote.Text = resourceLoader.GetString("SupTicketBeginning") + bus.Extra + resourceLoader.GetString("SupTicket");
#endif
                if (bus.Extra == "0")
                {
                    if (width >= 350)
                        chair.SetValue(Canvas.LeftProperty, 155);
                    else
                        chair.SetValue(Canvas.LeftProperty, 147);
                }
                if (bus.Extra != "0")
                {
                    ticket.Visibility = Visibility.Visible;
                    pricenote.Visibility = Visibility.Visible;
                }
                if (bus.Lowfloor)
                    chair.Visibility = Visibility.Visible;
            }

            if (bus.Type == 2) // end
                rect.Visibility = Visibility.Collapsed;

            if (bus.Type == 3) // walk
            {
#if WINDOWS_UWP
                rect.Fill = new SolidColorBrush((Color)Application.Current.Resources["SystemBaseHighColor"]);
#elif WINDOWS_PHONE_APP
                rect.Fill = new SolidColorBrush((Color)Application.Current.Resources["PhoneForegroundColor"]);
#endif
                /*if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                    rect.Fill = new SolidColorBrush(Colors.White);
                else
                    rect.Fill = new SolidColorBrush(Colors.Black);*/
                if (bus.Duration != null)
                    dist.SetValue(Canvas.TopProperty, 40);
            }
        }


        public String LineNumber
        {
            get { return (String)GetValue(LineNumberProperty); }
            set { SetValue(LineNumberProperty, value); }
        }
        private static readonly DependencyProperty LineNumberProperty = DependencyProperty.Register("LineNumber", typeof(String), typeof(DetailItem), null);

        public String Price
        {
            get { return (String)GetValue(PriceProperty); }
            set { SetValue(PriceProperty, value); }
        }
        private static readonly DependencyProperty PriceProperty = DependencyProperty.Register("Price", typeof(String), typeof(DetailItem), null);

        public String From
        {
            get { return (String)GetValue(FromProperty); }
            set { SetValue(FromProperty, value); }
        }
        private static readonly DependencyProperty FromProperty = DependencyProperty.Register("From", typeof(String), typeof(DetailItem), null);

        public String StartTime
        {
            get { return (String)GetValue(StartTimeProperty); }
            set { SetValue(StartTimeProperty, value); }
        }
        private static readonly DependencyProperty StartTimeProperty = DependencyProperty.Register("StartTime", typeof(String), typeof(DetailItem), null);

        public String TravelTime
        {
            get { return (String)GetValue(TravelTimeProperty); }
            set { SetValue(TravelTimeProperty, value); }
        }
        private static readonly DependencyProperty TravelTimeProperty = DependencyProperty.Register("TravelTime", typeof(String), typeof(DetailItem), null);

        public String Distance
        {
            get { return (String)GetValue(DistanceProperty); }
            set { SetValue(DistanceProperty, value); }
        }
        private static readonly DependencyProperty DistanceProperty = DependencyProperty.Register("Distance", typeof(String), typeof(DetailItem), null);

        public String Org
        {
            get { return (String)GetValue(OrgProperty); }
            set { SetValue(OrgProperty, value); }
        }
        private static readonly DependencyProperty OrgProperty = DependencyProperty.Register("Org", typeof(String), typeof(DetailItem), null);

    }
}
