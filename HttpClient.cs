using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace AmongUsDiscord {
    public class HttpClient {
        private readonly Plugin _plugin;

        // Properties for easy access to the config values
        private string Ip => _plugin.ConfigIp.Value;
        private string Port => _plugin.ConfigPort.Value.ToString();

        public HttpClient(Plugin plugin) {
            _plugin = plugin;
        }

        /// <summary>
        /// Send a get request to the given URL.
        /// </summary>
        /// <param name="url">URL to send the request to.</param>
        private static void SendGetRequest(string url) {
            try {
                var request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.PreAuthenticate = true;

                SendRequest(request);
            } catch (Exception) {
                Plugin.Log.LogWarning("The request gave an error!");
            }
        }

        /// <summary>
        /// Send a post request to the given URL with given data and content type.
        /// </summary>
        /// <param name="url">URL to send the request to.</param>
        /// <param name="data">Data to be put in the post request.</param>
        /// <param name="contentType">The content type of the data in the request.</param>
        private static void SendPostRequest(string url, string data, string contentType) {
            try {
                var dataBytes = Encoding.UTF8.GetBytes(data);

                var request = (HttpWebRequest)WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.ContentLength = dataBytes.Length;
                request.ContentType = contentType;
                request.Method = "POST";

                using (var requestBody = request.GetRequestStream()) {
                    requestBody.Write(dataBytes, 0, dataBytes.Length);
                }

                SendRequest(request);
            } catch (Exception) {
                Plugin.Log.LogWarning("The request gave an error!");
            }
        }

        /// <summary>
        /// Send a generic HTTP web request and handle the response by logging it.
        /// </summary>
        /// <param name="request">The request to send and handle its response of.</param>
        private static void SendRequest(HttpWebRequest request) {
            request.BeginGetResponse(asyncResult => {
                try {
                    using var response = request.EndGetResponse(asyncResult);
                    using var stream = response.GetResponseStream();

                    if (stream == null) {
                        Plugin.Log.LogWarning("The request has no response stream");
                        return;
                    }

                    using var reader = new StreamReader(stream);
                    Plugin.Log.LogInfo("Request response: " + reader.ReadToEnd());
                } catch (Exception) {
                    Plugin.Log.LogWarning("There was an exception handling the response of the request");
                }
            }, null);
        }

        /// <summary>
        /// Get the base URL for all used requests.
        /// </summary>
        /// <returns>The base URL as a string.</returns>
        private string GetBaseUrl() {
            return $"http://{Ip}:{Port}/among-us/";
        }

        /// <summary>
        /// Send a start request with the given list of player names as a GET request.
        /// </summary>
        /// <param name="playerNames">The list of player names to send.</param>
        public void SendStartRequest(List<string> playerNames) {
            Plugin.Log.LogInfo("Sending start request");

            var url = GetBaseUrl() + $"start?players={playerNames[0]}";

            for (var i = 1; i < playerNames.Count; i++) {
                url += $",{playerNames[i]}";
            }

            SendGetRequest(url);
        }

        /// <summary>
        /// Send a player death request with the given player name as a GET request.
        /// </summary>
        /// <param name="playerName">The name of the player that died.</param>
        public void SendPlayerDeathRequest(string playerName) {
            Plugin.Log.LogInfo("Sending death request");

            var url = GetBaseUrl() + $"death?player={playerName}";

            SendGetRequest(url);
        }

        /// <summary>
        /// Send a meeting called request as a GET request.
        /// </summary>
        public void SendMeetingCalledRequest() {
            Plugin.Log.LogInfo("Sending meeting start request");

            var url = GetBaseUrl() + "meetingstart";

            SendGetRequest(url);
        }

        /// <summary>
        /// Send a meeting end request as a GET request with the given player name as
        /// the exiled player.
        /// </summary>
        /// <param name="playerName">The name of the exiled player or null if there was no exiled player.</param>
        public void SendMeetingEndRequest(string playerName = null) {
            Plugin.Log.LogInfo("Sending meeting end request");

            var url = GetBaseUrl() + "meetingend";

            if (playerName != null) {
                url += $"?player={playerName}";
            }

            SendGetRequest(url);
        }

        /// <summary>
        /// Send a meeting end request as a GET request.
        /// </summary>
        public void SendEndRequest() {
            Plugin.Log.LogInfo("Sending end request");

            var url = GetBaseUrl() + "end";

            SendGetRequest(url);
        }
    }
}
