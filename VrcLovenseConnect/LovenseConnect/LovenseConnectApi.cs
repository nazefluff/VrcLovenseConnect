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

/// <summary>
/// The Namespace Of The Lovense Connect API; Created By Plague.
/// </summary>

namespace LovenseConnect
{
    /// <summary>
    /// The Main Class Of The Lovense Connect API; Created By Plague.
    /// </summary>
    [Obfuscation(Exclude = true, ApplyToMembers = true, StripAfterObfuscation = true)]
    public class LovenseConnectApi
    {
        /// <summary>
        /// Information About The Lovense Toy, Held In A Convenient Package.
        /// </summary>
        public class LovenseToy
        {
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

            public bool AllUnsupported { get; set; }
            public bool VibrateUnsupported { get; set; }
            public bool LinearUnsupported { get; set; }
            public bool RotateUnsupported { get; set; }
            #endregion
        }

        /// <summary>
        /// The Local WebClient To Send Requests.
        /// </summary>
        private static HttpClient Client { get; set; } = new HttpClient();

        /// <summary>
        /// Toys Caching For Lovense To Function Efficiently.
        /// </summary>
        public static List<LovenseToy> Toys { get; set; } = new List<LovenseToy>();

        /// <summary>
        /// Gets A List Of Toys Connected To This Local Lovense Connect Server URL.
        /// </summary>
        /// <param name="url">The Local Lovense Connect Server URL.</param>
        /// <returns>A List Of Toys Connected To This Local Lovense Connect Server URL.</returns>
        public static async Task<List<LovenseToy>?> GetToys(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            IsRequestPending = true;

            using var packet = await Client.GetAsync(new Uri(url.ToLower().Replace("/gettoys", "") + "/GetToys"));
            using var content = packet.Content;
            var response = await content.ReadAsStringAsync();

            IsRequestPending = false;

            if (!response.ToLower().Contains("\"ok\""))
            {
                return null;
            }

            if (response == "{}")
            {
                return null;
            }

            JObject jsonData = JObject.Parse(response);

            if (jsonData?.GetValue("code")?.ToString() != "200" || jsonData?.GetValue("type")?.ToString().ToLower() != "ok")
            {
                return null;
            }

            List<LovenseToy> toys = new();

            JObject data = JObject.Parse(jsonData?.GetValue("data")?.ToString() ?? string.Empty);

            foreach (JProperty toyid in data.Properties())
            {
                LovenseToy? toy = JsonConvert.DeserializeObject<LovenseToy>(toyid.Value.ToString());
                
                if (toy != null && toy.Status != 0)
                    toys.Add(toy);
            }

            Toys = toys;

            return toys;
        }

        /// <summary>
        /// Gets A Instance Of LovenseToy Which Contains Info About The Toy From The URL And ID.
        /// </summary>
        /// <param name="url">The Local Lovense Connect Server URL.</param>
        /// <param name="id">The Toy ID - Fun Fact: This Is The Device's MAC Address.</param>
        /// <returns>The LovenseToy Instance Containing Info About The Toy, Such As Battery Percentage, Type, Etc.</returns>
        public static async Task<LovenseToy?> GetToyInfoFromID(string url, string id)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(id))
            {
                return null;
            }

