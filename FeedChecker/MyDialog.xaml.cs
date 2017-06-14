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

namespace FeedChecker
{
    /// <summary>
    /// Interaktionslogik für MyDialolog.xaml
    /// </summary>
    partial class MyDialog : Window
    {

        public MyDialog(string messageText)
        {
            InitializeComponent();
            this.labelMessage.Content = messageText;
            this.tBoxInput.Focus();
        }

        public string ResponseText
        {
            get { return tBoxInput.Text; }
            set { tBoxInput.Text = value; }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void tBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return )
            {
                btnOk_Click(null, null);
            }
        }
    }
}
