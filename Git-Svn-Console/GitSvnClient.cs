using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Git_Svn_Console
{
    class GitSvnClient
    {
        public const string ERROR_INDICATOR = "<ERROR>";
        private IntPtr consoleHandle;
        private string workingDirectory;
        public GitSvnClient(string workingDir, IntPtr consoleHandle)
        {
            this.workingDirectory = workingDir;
            this.consoleHandle = consoleHandle;
        }

        public void Commit()
        {
            WinAPI.SendString("git svn dcommit\n", consoleHandle);
        }

        public void GetGitBranches(out List<string> gitBranchList, out string currentGitBranch)
        {
            var regexBranches = new Regex(@"^\s*(?<branchName>\S+?)?$", RegexOptions.Multiline);
            var regexCurrentBranch = new Regex(@"\*\s(?<branchName>\S+)?", RegexOptions.Multiline);

            var response = GetCommandResponse("git branch --list",5000).Trim();

            gitBranchList = new List<string>();

            gitBranchList = new List<string>(response.Replace("*","").Split(' '));

            // remove empty entries
            gitBranchList.RemoveAll(str => string.IsNullOrWhiteSpace(str));

            currentGitBranch = regexCurrentBranch.Match(response).Groups["branchName"].ToString();

            // set master to position 1
            if (gitBranchList.Contains("master"))
            {
                gitBranchList.Remove("master");
                gitBranchList.Insert(0, "master");
            }
        }


        public string GetCurrentSvnBranch()
        {
            string output = GetCommandResponse("git svn info --url",10000);

            // split type and "branch"
            // ^.*\/(?<category>.+?)\/(?<branch>.+?)\n$

            // type/"branch"
            // ^.*\/(?<branch>.+?\/.+?)\n$
            var regex = new Regex(@"^.*\/(?<repo>.+?)\/(?<category>.+?)\/(?<branch>.+?)$", RegexOptions.Multiline | RegexOptions.Compiled);
            if (regex.IsMatch(output))
            {
                var repo = regex.Match(output).Groups["repo"].ToString();
                var category = regex.Match(output).Groups["category"].ToString();
                var branch = regex.Match(output).Groups["branch"].ToString();

                // trunk
                if (repo == "svn")
                {
                    repo = category;
                    category = "";
                }
                return category != "" ? $"{repo}\n{category}/{branch}" :
                                        $"{repo}\n{branch}";
            }
            return ERROR_INDICATOR;
        }

        // to prevent simultaneous readings at one file
        static int commandResponseCounter = 0;
        private Object commandLockObject = new Object();

        private int CommandResponseCounter
        {
            get
            {
                lock (commandLockObject)
                {
                    commandResponseCounter++;
                    return commandResponseCounter;
                }
            }
        }

        private string GetCommandResponse(string command, long timeoutMs)
        {
            var tempFileName = $"c:\\temp\\cmd_{CommandResponseCounter}.TMP";
            Directory.CreateDirectory("c:\\temp");
            WinAPI.SendString($"{command} > \"{tempFileName}\"\n", consoleHandle);
            var sw = new Stopwatch();
            sw.Start();
            var output = "";
            while (sw.ElapsedMilliseconds <= timeoutMs)
            {
                if (File.Exists(tempFileName))
                {
                    try
                    {
                        output = File.ReadAllText(tempFileName).Replace("\n", "");
                        break;
                    }
                    catch (IOException)
                    {
                        Console.WriteLine(sw.ElapsedMilliseconds);
                        // ignored, git bash hasn't finished yet
                    }
                    Thread.Sleep(100);
                }
            }

            if(File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }
            return output;
        }

        public void Checkout(string branchname)
        {
            WinAPI.SendString($"git checkout {branchname}\n", consoleHandle);
        }


        public string DetermineWorkingDirectory()
        {
            return GetCommandResponse("pwd",5000);
        }
    }
}