            return (await GetToys(url))?.FirstOrDefault(o => o.Id == id);
        }

        /// <summary>
        /// The Current Lovense Amount Of All Toy IDs.
        /// </summary>
        private static Dictionary<(string, string), int> CurrentLovenseAmount = new Dictionary<(string, string), int>();

        /// <summary>
        /// Is A Lovense Request In Progress?
        /// </summary>
        public static bool IsRequestPending { get; set; } = false;

        /// <summary>
        /// The Last Known Latency Recorded.
        /// </summary>
        public static int LastKnownLatency { get; set; } = 225;

        /// <summary>
        /// Stopwatch Used For Calculating Latency.
        /// </summary>
        public static Stopwatch DelayWatch { get; set; } = new Stopwatch();

        public static async Task<bool> AllToy(string url, string id, int amount, bool ignoreDuplicateRequests = false)
        {
            try
            {
                if (!await InitCommand(id, amount, ignoreDuplicateRequests, "All"))
                    return false;

                amount = 0;
                var toy = Toys?.Find(o => o.Id == id);
                ConsoleHelper.Tell("Everything Toy " + toy?.Name + " (v=" + amount + ", t=" + toy?.Id + ")");

                using var packet1 = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate?v=" + amount + "&t=" + toy?.Id ?? string.Empty);
                using var packet2 = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate1?v=" + amount + "&t=" + toy?.Id ?? string.Empty);
                using var packet3 = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate2?v=" + amount + "&t=" + toy?.Id ?? string.Empty);
                using var packet4 = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Rotate?v=" + amount + "&t=" + toy?.Id ?? string.Empty);
                using var packet5 = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/AirAuto?v=" + amount + "&t=" + toy?.Id ?? string.Empty);

                using var content1 = packet1.Content;
                using var content2 = packet2.Content;
                using var content3 = packet3.Content;
                using var content4 = packet4.Content;
                using var content5 = packet5.Content;

                var response1 = (await content1.ReadAsStringAsync()).ToLower().Contains("\"ok\"");
                var response2 = (await content3.ReadAsStringAsync()).ToLower().Contains("\"ok\"");
                var response3 = (await content2.ReadAsStringAsync()).ToLower().Contains("\"ok\"");
                var response4 = (await content4.ReadAsStringAsync()).ToLower().Contains("\"ok\"");
                var response5 = (await content5.ReadAsStringAsync()).ToLower().Contains("\"ok\"");

                if (
                    !response1 &&
                    !response2 &&
                    !response3 &&
                    !response4 &&
                    !response5
                    )
                    return false;

                IsRequestPending = false;
                return await EndCommand(id, "Vibrate", amount);
            }
            catch
            {
                IsRequestPending = false;

                return false;
            }
        }

        /// <summary>
        /// A Simple "Vibrate This Much" Method Which Will Vibrate The Toy {x} Amount From The Local Lovense Connect Server URL And ID Of The Toy.
        /// </summary>
        /// <param name="url">The Local Lovense Connect Server URL.</param>
        /// <param name="id">The Toy ID - Fun Fact: This Is The Device's MAC Address.</param>
        /// <param name="amount">The Vibration Intensity.</param>
        /// <param name="ignoreDuplicateRequests">Whether To Ignore Duplicate Requests Or Not.</param>
        /// <returns></returns>
        public static async Task<bool> VibrateToy(string url, string id, int amount, bool ignoreDuplicateRequests = false)
        {
            try
            {
                if (!await InitCommand(id, amount, ignoreDuplicateRequests, "Vibrate"))
                    return false;

                var toy = Toys?.Find(o => o.Id == id);
                ConsoleHelper.Tell("Vibrating Toy " + toy?.Name + " (v=" + amount + ", t=" + toy?.Id + ")");
                using var packet = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate?v=" + amount + "&t=" + toy?.Id ?? string.Empty);
                using var content = packet.Content;
                var response = await content.ReadAsStringAsync();

                if (!string.IsNullOrEmpty(response) && !response.ToLower().Contains("\"ok\""))
                {
                    IsRequestPending = false;

                    return false;
                }

                if (toy?.Name.ToLower().Contains("edge") ?? false)
                {
                    await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate1?v=" + amount + "&t=" + toy.Id);

                    using var vibrate2Packet = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate2?v=" + amount + "&t=" + toy.Id);
                    using var vibrate2Content = vibrate2Packet.Content;
                    response = await vibrate2Content.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(response) && !response.ToLower().Contains("\"ok\""))
                    {
                        IsRequestPending = false;

                        return false;
                    }
                }
                else
                {
                    using var vibratePacket = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Vibrate?v=" + amount + "&t=" + toy?.Id);
                    using var vibrateContent = vibratePacket.Content;
                    response = await vibrateContent.ReadAsStringAsync();

                    if (!string.IsNullOrEmpty(response) && !response.ToLower().Contains("\"ok\""))
                    {
                        IsRequestPending = false;

                        return false;
                    }
                }

                return await EndCommand(id, "Vibrate", amount);
            }
            catch
            {
                IsRequestPending = false;

                return false;
            }
        }

        /// <summary>
        /// A Simple "Rotate This Much" Method Which Will Rotate The Toy {x} Amount From The Local Lovense Connect Server URL And ID Of The Toy.
        /// </summary>
        /// <param name="url">The Local Lovense Connect Server URL.</param>
        /// <param name="id">The Toy ID - Fun Fact: This Is The Device's MAC Address.</param>
        /// <param name="amount">The Rotation Intensity.</param>
        /// <param name="ignoreDuplicateRequests">Whether To Ignore Duplicate Requests Or Not.</param>
        /// <returns></returns>
        public static async Task<bool> RotateToy(string url, string id, int amount, bool ignoreDuplicateRequests = false)
        {
            try
            {
                if (!await InitCommand(id, amount, ignoreDuplicateRequests, "Rotate"))
                    return false;

                var toy = Toys?.Find(o => o.Id == id);
                ConsoleHelper.Tell("Rotating Toy " + toy?.Name + " (v=" + amount + ", t=" + toy?.Id + ")");
                using var rotatePacket = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/Rotate?v=" + amount + "&t=" + toy?.Id);
                using var rotateContent = rotatePacket.Content;
                var response = await rotateContent.ReadAsStringAsync();

                return await EndCommand(id, "Rotate", amount);
            }
            catch
            {
                IsRequestPending = false;

                return false;
            }
        }

        /// <summary>
        /// A Simple "Air Contract" Method Which Will Pump The Toy {x} Amount From The Local Lovense Connect Server URL And ID Of The Toy.
        /// </summary>
        /// <param name="url">The Local Lovense Connect Server URL.</param>
        /// <param name="id">The Toy ID - Fun Fact: This Is The Device's MAC Address.</param>
        /// <param name="amount">The Pumping Intensity.</param>
        /// <param name="ignoreDuplicateRequests">Whether To Ignore Duplicate Requests Or Not.</param>
        /// <returns></returns>
        public static async Task<bool> PumpToy(string url, string id, int amount, bool ignoreDuplicateRequests = false)
        {
            try
            {
                if (!await InitCommand(id, amount, ignoreDuplicateRequests, "AirAuto"))
                    return false;

                var toy = Toys?.Find(o => o.Id == id);
                ConsoleHelper.Tell("Pumping Toy " + toy?.Name + " (v=" + amount + ", t=" + toy?.Id + ")");
                using var airAutoPacket = await Client.GetAsync(url.ToLower().Replace("/gettoys", "") + "/AirAuto?v=" + amount + "&t=" + toy?.Id);
                using var airAutoContent = airAutoPacket.Content;
                var response = await airAutoContent.ReadAsStringAsync();

                return await EndCommand(id, "AirAuto", amount);
            }
            catch
            {
                IsRequestPending = false;

                return false;
            }
        }

        private static async Task<bool> InitCommand(string id, int amount, bool ignoreDuplicateRequests, string command)
        {

            if (ignoreDuplicateRequests && CurrentLovenseAmount != null && CurrentLovenseAmount.ContainsKey((id, command)) && CurrentLovenseAmount[(id, command)] == amount)
            {
                Console.WriteLine("SKIP: Duplicate " + command + amount);
                return false;
            }

            if (IsRequestPending)
            {
                await Task.Delay(40);
                if (IsRequestPending)
                {
                    Console.WriteLine("SKIP: Pending " + command + amount);
                    return false;
                }
            }

            DelayWatch.Reset();

            if (Toys == null || Toys.Count == 0)
            {
                return false;
            }

            var toy = Toys?.Find(o => o.Id == id);

            IsRequestPending = true;

            if (!DelayWatch.IsRunning)
            {
                DelayWatch.Start();
            }

            if (toy?.Name == "Unknown")
            {
                IsRequestPending = false;
                return false; // Assume Toy Disconnected
            }

            return true;
        }

        private static async Task<bool> EndCommand(string id, string command, int amount)
        {
            if (DelayWatch.IsRunning)
            {
                DelayWatch.Stop();

                LastKnownLatency = (int)DelayWatch.Elapsed.TotalMilliseconds;
            }


            if (CurrentLovenseAmount != null)
            {
                await Task.Delay(20);
                CurrentLovenseAmount[(id, command)] = amount;
            }

            IsRequestPending = false;
            return true;
        }

        public static int RangeConv(float input, float MinPossibleInput, float MaxPossibleInput, float MinConv, float MaxConv)
        {
            return Convert.ToInt32((((input - MinPossibleInput) * (MaxConv - MinConv)) / (MaxPossibleInput - MinPossibleInput)) + MinConv);
        }
    }
}