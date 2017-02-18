using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace Timetable
{
    public sealed partial class ChangelogWindow : UserControl
    {
        public ChangelogWindow(double width, double height)
        {
            this.InitializeComponent();
            if (Windows.Foundation.Metadata.ApiInformation.IsTypePresent("Windows.UI.ViewManagement.StatusBar"))
            {
                panel.Width = width - 20;
                scroller.Height = height - 170;
            }
            else
            {
                panel.Width = 430;
                scroller.Height = height * 0.7 - 170;
            }
        }
    }
}
