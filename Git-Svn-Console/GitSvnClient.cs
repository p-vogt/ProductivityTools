using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Git_Svn_Console
{
    class GitSvnClient
    {
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
            var cmd = "/C git svn info --url";
            var output = WinAPI.PerformShellCommand(workingDirectory, cmd);
            // split type and "branch"
            // ^.*\/(?<category>.+?)\/(?<branch>.+?)\n$

            // type/"branch"
            // ^.*\/(?<branch>.+?\/.+?)\n$
            var regex = new Regex(@"^.*\/(?<repo>.+?)\/(?<category>.+?)\/(?<branch>.+?)\n$", RegexOptions.Multiline | RegexOptions.Compiled);
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
            return "<ERROR>";
        }

        public void Checkout(string branchname)
        {
            WinAPI.SendString($"git checkout {branchname}\n", consoleHandle);
        }


        public string GetWorkingDirectory()
        {
            WinAPI.SendString("pwd\n", consoleHandle);
            return "";
        }
    }
}
