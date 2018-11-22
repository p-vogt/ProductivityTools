using mshtml;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Navigation;

namespace CarrierWatcher
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public class Job
        {
            public string Location;
            public string Department;
            public string JobName;
        }
        private const string OLD_DATA_FILE_NAME = @"urls_old.txt";
        private const string NEW_DATA_FILE_NAME = @"urls.txt";

        private const string NEW_JOBS_FILE_NAME = @"new.txt";
        private const string REMOVED_JOBS_FILE_NAME = @"removed.txt";
        public MainWindow()
        {
            InitializeComponent();
            wb.LoadCompleted += Wb_LoadCompleted;
        }
        private int curPage;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            curPage = 1;
            analyzedJobs = 0;
            Properties.Settings.Default.Save();
            button.IsEnabled = false;
            tBoxLocation.IsEnabled = false;
            _list.Clear();
            jobsBielefeld.Clear();
            string url = @"https://jobs.dmgmori.com/main?fn=bm.ausschreibungsuebersicht&cfg_kbez=Internet";
            try
            {
                wb.Navigate(url);

            }
            catch (WebException ex)
            {
                // ex.Message -> ...Remoteserver/Remotename... = no internet
                if (!Regex.IsMatch(ex.Message.ToLower(), ".*(remoteserver|remotename).*"))
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
        }
        List<string> _list = new List<string>();
        List<Job> jobs = new List<Job>();

        List<String> jobStrings = new List<string>();
        List<string> jobsBielefeld = new List<string>();
        int analyzedJobs;
        private void Wb_LoadCompleted(object sender, NavigationEventArgs e)
        {
            string location = Properties.Settings.Default.LOCATION;
            string result = (wb.Document as IHTMLDocument2).body.outerHTML;
            Regex regexPagecnt = new Regex(@"'page_count':\s'(?<pageCount>\d+)'", RegexOptions.IgnoreCase);
            MatchCollection matchesPageCnt = regexPagecnt.Matches(result);
            int pageCount = 1;

            if (matchesPageCnt.Count > 0)
            {
                pageCount = Int32.Parse(matchesPageCnt[0].Groups["pageCount"].ToString());
            }


            Regex regexTable = new Regex(@"col_ST_PROJEKT_AUFG_GEBIET[\s\S]+?<\/TR>", RegexOptions.IgnoreCase);
            Regex regexRefnr = new Regex(@"refnr=(?<refnr>\d+)", RegexOptions.IgnoreCase);

            string template = @">(?<value>[\s\S]+?)&nbsp; <\/TD>";
            string department = "col_ST_PROJEKT_AUFG_GEBIET";
            string jobDescription = "col_ST_PROJEKT_BEZ";
            string curLocation = "col_ST_PROJEKT_ORT";
            Regex regexJob = new Regex(@"<TR(?<value>[\s\S]+?)<\/TR>", RegexOptions.IgnoreCase);
            Regex regexDepartment = new Regex(department + template, RegexOptions.IgnoreCase);
            Regex regexJobDescription = new Regex(jobDescription + template, RegexOptions.IgnoreCase);
            Regex regexLocation = new Regex(curLocation + template, RegexOptions.IgnoreCase);
            MatchCollection jobMatches = regexJob.Matches(result);


            foreach (Match jobMatch in jobMatches)
            {
                Job job = new Job();

                MatchCollection matchesDepartment = regexDepartment.Matches(jobMatch.Groups[0].ToString());
                if (matchesDepartment.Count > 0)
                {
                    job.Department = matchesDepartment[0].Groups["value"].Value;
                }
                MatchCollection matchesJobName = regexJobDescription.Matches(jobMatch.Groups[0].ToString());
                if (matchesJobName.Count > 0)
                {
                    job.JobName = matchesJobName[0].Groups["value"].Value;
                }
                MatchCollection matchesLocation = regexLocation.Matches(jobMatch.Groups[0].ToString());
                if (matchesLocation.Count > 0)
                {
                    job.Location = matchesLocation[0].Groups["value"].Value;
                }
                if (!string.IsNullOrWhiteSpace(job.Department) || !string.IsNullOrWhiteSpace(job.JobName) || !string.IsNullOrWhiteSpace(job.Location))
                {
                    jobs.Add(job);
                    jobStrings.Add(job.JobName + "##" + job.Department + "##" + job.Location);

                }
            }
            

            MatchCollection matchesTable = regexTable.Matches(result);
            foreach (Match mt in matchesTable)
            {
                MatchCollection matchesTRefnr = regexRefnr.Matches(mt.Groups[0].ToString());
                if (matchesTRefnr.Count > 0)
                {
                    String refNr = matchesTRefnr[0].Groups["refnr"].ToString();
                    if (!_list.Contains(refNr))
                    {
                        _list.Add(refNr);
                    }

                }
            }

            if (curPage < pageCount)
            {
                NextPage(pageCount);
            }
            else
            {
                //TODO
                analyzedJobs = _list.Count;
                if (analyzedJobs < _list.Count)
                {
                    string urlPrefix = "https://jobs.dmgmori.com/main?fn=bm.jobsdetail&refnr=";
                    wb.Navigate(urlPrefix + _list[analyzedJobs]);

                    if (analyzedJobs > 0 && analyzedJobs <= _list.Count)
                    {
                        string jobDesc = (wb.Document as mshtml.IHTMLDocument2).body.outerHTML;
                        Regex regexJobLocation = new Regex(@"<P>Für.*?(?<Ort>" + location + @").*?<\/P>", RegexOptions.IgnoreCase);
                        MatchCollection matchesBielefeld = regexJobLocation.Matches(jobDesc);

                        if (matchesBielefeld.Count > 0)
                        {
                            String entryText = urlPrefix + _list[analyzedJobs - 1];


                            Regex regexJobTitle = new Regex("class=\"?advTitle\"?>(?<jobTitle>.*?)<");
                            MatchCollection matchesJobTitle = regexJobTitle.Matches(jobDesc);

                            if (matchesJobTitle.Count > 0)
                            {
                                entryText += " " + matchesJobTitle[0].Groups["jobTitle"].ToString();
                            }
                            jobsBielefeld.Add(entryText);

                        }

                    }
                    analyzedJobs++;
                }
                else
                {
                    if (File.Exists(location + NEW_DATA_FILE_NAME))
                    {
                        if (File.Exists(location + OLD_DATA_FILE_NAME))
                        {
                            File.Delete(location + OLD_DATA_FILE_NAME);
                        }
                        File.Copy(location + NEW_DATA_FILE_NAME, location + OLD_DATA_FILE_NAME);
                    }
                    File.WriteAllLines(location + NEW_DATA_FILE_NAME, jobStrings);

                    button.IsEnabled = true;
                    tBoxLocation.IsEnabled = true;
                    btnCompare_Click(null, null);
                   // wb.Navigate("www.google.de/search?q=fertig");
                }

            }

        }

        private static void ExecuteScript(object doc, string js)
        {
            IHTMLDocument2 thisDoc;
            if (!(doc is IHTMLDocument2))
                return;
            else
                thisDoc = (IHTMLDocument2)doc;
            thisDoc.parentWindow.execScript(js);
        }

        private void NextPage(int pageCnt)
        {

            string nextPage = "page_form_page(" + curPage + ", { 'page_count': '" + pageCnt + "', 'current_page': '" + curPage + "', 'on_change_page': 'page_form_page', 'on_goto_page': 'gotopage_form_page', 'on_change_page_size': 'change_sizepage_form_page' });";
            ExecuteScript(wb.Document, nextPage);
            curPage++;
        }

        private void btnOpenFolder_Click(object sender, RoutedEventArgs e)
        {
            string location = Properties.Settings.Default.LOCATION;
            Process.Start("explorer.exe", "/select," + location + NEW_DATA_FILE_NAME);
        }

        private void btnCompare_Click(object sender, RoutedEventArgs e)
        {
            List<string> removedJobs;
            List<string> addedJobs;
            string location = Properties.Settings.Default.LOCATION;

            if (File.Exists(location + NEW_DATA_FILE_NAME)
             && File.Exists(location + OLD_DATA_FILE_NAME))
            {
                var oldFileContent = File.ReadAllLines(location + OLD_DATA_FILE_NAME);
                var newFileContent = File.ReadAllLines(location + NEW_DATA_FILE_NAME);
                removedJobs = oldFileContent.Except(newFileContent).ToList();
                addedJobs = newFileContent.Except(oldFileContent).ToList();

                File.WriteAllLines(REMOVED_JOBS_FILE_NAME, removedJobs);
                File.WriteAllLines(NEW_JOBS_FILE_NAME, addedJobs);

                DiffWindow diffWindow = new DiffWindow(removedJobs, addedJobs);
                diffWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Eine der Dateien existiert nicht.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
