﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Git_Svn_Console
{
    class GitSvnClient
    {
        private const string ERROR_INDICATOR = "<ERROR>";
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

        public void GetGitBranches(Regex regexBranches, Regex regexCurrentBranch, out List<string> gitBranchList, out string currentGitBranch)
        {
            var output = WinAPI.PerformShellCommand(workingDirectory, "/C git branch --list");

            gitBranchList = new List<string>();
            foreach (Match match in regexBranches.Matches(output))
            {
                gitBranchList.Add(match.Groups["branchName"].ToString());
            }
            currentGitBranch = regexCurrentBranch.Match(output).Groups["branchName"].ToString();

            // set master to position 1
            if (gitBranchList.Contains("master"))
            {
                gitBranchList.Remove("master");
                gitBranchList.Insert(0, "master");
            }
        }

        public string GetCurrentSvnBranch()
        {
            const string tempFileName = "c:\\temp\\svnbranch.TMP";
            Directory.CreateDirectory("c:\\temp");
            WinAPI.SendString($"git svn info --url > \"{tempFileName}\"\n", consoleHandle);
            Thread.Sleep(1000); // git svn info needs some time
            var output = "";
            if (File.Exists(tempFileName))
            {
                // when git svn info hasn't finished yet
                var numTries = 0;
                while(numTries < 30)
                {
                    try
                    {
                        ++numTries;
                        output = File.ReadAllText(tempFileName).Replace("\n", "");
                    }
                    catch (IOException ex)
                    {
                        Thread.Sleep(100);
                    }
                }
                
                
                File.Delete(tempFileName);
            }

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

        public void Checkout(string branchname)
        {
            WinAPI.SendString($"git checkout {branchname}\n", consoleHandle);
        }


        public string DetermineWorkingDirectory()
        {
            const string pwdFileName = "c:\\temp\\pwd.TMP";
            Directory.CreateDirectory("c:\\temp");

            WinAPI.SendString($"pwd > \"{pwdFileName}\"\n", consoleHandle);
            Thread.Sleep(100);
            if (File.Exists(pwdFileName))
            {
                workingDirectory = File.ReadAllText(pwdFileName).Replace("\n", "");
                File.Delete(pwdFileName);
                return workingDirectory;
            }
            return "";
        }
    }
}
