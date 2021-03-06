﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Git_Svn_Console
{
    public class GitSvnClient
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
            ClearCurrentInput();
            WinAPI.SendString("git svn dcommit\n", consoleHandle);
        }

        public void GetGitBranches(out List<string> gitBranchList, out string currentGitBranch)
        {
            ClearCurrentInput();
            var regexBranches = new Regex(@"^\s*(?<branchName>\S+?)?$", RegexOptions.Multiline);
            var regexCurrentBranch = new Regex(@"\*\s(?<branchName>\S+)?", RegexOptions.Multiline);

            var response = GetCommandResponse("git branch --list", 5000).Trim();

            gitBranchList = new List<string>();

            gitBranchList = new List<string>(response.Replace("*", "").Split(' '));

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

        internal void CloneRepository(string sourceRepo, string destinationFolder, string revision)
        {
            if (sourceRepo == null)
            {
                throw new NullReferenceException(nameof(sourceRepo));
            }
            if (destinationFolder == null)
            {
                throw new NullReferenceException(nameof(destinationFolder));
            }
            if (revision == null)
            {
                throw new NullReferenceException(nameof(revision));
            }
            destinationFolder = destinationFolder.Replace("\\", "/");
            Directory.CreateDirectory(destinationFolder);
            if (Directory.Exists(destinationFolder))
            {
                ChangeDirectoy(destinationFolder);

                WinAPI.SendString($"git svn clone -r{revision}:HEAD {sourceRepo}\n", consoleHandle);
            }
            else
            {
                // TODO ERROR 
            }

        }
        public void CreateSvnBranch(string branchName)
        {
            string output = GetCommandResponse($"git svn branch -n -m \"create Branch {branchName}\"  {branchName}", 10000);
            GetCommandResponse($"git svn branch -m \"create Branch {branchName}\"  {branchName}", 10000);
        }
        public string GetCurrentSvnLocation()
        {
            ClearCurrentInput();
            string output = GetCommandResponse("git svn info --url", 10000);

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

        internal void ChangeDirectoy(string destination)
        {
            WinAPI.SendString($"cd {destination}\n", consoleHandle);
        }

        // to prevent simultaneous readings at one file
        static int commandResponseCounter = 0;
        private object commandLockObject = new Object();

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

        public static readonly string TEMP_CMD_DIRECTORY = @"c:\temp\";
        public static readonly string TEMP_CMD_FILE_NAME = @"cmd_";
        public static readonly string TEMP_CMD_FILE_NAME_REGEX = @"cmd_\d+.TMP";
        private string GetCommandResponse(string command, long timeoutMs)
        {
            ClearCurrentInput();
            var tempFileName = $"{TEMP_CMD_DIRECTORY}{TEMP_CMD_FILE_NAME}{CommandResponseCounter}.TMP";
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

            if (File.Exists(tempFileName))
            {
                File.Delete(tempFileName);
            }
            return output;
        }

        public void Checkout(string branchname)
        {
            ClearCurrentInput();
            WinAPI.SendString($"git checkout {branchname}\n", consoleHandle);
        }


        public string DetermineWorkingDirectory()
        {
            return GetCommandResponse("pwd", 5000);
        }

        public void Fetch()
        {
            ClearCurrentInput();
            WinAPI.SendString($"git svn rebase\n", consoleHandle);
        }

        public void ClearCurrentInput()
        {
            WinAPI.SendCtrlChar('U', consoleHandle);
        }

        public int GetCurrentSvnRevision(string repoUrl)
        {
            var response = GetCommandResponse($"svn info {repoUrl}", 3000);
            var revisionNumber = "-1";
            var regex = new Regex(@"Revision: (?<number>\d+)");
            if (regex.IsMatch(response))
            {
                revisionNumber = regex.Match(response).Groups["number"].ToString();
            }

            return int.Parse(revisionNumber);
        }

        public void CreateGitSvnBranch(string branchName)
        {
            var output = GetCommandResponse($"git checkout -b {branchName} origin/{branchName}", 10000);
        }
    }
}
