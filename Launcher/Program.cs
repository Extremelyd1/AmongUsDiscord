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
        
        private static readonly string DISCORD_EXECUTABLE_NAME = "AmongUsDiscord.exe";
        
        private static readonly string AMONG_US_EXECUTABLE_NAME = "Among Us.exe";

        public static void Main(string[] args) {
            new Program().Init();
        }

        private Program() {
        }

        private void Init() {
            string steamDirectoryPath = GetSteamDirectoryPath();
            
            string latestReleaseUrl = GetLatestReleaseUrl();
            
            string filePath = steamDirectoryPath + AMONG_US_PATH;
            
            // Check whether discord integration file already exists
            if (File.Exists(filePath + DISCORD_EXECUTABLE_NAME)) {
                File.Delete(filePath + DISCORD_EXECUTABLE_NAME);
            }
            
            DownloadIntegration(latestReleaseUrl, filePath + DISCORD_EXECUTABLE_NAME);
            
            // Launch Among Us
            Process.Start(filePath + AMONG_US_EXECUTABLE_NAME);
            
            // Launch Among Us Discord
            Process.Start(filePath + DISCORD_EXECUTABLE_NAME);
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