using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace QuickAccessOverlay
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_MouseEnter(object sender, MouseEventArgs e)
        {
            //if (Keyboard.IsKeyDown(Key.LeftShift) && !expander.IsExpanded)
            //{
            //    expander.IsExpanded = true;
            //    Left -= (100);
            //}
        }

        private void Window_MouseLeave(object sender, MouseEventArgs e)
        {
            //if (!Keyboard.IsKeyDown(Key.LeftShift) && expander.IsExpanded)
            //{
            //    Left += (100);
            //    expander.IsExpanded = false;
            //}
        }
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            var buffer = e.Data.GetData(DataFormats.FileDrop, false) as string[];
            foreach (var file in buffer)
            {
                BitmapSource bmpSrc = LoadIcon(file);
                imageTest.Source = bmpSrc;
            }
        }

        private void Window_DragOver(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.StringFormat, true))
            {
                string[] filenames = e.Data.GetData(DataFormats.FileDrop, true) as string[];

                if (filenames.Count() > 1 || !System.IO.Directory.Exists(filenames.First()))
                {
                    e.Effects = DragDropEffects.None;
                    e.Handled = true;
                }
                else
                {
                    e.Handled = true;
                    e.Effects = DragDropEffects.Move;
                }
            }
        }

        internal BitmapSource LoadIcon(string programPath)
        {
            if (File.Exists(programPath))
            {
                Icon tmpicon = System.Drawing.Icon.ExtractAssociatedIcon(programPath);
                if (tmpicon != null)
                {
                    Bitmap bitmap = tmpicon.ToBitmap();

                    return System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            bitmap.GetHbitmap(),
                            IntPtr.Zero,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromWidthAndHeight(bitmap.Width, bitmap.Height));
                }
            }
            return null;
        }
    }
}
