using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using VrcLovenseConnect.Helpers;

namespace LovenseConnect {
    [Obfuscation(Exclude = true, ApplyToMembers = true, StripAfterObfuscation = true)]
    public class LovenseConnectApi {
        public class LovenseToy {
            #region Debug Info
            public string FVersion { get; set; } = "0";
            public string Version { get; set; } = "";
            #endregion

            #region Useful Info
            public string Name { get; set; } = "Unknown";
            public string Battery { get; set; } = "0%";
            public int Status { get; set; } = 0;
            public string Id { get; set; } = "";
            public string NickName { get; set; } = "";

            public string VibrateUrl { get; set; } = "";
            public string RotateUrl { get; set; } = "";
            public string PumpUrl { get; set; } = "";

            public bool VibrateUnsupported { get; set; }
            public bool LinearUnsupported { get; set; }
            public bool RotateUnsupported { get; set; }
            #endregion
        }

        private static HttpClient Client { get; set; } = new HttpClient();
        public static List<LovenseToy> Toys { get; set; } = new List<LovenseToy>();

        public static async Task<List<LovenseToy>?> GetToys(string url, int stopDelay = 2) {
            if (string.IsNullOrEmpty(url))
                return null;

            IsRequestPending = true;
            using var packet = await Client.GetAsync(new Uri(url + "/GetToys"));
            using var content = packet.Content;
            var response = await content.ReadAsStringAsync();
            IsRequestPending = false;

            if (!response.ToLower().Contains("\"ok\"") || response == "{}")
                return null;

            JObject jsonData = JObject.Parse(response);
            if (jsonData?.GetValue("code")?.ToString() != "200" || jsonData?.GetValue("type")?.ToString().ToLower() != "ok")
                return null;

            List<LovenseToy> toys = new();
            JObject data = JObject.Parse(jsonData?.GetValue("data")?.ToString() ?? string.Empty);

            foreach (JProperty toyid in data.Properties()) {
                LovenseToy? toy = JsonConvert.DeserializeObject<LovenseToy>(toyid.Value.ToString());
                if (toy != null && toy.Status != 0) {
                    switch (toy.Name.ToLower()) {
                        case "max":
                            toy.VibrateUrl = url + "/AVibrate?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.PumpUrl = url + "/AAirLevel?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.RotateUnsupported = true;
                            break;
                        case "nora":
                            toy.VibrateUrl = url + "/AVibrate?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.RotateUrl = url + "/ARotate?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.LinearUnsupported = true;
                            break;
                        case "edge":
                            toy.VibrateUrl = url + "/AVibrate?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.RotateUnsupported = true;
                            toy.LinearUnsupported = true;
                            break;
                        default:
                            toy.VibrateUrl = url + "/AVibrate?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.PumpUrl = url + "/AAirLevel?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            toy.RotateUrl = url + "/ARotate?t=" + toy.Id + "&sec=" + stopDelay + "&";
                            break;
                    }
                    toys.Add(toy);
                }
            }

            Toys = toys;
            return toys;
        }

        private static Dictionary<(string, string), int> CurrentLovenseAmount = new Dictionary<(string, string), int>();

        public static bool IsRequestPending { get; set; } = false;

        public static int RqCount { get; set; } = 0;

        public static Stopwatch DelayWatch { get; set; } = new Stopwatch();

        private static async Task<bool> SendRq(String Url, String UrlParams) {
            var packet = await Client.GetAsync(Url + UrlParams);
            var response = await packet.Content.ReadAsStringAsync();
            if (!string.IsNullOrEmpty(response) && !response.ToLower().Contains("\"ok\"")) {
                IsRequestPending = false;
                return false;
            }
            return true;
        }

        public static async Task<bool> VibrateToy(string url, LovenseToy toy, int amount, bool ignoreDuplicateRequests = false) {
            try {
                if (!await InitCommand(toy, amount, ignoreDuplicateRequests, "Vibrate"))
                    return false;

                RqCount++;
                ConsoleHelper.Tell(RqCount + ". Vibrating \"" + toy.Name + "\" (" + amount + ")");
                if (await SendRq(toy.VibrateUrl, "v=" + amount))
                    return await EndCommand(toy.Id, "Vibrate", amount);

                return false;
            } catch {
                IsRequestPending = false;
                return false;
            }
        }

        public static async Task<bool> RotateToy(string url, LovenseToy toy, int amount, bool ignoreDuplicateRequests = false) {
            try {
                if (!await InitCommand(toy, amount, ignoreDuplicateRequests, "Rotate"))
                    return false;

                RqCount++;
                ConsoleHelper.Tell(RqCount + ". Rotating  \"" + toy.Name + "\" (" + amount + ")");
                if (await SendRq(toy.RotateUrl, "v=" + amount))
                    return await EndCommand(toy.Id, "Rotate", amount);

                return false;
            } catch {
                IsRequestPending = false;
                return false;
            }
        }

        public static async Task<bool> PumpToy(string url, LovenseToy toy, int amount, bool ignoreDuplicateRequests = false) {
            try {
                if (!await InitCommand(toy, amount, ignoreDuplicateRequests, "AirAuto"))
                    return false;

                RqCount++;
                ConsoleHelper.Tell(RqCount + ". Pumping   \"" + toy.Name + "\" (" + amount + ")");
                if (await SendRq(toy.PumpUrl, "a=" + amount))
                    return await EndCommand(toy.Id, "AirAuto", amount);

                return false;
            } catch {
                IsRequestPending = false;
                return false;
            }
        }

        private static async Task<bool> InitCommand(LovenseToy toy, int amount, bool ignoreDuplicateRequests, string command) {
            if (ignoreDuplicateRequests && CurrentLovenseAmount != null && CurrentLovenseAmount.ContainsKey((toy.Id, command)) && CurrentLovenseAmount[(toy.Id, command)] == amount) {
                Console.WriteLine("(Skip: Duplicate " + command + amount + ")");
                return false;
            }

            if (IsRequestPending) {
                Console.WriteLine("(Skip: Pending " + command + amount + ")");
                return false;
            }

            DelayWatch.Reset();
            if (Toys == null || Toys.Count == 0)
                return false;

            IsRequestPending = true;
            if (!DelayWatch.IsRunning)
                DelayWatch.Start();

            return true;
        }

        private static async Task<bool> EndCommand(string id, string command, int amount) {
            if (DelayWatch.IsRunning)
                DelayWatch.Stop();

            if (CurrentLovenseAmount != null)
                CurrentLovenseAmount[(id, command)] = amount;

            IsRequestPending = false;
            // await Task.Delay(20);
            return true;
        }
    }
}