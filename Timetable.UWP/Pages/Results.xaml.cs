using System;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Core;
using Windows.UI.Xaml.Navigation;
using System.Collections.Generic;
using System.Globalization;
using Windows.UI.Xaml.Media;
using Windows.UI;
using System.Net.Http;
using Windows.UI.StartScreen;
using Windows.UI.Notifications;
using Windows.Data.Xml.Dom;
using Windows.Foundation.Metadata;
using Windows.UI.ViewManagement;
using Windows.ApplicationModel.Resources;
using Windows.ApplicationModel.DataTransfer;
using Windows.System.Profile;
using Windows.UI.Xaml.Media.Animation;

namespace Timetable
{
    /// <summary>
    /// Search results.
    /// </summary>
    public sealed partial class Results : Page
    {
        int sortmode = 1;

        //private void WindowResized(object sender, WindowSizeChangedEventArgs e)
        private void WindowResized(ApplicationView sender, object args)
        {
            double width = Window.Current.Bounds.Width;
            if (width > 690 || Window.Current.Bounds.Height < 400)
            {
                Appbar.Visibility = Visibility.Collapsed;
                Appbar2.Visibility = Visibility.Visible;
                title.Visibility = Visibility.Collapsed;
                title2.Width = width - 280;
            }
            else
            {
                Appbar.Visibility = Visibility.Visible;
                Appbar2.Visibility = Visibility.Collapsed;
                title.Visibility = Visibility.Visible;
            }


            int newsize = 400;

            if (AnalyticsInfo.VersionInfo.DeviceFamily == "Windows.Mobile" && width < 832 && width > 625)
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

            if (width > 420)
            {
                int tilenum = (int)width - 20;
                tilenum /= newsize + 6;
                int margin = ((int)width - tilenum * (newsize + 6)) / 2 - 10;
                LineList.Margin = new Thickness(margin, 1, 0, 0);
            }
            else
                LineList.Margin = new Thickness(0, 1, 0, 0);
        }

        private void GotoSettings(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(Settings));
            SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
        }

        private void CopyLine(object s, RoutedEventArgs ev, Card sender)
        {
            DataPackage package = new DataPackage();
            if (sender.LineNumber != "")
                package.SetText($@"{sender.LineNumber.Trim()}, {sender.StartTime} {sender.From} - {sender.EndTime} {sender.To}");
            else
                package.SetText($@"{sender.StartTime} {sender.From} - {sender.EndTime} {sender.To}");
            Clipboard.SetContent(package);
        }

        private void WindowActivated(object sender, WindowActivatedEventArgs e)
        {
            if (e.WindowActivationState == CoreWindowActivationState.Deactivated)
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    title.Foreground = new SolidColorBrush(Colors.White);
                    title2.Foreground = new SolidColorBrush(Colors.White);
                    titlebg.Fill = new SolidColorBrush(Colors.Black);
                    Appbar2.Background = new SolidColorBrush(Colors.Black);
                }
                 else
                {
                    title.Foreground = new SolidColorBrush(Colors.Black);
                    title2.Foreground = new SolidColorBrush(Colors.Black);
                    titlebg.Fill = new SolidColorBrush(Colors.White);
                    Appbar2.Background = new SolidColorBrush(Colors.White);
                }
            }
            else
            {
                title.Foreground = new SolidColorBrush(Colors.White);
                title2.Foreground = new SolidColorBrush(Colors.White);
                titlebg.Fill = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
                Appbar2.Background = new SolidColorBrush((Color)Application.Current.Resources["SystemAccentColor"]);
            }
        }

        private async void HandleKeyboard(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.GamepadX)
                if (timedata == null) // saved line
                    Update(this, null);
                else                  // coming from search
                    Save(this, null);
            if (e.Key == Windows.System.VirtualKey.GamepadY) // sort
            {
                var sortdialog = new ContentDialog();
                sortdialog.Title = resourceLoader.GetString("Sort");
                ListView container = new ListView();
                TextBlock item1 = new TextBlock();
                TextBlock item2 = new TextBlock();
                TextBlock item3 = new TextBlock();
                TextBlock item4 = new TextBlock();
                item1.Text = resourceLoader.GetString("Sort1");
                item2.Text = resourceLoader.GetString("Sort2");
                item3.Text = resourceLoader.GetString("Sort3");
                item4.Text = resourceLoader.GetString("Sort4");
                container.Items.Add(item1);
                container.Items.Add(item2);
                container.Items.Add(item3);
                container.Items.Add(item4);
                container.IsItemClickEnabled = true;
                container.ItemClick += (s, arg) =>
                {
                    sortmode = ((ListView)s).SelectedIndex + 1;
                    LineList.Items.Clear();
                    sortLines(sortmode);
                    sortdialog.Hide();
                };
                container.SelectedIndex = sortmode - 1;
                sortdialog.Content = container;
                await sortdialog.ShowAsync();
            }
            if (e.Key == Windows.System.VirtualKey.GamepadView)
            {
                Frame.Navigate(typeof(Settings), null, new EntranceNavigationTransitionInfo());
                SystemNavigationManager.GetForCurrentView().AppViewBackButtonVisibility = AppViewBackButtonVisibility.Visible;
            }
        }

        private void LinelistItemclick(object sender, ItemClickEventArgs e)
        {
            CardClicked((Card)LineList.SelectedItem, null);
        }
    }
}