using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace AmongUsDiscordIntegration {
    public class HttpClient {
        private readonly string _ip;
        private readonly string _port;

        public HttpClient(string ip, string port) {
            _ip = ip;
            _port = port;
        }

        private string SendGetRequest(string url) {
            try {
                var request = (HttpWebRequest) WebRequest.Create(url);
                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.PreAuthenticate = true;

                using var response = (HttpWebResponse) request.GetResponse();
                using var stream = response.GetResponseStream();
                
                if (stream == null) {
                    Console.WriteLine("The request has no response");
                    return "";
                }
                
                using var reader = new StreamReader(stream);
                
                return reader.ReadToEnd();
            }
            catch (Exception) {
                Console.WriteLine("The request gave an error!");
            }

            return "";
        }

        private string SendPostRequest(string url, string data, string contentType, string method = "POST") {
            var dataBytes = Encoding.UTF8.GetBytes(data);

            var request = (HttpWebRequest) WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            request.ContentLength = dataBytes.Length;
            request.ContentType = contentType;
            request.Method = method;

            using (var requestBody = request.GetRequestStream()) {
                requestBody.Write(dataBytes, 0, dataBytes.Length);
            }

            using var response = (HttpWebResponse) request.GetResponse();
            using var stream = response.GetResponseStream();
                
            if (stream == null) {
                Console.WriteLine("The request has no response");
                return "";
            }
                
            using var reader = new StreamReader(stream);
                
            return reader.ReadToEnd();
        }

        private string GetBaseUrl() {
            return $"http://{_ip}:{_port}/among-us/";
        }

        public string SendStartRequest(List<string> playerNames) {
            Console.WriteLine("Sending start request");
            
            var url = GetBaseUrl() + $"start?players={playerNames[0]}";

            for (var i = 1; i < playerNames.Count; i++) {
                url += $",{playerNames[i]}";
            }

            return SendGetRequest(url);
        }

        public string SendPlayerDeathRequest(string playerName) {
            Console.WriteLine("Sending death request");
            
            var url = GetBaseUrl() + $"death?player={playerName}";

            return SendGetRequest(url);
        }

        public string SendMeetingCalledRequest() {
            Console.WriteLine("Sending meeting start request");
            
            var url = GetBaseUrl() + "meetingstart";

            return SendGetRequest(url);
        }
        
        public string SendMeetingEndRequest() {
            Console.WriteLine("Sending meeting end request");
            
            var url = GetBaseUrl() + "meetingend";

            return SendGetRequest(url);
        }

        public string SendEndRequest() {
            Console.WriteLine("Sending end request");
            
            var url = GetBaseUrl() + "end";

            return SendGetRequest(url);
        }
    }
}