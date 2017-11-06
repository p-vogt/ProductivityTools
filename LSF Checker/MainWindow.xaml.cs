using FeedChecker;
using mshtml;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Navigation;
using System.Xml;
using System.Xml.Serialization;
using WpfTrayIcon;

namespace LSF_Checker
{
    public struct Module
    {
        public string Name;
        public string Grade;

        public override string ToString()
        {
            return Name + " " + Grade;
        }
    }
    enum Steps
    {
        Starting,
        Login,
        MyFunctions,
        ExamAdministration,
        GradeOverview,
        ChooseDegree,
        ExamAdmin2,
        ExternalGrades,
        ExternalGradesClick,
        Done
    }
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SimpleTrayIcon notifyIcon;
        private const string filePath = "data.bu";
        private List<Module> OldModules = new List<Module>();
        private List<Module> Modules = new List<Module>();
        Steps currentStep = Steps.Starting;
        public MainWindow()
        {
            InitializeComponent();
            browser.LoadCompleted += browser_LoadCompleted;
            cBoxGetMaster.IsChecked = Properties.Settings.Default.GET_MASTER;

            notifyIcon = new SimpleTrayIcon(this);
            notifyIcon.Visible = true;
            WindowState = WindowState.Minimized;
            Visibility = Visibility.Hidden;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            browser.Navigate("https://www.fh-bielefeld.de/qisserver/rds?state=wlogin&login=in&breadCrumbSource=portal");
            currentStep = Steps.Starting;
            lViewModules.Items.Clear();
            Modules.Clear();
            OldModules.Clear();
            if (File.Exists(filePath))
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    XmlReader reader = XmlReader.Create(fs);
                    XmlSerializer serializer = new XmlSerializer(OldModules.GetType());
                    OldModules = (List<Module>)serializer.Deserialize(reader);
                }
            }
            progressBar.IsIndeterminate = true;
        }

        private void browser_LoadCompleted(object sender, NavigationEventArgs e)
        {
            var document = browser.Document as mshtml.HTMLDocument;
            switch (currentStep)
            {
                case Steps.Starting:

                    var usernameElem = document.getElementById("asdf");
                    usernameElem.setAttribute("value", Properties.Settings.Default.USERNAME);

                    var passwordElem = document.getElementById("fdsa");
                    passwordElem.setAttribute("value", Properties.Settings.Default.PASSWORD);

                    document.getElementsByName("submit").item(0).click();


                    currentStep = Steps.ExamAdministration;
                    break;
                case Steps.ExamAdministration:
                    browser.Navigate("https://www.fh-bielefeld.de/qisserver/rds?state=change&type=1&moduleParameter=studyPOSMenu&nextdir=change&next=menu.vm&subdir=applications&xml=menu&purge=y&navigationPosition=functions%2CstudyPOSMenu&breadcrumb=studyPOSMenu&topitem=functions&subitem=studyPOSMenu");
                    currentStep = Steps.GradeOverview;
                    break;
                case Steps.GradeOverview:
                    var links = document.getElementsByTagName("a");
                    foreach (HTMLAnchorElement link in links)
                    {
                        if (link.innerText != null && link.innerText.Contains("Notenspiegel"))
                        {
                            link.click();
                        }
                    }
                    currentStep = Steps.ChooseDegree;
                    break;
                case Steps.ChooseDegree:
                    var degrees = document.getElementsByTagName("a");
                    string searchPattern = "MA";
                    if (!Properties.Settings.Default.GET_MASTER)
                    {
                        searchPattern = "BA";
                    }
                    foreach (HTMLAnchorElement degree in degrees)
                    {

                        if (degree.title != null && degree.title.Contains("Leistungen für Abschluss " + searchPattern))
                        {
                            degree.click();
                        }
                    }
                    currentStep = Steps.ExamAdmin2;
                    break;
                case Steps.ExamAdmin2:

                    GetModulesMain(); // Main grades

                    // exam administration
                    browser.Navigate("https://www.fh-bielefeld.de/qisserver/rds?state=change&type=1&moduleParameter=studyPOSMenu&nextdir=change&next=menu.vm&subdir=applications&xml=menu&purge=y&navigationPosition=functions%2CstudyPOSMenu&breadcrumb=studyPOSMenu&topitem=functions&subitem=studyPOSMenu");

                    if (Properties.Settings.Default.GET_MASTER)
                    {
                        currentStep = Steps.ExternalGrades;
                    }
                    else
                    {
                        currentStep = Steps.Done;
                    }


                    break;
                case Steps.ExternalGrades:

                    var links2 = document.getElementsByTagName("a");
                    foreach (HTMLAnchorElement link in links2)
                    {
                        if (link.innerText != null && link.innerText.Contains("Nur Noten für erbrachte Fremdleistungen"))
                        {
                            link.click();
                        }
                    }
                    currentStep = Steps.ExternalGradesClick;
                    break;
                case Steps.ExternalGradesClick:

                    var links3 = document.getElementsByTagName("a");
                    foreach (HTMLAnchorElement link in links3)
                    {
                        if (link.title != null && link.title.Contains("Leistungen für Abschluss BA"))
                        {
                            link.click();
                        }
                    }

                    currentStep = Steps.Done;
                    break;
                case Steps.Done:
                    GetModulesExternal(); // Main grades

                    lViewModules.Items.Refresh();

                    XmlSerializer x = new System.Xml.Serialization.XmlSerializer(Modules.GetType());

                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        x.Serialize(fs, Modules);
                    }


                    // Remove old

                    foreach (Module module in Modules)
                    {
                        bool foundModule = false;
                        foreach (Module oldModule in OldModules)
                        {
                            if (module.Name == oldModule.Name)
                            {
                                foundModule = true;
                            }
                        }

                        if (!foundModule)
                        {
                            lViewModules.Items.Add(module);
                        }
                    }
                    progressBar.IsIndeterminate = false;
                    if (lViewModules.Items.Count > 0)
                    {
                        if (notifyIcon != null)
                        {
                            notifyIcon.ShowBalloonTip(10000, "New LSF entry!");
                            Dispatcher.Invoke(() =>
                            {
                                Visibility = Visibility.Visible;
                                this.ShowInTaskbar = true;
                            });
                        }


                    }
                    break;
            }





        }
        private void GetModulesExternal()
        {
            var document = browser.Document as mshtml.HTMLDocument;
            string pageContent = (document as IHTMLDocument2).body.outerHTML;
            Regex regexTable = new Regex(@"\<div class=""abstand_pruefinfo""\>[\s\S]*?\<\/table\>");
            MatchCollection matches = regexTable.Matches(pageContent);
            foreach (Match match in matches)
            {
                Regex regexModule = new Regex(@"\<td align=""left""[\s\S]+?\<td align=""left"".*\>\s[ \t]*(?<moduleName>.*)[\s\S]+?\<td align=[ \S]*?class=""tabelle1_alignright"".*\>[\s\S]*?(?<grade>\d,\d)");

                MatchCollection moduleMatches = regexModule.Matches(match.Value);
                foreach (Match moduleMatch in moduleMatches)
                {
                    Module m = new Module
                    {
                        Name = moduleMatch.Groups["moduleName"].ToString(),
                        Grade = moduleMatch.Groups["grade"].ToString()
                    };
                    Modules.Add(m);
                }
            }
        }
        private void GetModulesMain()
        {
            var document = browser.Document as mshtml.HTMLDocument;
            string pageContent = (document as IHTMLDocument2).body.outerHTML;
            Regex regexTable = new Regex(@"\<div class=""abstand_pruefinfo""\>[\s\S]*?\<\/table\>");
            MatchCollection matches = regexTable.Matches(pageContent);
            foreach (Match match in matches)
            {
                Regex regexModule = new Regex(@"\<td width=""22%"" .*\>\s[ \t]*(?<moduleName>.*)[\s\S]+?\<td width=""8%""[ \S]*?class=""tabelle1_alignright"".*\>[\s\S]*?(?<grade>\d,\d)");

                MatchCollection moduleMatches = regexModule.Matches(match.Value);
                foreach (Match moduleMatch in moduleMatches)
                {
                    Module m = new Module
                    {
                        Name = moduleMatch.Groups["moduleName"].ToString(),
                        Grade = moduleMatch.Groups["grade"].ToString()
                    };
                    Modules.Add(m);
                }
            }
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            //switch (currentStep)
            {
                // case Steps.Login:
                var document = browser.Document as mshtml.HTMLDocument;
                document.getElementsByName("submit").item(0).click();
                // break;
            }
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

        private void cBoxGetMaster_Checked(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.GET_MASTER = cBoxGetMaster.IsChecked == true;
            Properties.Settings.Default.Save();
        }
    }
}
