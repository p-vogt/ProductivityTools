using mshtml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
using System.Xml.Linq;

namespace CarrierWatcher
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const string OLD_DATA_FILE_NAME = @"urls_old.txt";
        private const string NEW_DATA_FILE_NAME = @"urls.txt";
        public MainWindow()
        {
            InitializeComponent();
            wb.LoadCompleted += Wb_LoadCompleted;
        }
        private int curPage = 1;
        private void button_Click(object sender, RoutedEventArgs e)
        {
            button.IsEnabled = false;
            _list.Clear();
            jobsBielefeld.Clear();
            finishedProcess = false;
            using (var client = new WebClient())
            {
                client.Encoding = Encoding.UTF8;

                client.Proxy = null;                
               String url = @"https://jobs.dmgmori.com/main?fn=bm.ausschreibungsuebersicht&cfg_kbez=Internet";
                try
                {
                    string result = client.DownloadString(url);
                   

                    //String test = wb.Document.ToString();
                   
                    wb.Navigate(url);
                    

                    


            
                    
                }
                catch (WebException ex)
                {
                    // ex.Message -> ...Remoteserver/Remotename... = no internet
                    if (!Regex.IsMatch(ex.Message.ToLower(), ".*(remoteserver|remotename).*"))
                    {
                        MessageBox.Show("Feed URL / Username / Passwort falsch?\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Feed URL / Username / Passwort falsch?\n\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

            }
            }
        List<String> _list = new List<String>();
        List<String> jobsBielefeld = new List<String>();
        int analyzedJobs = 0;
        bool finishedProcess = false;
        private void Wb_LoadCompleted(object sender, NavigationEventArgs e)
        {
            string result = (wb.Document as mshtml.IHTMLDocument2).body.outerHTML;
            Regex regexPagecnt = new Regex(@"'page_count':\s'(?<pageCount>\d+)'", RegexOptions.IgnoreCase);
            MatchCollection matchesPageCnt = regexPagecnt.Matches(result);
            int pageCount = 1;

            if (matchesPageCnt.Count > 0)
            {
                pageCount = Int32.Parse(matchesPageCnt[0].Groups["pageCount"].ToString());
            }


            Regex regexTable = new Regex(@"<tr class.*?>(.|\s)+?<\/tr>", RegexOptions.IgnoreCase);
            Regex regexRefnr = new Regex(@"refnr=(?<refnr>\d+)", RegexOptions.IgnoreCase);

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

            if(curPage<pageCount)
            {
                NextPage(pageCount);
            } else
            {
                if(analyzedJobs<_list.Count)
                {
                    String urlPrefix = "https://jobs.dmgmori.com/main?fn=bm.jobsdetail&refnr=";
                    wb.Navigate(urlPrefix + _list[analyzedJobs]);

                    if (analyzedJobs>0 && analyzedJobs <= _list.Count)
                    {
                        string jobDesc = (wb.Document as mshtml.IHTMLDocument2).body.outerHTML;
                        Regex regexJobLocation = new Regex(@"<P>Für.*?(?<Ort>BIELEFELD).*?<\/P>", RegexOptions.IgnoreCase);
                        MatchCollection matchesBielefeld = regexJobLocation.Matches(jobDesc);
                        if (matchesBielefeld.Count > 0)
                        {
                            jobsBielefeld.Add(urlPrefix + _list[analyzedJobs-1]);
                        }
                        
                    }
                    analyzedJobs++;
                }
                else
                {
                    finishedProcess = true;
                    if (File.Exists(NEW_DATA_FILE_NAME))
                    {
                        if (File.Exists(OLD_DATA_FILE_NAME))
                        {
                            File.Delete(OLD_DATA_FILE_NAME);
                        }
                        File.Copy(NEW_DATA_FILE_NAME, OLD_DATA_FILE_NAME);
                    }
                    File.WriteAllLines(NEW_DATA_FILE_NAME, jobsBielefeld);
                    button.IsEnabled = true;
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
            string nextPage = "page_form_page(" + curPage + ", { 'page_count': '"+ pageCnt+"', 'current_page': '" + curPage + "', 'on_change_page': 'page_form_page', 'on_goto_page': 'gotopage_form_page', 'on_change_page_size': 'change_sizepage_form_page' });";
            ExecuteScript(wb.Document, nextPage);
            curPage++;
        }
    }
}
