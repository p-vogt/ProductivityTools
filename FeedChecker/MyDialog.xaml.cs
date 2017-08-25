﻿using System.Windows;
using System.Windows.Input;

namespace FeedChecker
{
    /// <summary>
    /// Interaktionslogik für MyDialolog.xaml
    /// </summary>
    partial class MyDialog : Window
    {

        private bool _usePasswordBox;
        public MyDialog(string messageText, bool usePasswordBox)
        {
            InitializeComponent();
            this.labelMessage.Content = messageText;
            _usePasswordBox = usePasswordBox;
            if (usePasswordBox)
            {
                tBoxInput.Visibility = Visibility.Hidden;
                this.tBoxPassword.Focus();
            }
            else
            {
                tBoxPassword.Visibility = Visibility.Hidden;
                this.tBoxInput.Focus();
            }
        }

        public string ResponseText
        {
            get
            {
                if (_usePasswordBox)
                {
                    return tBoxPassword.Password;
                }
                else
                {
                    return tBoxInput.Text;
                }

            }
        }

        private void btnOk_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void tBoxInput_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Return)
            {
                btnOk_Click(null, null);
            }
        }
    }
}
