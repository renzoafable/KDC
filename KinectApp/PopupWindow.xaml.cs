using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace KinectApp
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class PopupWindow : Window
    {
        public PopupWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var desktopArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopArea.Right - this.Width;
            this.Top = desktopArea.Bottom - this.Height;
        }
    }
}
