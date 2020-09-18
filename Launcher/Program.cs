using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Win32;

namespace Launcher {
    internal class Program {
        private const string VersionFileUrl = "https://extremelyd1.github.io/AmongUsDiscord/latest.txt";

        private const string RegistrySteamInstallPathPath = "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam";

        private const string RegistrySteamInstallPathKeyName = "InstallPath";

        private const string AmongUsManifestPath = "\\steamapps\\appmanifest_945360.acf";

        private const string LibraryFoldersPath = "\\steamapps\\libraryfolders.vdf";
        
        private const string AmongUsPath = "\\steamapps\\common\\Among Us\\";

        private const string DiscordExecutableName = "AmongUsDiscordIntegration.exe";

        private const string AmongUsExecutableName = "Among Us.exe";

        private readonly bool _dev;

        public static void Main(string[] args) {
            if (args.Length < 1) {
                new Program(false).Init();

                return;
            }

            if (args[0].Equals("dev")) {
                new Program(true).Init();
            }
        }

        private Program(bool dev) {
            _dev = dev;
        }

        private void Init() {
            if (_dev) {
                Console.WriteLine("Launched in dev mode");
            }

            Console.WriteLine("Finding steam directory path...");

            if (!GetAmongUsSteamLibraryPath(out var amongUsSteamDirectoryPath)) {
                Console.WriteLine("Could not find Among Us installation path");
                Console.WriteLine("Exiting in 5 seconds...");
                
                Thread.Sleep(5000);
                
                return;
            }
            
            Console.WriteLine("Checking for updates...");
            
            var latestReleaseUrl = GetLatestReleaseUrl();
            
            var filePath = amongUsSteamDirectoryPath + AmongUsPath;

            if (!_dev) {
                // Check whether discord integration file already exists
                if (File.Exists(filePath + DiscordExecutableName)) {
                    File.Delete(filePath + DiscordExecutableName);
                }

                DownloadIntegration(latestReleaseUrl, filePath + DiscordExecutableName);
            }

            // Check if game is not running yet
            if (Process.GetProcessesByName("Among Us").Length == 0) {
                Console.WriteLine("Launching game...");

                // Launch Among Us
                Process.Start(filePath + AmongUsExecutableName);
            } else {
                Console.WriteLine("Game is already running...");
            }

            Console.WriteLine("Launching discord integration...");
            
            // Launch Among Us Discord
            Process.Start(filePath + DiscordExecutableName);

            Console.WriteLine("Exiting...");
        }

        private string GetLatestReleaseUrl() {
            using (var client = new WebClient()) {
                return client.DownloadString(VersionFileUrl);
            }
        }

        private bool GetAmongUsSteamLibraryPath(out string amongUsSteamLibraryPath) {
            amongUsSteamLibraryPath = "";
            
            var mainSteamDirectoryPath = GetMainSteamDirectoryPath();

            // If the manifest file for Among Us resides in the main steam directory
            // we can immediately return it
            if (File.Exists(mainSteamDirectoryPath + AmongUsManifestPath)) {
                amongUsSteamLibraryPath = mainSteamDirectoryPath;

                return true;
            }
            
            // Otherwise we have to search for the Steam Library in which Among Us is stored
            var libraryFoldersContent = ReadFileToString(mainSteamDirectoryPath + LibraryFoldersPath);

            if (!ParseLibraryFoldersFile(libraryFoldersContent, out var steamLibraryPaths)) {
                return false;
            }

            foreach (var path in steamLibraryPaths) {
                if (File.Exists(path + AmongUsManifestPath)) {
                    amongUsSteamLibraryPath = path;

                    return true;
                }
            }

            return false;
        }

        private string GetMainSteamDirectoryPath() {
            return (string) Registry.GetValue(
                RegistrySteamInstallPathPath, 
                RegistrySteamInstallPathKeyName,
                ""
            );
        }

        private bool ParseLibraryFoldersFile(string libraryFoldersContent, out List<string> steamLibraryPaths) {
            steamLibraryPaths = new List<string>();

            // Remove all spaces and tabs from the content
            libraryFoldersContent = Regex.Replace(libraryFoldersContent, "[\t ]", "");

            // Split on " character
            var libraryFoldersContentSplit = libraryFoldersContent.Split('\"');

            if (libraryFoldersContentSplit.Length < 15) {
                return false;
            }

            for (var i = 13; i < libraryFoldersContentSplit.Length; i += 4) {
                steamLibraryPaths.Add(libraryFoldersContentSplit[i]);
            }

            return true;
        }

        private void DownloadIntegration(string url, string downloadFile) {
            using (var client = new WebClient()) {
                client.DownloadFile(url, downloadFile);
            }
        }

        private string ReadFileToString(string path) {
            return File.ReadAllText(path);
        }
    }
}