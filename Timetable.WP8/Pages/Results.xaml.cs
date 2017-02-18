using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Globalization;
using Windows.UI.Xaml.Media;
using System.Net.Http;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using System.Threading.Tasks;
using Windows.UI.StartScreen;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Core;

namespace Timetable
{
    /// <summary>
    /// Search results.
    /// </summary>
    public sealed partial class Results : Page
    {
        private Card current;

        private void WindowResized(ApplicationView sender, object args)
        {
            double width = ApplicationView.GetForCurrentView().VisibleBounds.Width;
            int newsize = 400;

            if (width < 832 && width > 625)
            {
                foreach (Card card in LineList.Items)
                {
                    LineList.Margin = new Thickness(0, 1, 0, 0);
                    newsize = ((int)width - 32) / 2;
                    card.ChangeSize(newsize);
                }
            }
            else
            {
                foreach (Card card in LineList.Items)
                    card.ChangeSize(400);
            }
        }

        private void GotoSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
        }
    }
}