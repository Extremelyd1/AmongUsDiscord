using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using Microsoft.Win32;

namespace Launcher {
    internal class Program {
        private static readonly string VERSION_FILE_URL = "https://extremelyd1.github.io/AmongUsDiscord/latest.txt";

        private static readonly string REGISTRY_STEAM_INSTALL_PATH_PATH = 
            "HKEY_LOCAL_MACHINE\\SOFTWARE\\WOW6432Node\\Valve\\Steam";

        private static readonly string REGISTRY_STEAM_INSTALL_PATH_KEY_NAME = "InstallPath";

        private static readonly string AMONG_US_PATH = "\\steamapps\\common\\Among Us\\";
        
        private static readonly string DISCORD_EXECUTABLE_NAME = "AmongUsDiscordIntegration.exe";
        
        private static readonly string AMONG_US_EXECUTABLE_NAME = "Among Us.exe";

        private bool _dev;

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
            
            string steamDirectoryPath = GetSteamDirectoryPath();
            
            Console.WriteLine("Checking for updates...");
            
            string latestReleaseUrl = GetLatestReleaseUrl();
            
            string filePath = steamDirectoryPath + AMONG_US_PATH;

            if (!_dev) {
                // Check whether discord integration file already exists
                if (File.Exists(filePath + DISCORD_EXECUTABLE_NAME)) {
                    File.Delete(filePath + DISCORD_EXECUTABLE_NAME);
                }

                DownloadIntegration(latestReleaseUrl, filePath + DISCORD_EXECUTABLE_NAME);
            }

            // Check if game is not running yet
            if (Process.GetProcessesByName("Among Us").Length == 0) {
                Console.WriteLine("Launching game...");

                // Launch Among Us
                Process.Start(filePath + AMONG_US_EXECUTABLE_NAME);
            } else {
                Console.WriteLine("Game is already running...");
            }

            Console.WriteLine("Launching discord integration...");
            
            // Launch Among Us Discord
            Process.Start(filePath + DISCORD_EXECUTABLE_NAME);

            Console.WriteLine("Exiting...");
        }

        private string GetLatestReleaseUrl() {
            using (var client = new WebClient()) {
                return client.DownloadString(VERSION_FILE_URL);
            }
        }

        private string GetSteamDirectoryPath() {
            return (string) Registry.GetValue(
                REGISTRY_STEAM_INSTALL_PATH_PATH, 
                REGISTRY_STEAM_INSTALL_PATH_KEY_NAME,
                ""
            );
        }

        private void DownloadIntegration(string url, string downloadFile) {
            using (var client = new WebClient()) {
                client.DownloadFile(url, downloadFile);
            }
        }
    }
}