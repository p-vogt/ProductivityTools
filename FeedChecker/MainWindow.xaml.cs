using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml.Linq;
using WpfTrayIcon;

public static class ExtensionMethods
{
    private static Action EmptyDelegate = delegate () { };

    public static void Refresh(this UIElement uiElement)
    {
        uiElement.Dispatcher.Invoke(DispatcherPriority.Render, EmptyDelegate);
    }
}
namespace FeedChecker
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        List<ILIASCourse> oldCourseList = new List<ILIASCourse>();
        List<ILIASCourse> changeList = new List<ILIASCourse>();
        MediaPlayer mplayer = new MediaPlayer();
        private SimpleTrayIcon notifyIcon;

        public double Volume
        {
            set
            {
                value = value > 1 ? 1 : value;
                value = value < 0 ? 0 : value;

                mplayer.Volume = value;
                Properties.Settings.Default.VOLUME = value;
                Properties.Settings.Default.Save();
                NotifyPropertyChanged();
            }
            get { return mplayer.Volume; }
        }

        private const string OLD_DATA_FILE_NAME = @"olddata.txt";

        public MainWindow()
        {
            InitializeComponent();

            Volume = Properties.Settings.Default.VOLUME;

            MemoryStream ms = new MemoryStream();
            (Properties.Resources.rss_circle_color).Save(ms);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();


            Icon = image;

            notifyIcon = new SimpleTrayIcon(this, Properties.Resources.rss_circle_color);
            notifyIcon.Visible = true;

            WindowState = WindowState.Minimized;
            Visibility = Visibility.Hidden;
            try
            {
                string olddata = File.ReadAllText(OLD_DATA_FILE_NAME);
                XDocument doc = XDocument.Parse(olddata);
                oldCourseList = XDocToILIASCourseList(doc);
            }
            catch (Exception)
            {

            }
            getDataAndCompareWithOld();

            DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 1, 0);
            dispatcherTimer.Start();


        }


        public void PlaySound()
        {
            mplayer.MediaFailed += (o, args) =>
            {
                MessageBox.Show("Media Failed!!");
            };
            mplayer.Open(
                new Uri("notificationSound.mp3", UriKind.Relative));
            mplayer.Play();
        }
        void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            notifyIcon.Visible = false;
            ShowInTaskbar = true;
            //Reihenfolge von Visible und Normal beachten!
            Visibility = Visibility.Visible;
            WindowState = WindowState.Normal;
            Activate();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            getDataAndCompareWithOld();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            Task.Run(() => getDataAndCompareWithOld(true));
        }

        private void getDataAndCompareWithOld(bool msgBoxWhenFinished = false)
        {
            Dispatcher.Invoke(() =>
            {
                treeView.Items.Clear();
            });

            string rssFeedUrl = Properties.Settings.Default.FEEDURL;
            if (rssFeedUrl == "")
            {
                MessageBox.Show("Invalid Feed URL", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;

                IWebProxy defaultProxy = WebRequest.DefaultWebProxy;
                if (defaultProxy != null)
                {
                    defaultProxy.Credentials = CredentialCache.DefaultCredentials;
                    client.Proxy = defaultProxy;
                }

                client.UseDefaultCredentials = false;
                client.Credentials = new NetworkCredential(Properties.Settings.Default.USERNAME,
                    Properties.Settings.Default.PASSWORD);
                List<ILIASCourse> newCourseList = new List<ILIASCourse>();
                try
                {
                    string result = client.DownloadString(rssFeedUrl);

                    File.WriteAllText(OLD_DATA_FILE_NAME, result);
                    XDocument doc = XDocument.Parse(result);
                    newCourseList = XDocToILIASCourseList(doc);
                }
                catch (WebException ex)
                {
                    string msg = ex.Message.ToLower();
                    // ex.Message -> ...Remoteserver/Remotename... = no internet
                    if ((!Regex.IsMatch(msg, ".*(remoteserver|remotename).*") && !msg.Contains("timeout"))
                       || msg.Contains("(401)")) // 401 => not authorized
                    {

                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show("Feed URL / Username / Passwort falsch?\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        });
                    }
                    return;
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show("Feed URL / Username / Passwort falsch?\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                    return;
                }

                List<ILIASCourse> newChangeList = CompareCourseLists(oldCourseList, newCourseList);
                changeList.InsertRange(0, newChangeList);
                foreach (var course in changeList)
                {
                    var item = new TreeViewItem();
                    item.DataContext = course;
                    item.PreviewMouseDown += OnTitleClick;
                    treeView.Items.Add(item);
                    item.Header = course.title;
                    var pubDateItem = new TreeViewItem();

                    pubDateItem.Header = course.pubDate;
                    var guidItem = new TreeViewItem();
                    guidItem.Header = course.guid;
                    guidItem.PreviewMouseDown += OnLinkClick;
                    item.Items.Add(pubDateItem);
                    item.Items.Add(guidItem);
                }

                oldCourseList = newCourseList;
                Dispatcher.Invoke(() =>
                {
                    labelNumOfChangesValue.Content = changeList.Count.ToString();
                });
                if (newChangeList.Count > 0)
                {
                    try
                    {
                        PlaySound();
                        if (notifyIcon != null)
                            notifyIcon.ShowBalloonTip(10000, "New ILIAS stuff (" + changeList.Count + ")!");
                        Dispatcher.Invoke(() =>
                        {
                            Visibility = Visibility.Visible;
                            this.ShowInTaskbar = true;
                        });

                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show(ex.Message);
                        });
                        return;
                    }
                }
            }
            if (msgBoxWhenFinished)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show("Finished!", "Finished", MessageBoxButton.OK, MessageBoxImage.Information);
                });
            }
        }


        private List<ILIASCourse> CompareCourseLists(List<ILIASCourse> oldList, List<ILIASCourse> newList)
        {
            List<ILIASCourse> iliasCourseList = new List<ILIASCourse>();

            foreach (var course in newList)
            {
                if (!oldList.Any(x => x.Equals(course)))
                {
                    iliasCourseList.Add(course);
                }
            }
            return iliasCourseList;
        }

        void OnTitleClick(object sender, RoutedEventArgs e)
        {

        }

        void OnLinkClick(object sender, RoutedEventArgs e)
        {
            TreeViewItem thisItem = (TreeViewItem)sender;
            ILIASCourse course = (ILIASCourse)thisItem.DataContext;
            System.Diagnostics.Process.Start(course.guid.Replace("amp;", ""));
        }

        /// <summary>
        /// XDocument to ILIASCourse parser.
        /// </summary>
        /// <param name="doc">XDocument that contains the xml doc.</param>
        /// <returns></returns>
        private List<ILIASCourse> XDocToILIASCourseList(XDocument doc)
        {
            List<ILIASCourse> courseList = new List<ILIASCourse>();

            foreach (var element in doc.Descendants("item"))
            {
                ILIASCourse course = new ILIASCourse();

                foreach (var node in element.Nodes())
                {
                    if (node.ToString().StartsWith("<title>"))
                    {
                        course.title = node.ToString().Replace("<title>", "").Replace("</title>", "");
                    }
                    else if (node.ToString().StartsWith("<guid>"))
                    {
                        course.guid = node.ToString().Replace("<guid>", "").Replace("</guid>", "");
                        ;
                    }
                    else if (node.ToString().StartsWith("<link>"))
                    {
                        course.link = node.ToString().Replace("<link>", "").Replace("</link>", "");
                        ;
                    }
                    else if (node.ToString().StartsWith("<pubDate>"))
                    {
                        course.pubDate = node.ToString().Replace("<pubDate>", "").Replace("</pubDate>", "");
                        ;
                    }
                    else if (node.ToString().StartsWith("<description>"))
                    {
                        course.description = node.ToString().Replace("<description>", "").Replace("</description>", "");
                        ;
                    }

                }

                courseList.Add(course);
            }
            return courseList;
        }

        private void btnChangeUser_Click(object sender, RoutedEventArgs e)
        {
            var dialogUserName = new MyDialog("Enter username:", false);
            if (dialogUserName.ShowDialog() == true)
            {
                Properties.Settings.Default.USERNAME = dialogUserName.ResponseText;
            }
            var dialogPassword = new MyDialog("Enter password:", true);
            if (dialogPassword.ShowDialog() == true)
            {
                Properties.Settings.Default.PASSWORD = dialogPassword.ResponseText;
            }
            Properties.Settings.Default.Save();
        }

        private void btnClear_Click(object sender, RoutedEventArgs e)
        {
            treeView.Items.Clear();
            changeList.Clear();
            labelNumOfChangesValue.Content = "0";
        }



        public event PropertyChangedEventHandler PropertyChanged;
        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void btnTestNotification_Click(object sender, RoutedEventArgs e)
        {
            PlaySound();
            notifyIcon.ShowBalloonTip(10000, "New ILIAS stuff (" + changeList.Count + ")!");
        }

        private void btnOpenILIAS_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://nbl.fh-bielefeld.de/");
        }

        private void btnChangeFeedUrl_Click(object sender, RoutedEventArgs e)
        {
            var dialogUserName = new MyDialog("Enter Feed URL:", false);
            if (dialogUserName.ShowDialog() == true)
            {
                Properties.Settings.Default.FEEDURL = dialogUserName.ResponseText.Replace("\n", "").Replace("\r", "");
                Properties.Settings.Default.Save();
            }
        }

        private void btnResetFeed_Click(object sender, RoutedEventArgs e)
        {
            oldCourseList.Clear();
            File.WriteAllText(OLD_DATA_FILE_NAME, "");
        }
    }
}
