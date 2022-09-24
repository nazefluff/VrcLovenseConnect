using Rug.Osc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VrcLovenseConnect.ToyManagers;

namespace VrcLovenseConnect.Helpers
{
    internal class OscModule
    {
        readonly Config config;
        readonly IToyManager lovenseManager;
        readonly IToyManager buttplugManager;
        readonly Dictionary<string, double> retries; // Also include command as ID?
        int nbrMessages;

        internal bool Play { get; set; } = true;

        internal float Haptics { get; set; }

        internal float ActiveHaptics { get; set; }

        internal bool IsBooleanContact { get; set; }

        internal OscModule(Config config, IToyManager lovenseManager, IToyManager buttplugManager)
        {
            this.config = config;
            this.lovenseManager = lovenseManager;
            this.buttplugManager = buttplugManager;
            retries = new Dictionary<string, double>();
        }

        /// <summary>
        /// Listens to OSC messages and updates toys.
        /// </summary>
        internal async Task Listen()
        {
            OscMessage? message;
            bool messageReceived;

            // Listens for OSC messages on localhost, port 9001.
            using var oscReceiver = new OscReceiver(IPAddress.Loopback, config.OscPort);
            oscReceiver.Connect();
            Console.WriteLine($"Connected and listening to {oscReceiver.LocalAddress}:{oscReceiver.RemoteEndPoint.Port}...");

            // Initiates dictionary
            foreach (var toy in config.Toys)
                retries[toy.Name] = 0;

            // Loops until the program is closed.
            while (Play)
            {
                try
                {
                    // Listens for one tick. Non-blocking.
#pragma warning disable S125
//#if DEBUG
//                    messageReceived = true;
//                    OscPacket packet = new OscMessage("/avatar/parameters/LovenseHaptics", 0.1f);
//#else
                    messageReceived = oscReceiver.TryReceive(out OscPacket packet);
#pragma warning restore S125
//#endif

                    // Message received, sends intensity to command the toy.
                    if (messageReceived)
                    {
                        message = packet as OscMessage;

                        // Browses all connected toys.
                        foreach (var toy in config.Toys)
                        {
                            // If an Avatar Parameter for controlling toys is received, fetches its value.
                            if (message != null && (message.Address == toy.VibrateAddress
                            || message.Address == toy.PumpAddress
                            || message.Address == toy.RotateAddress))
                            {
                                // Looks into every N message.
                                if (nbrMessages == 0)
                                {
                                    // Reads the message's value and determines its type.
                                    IsBooleanContact = message.FirstOrDefault() as bool? ?? false;
                                    Haptics = IsBooleanContact ? 0.0f : (message.FirstOrDefault() as float? ?? 0.0f);

                                    if (IsBooleanContact || Haptics > 0)
                                    {
#if DEBUG
                                        Console.WriteLine(message.ToString());
                                        Logger.LogDebugInfo($"Processing: {message}");
#endif
                                        // Resets retries for this toy.
                                        retries[toy.Name] = 0;

                                        // Stores current haptics for retries (prevents stopping the toy to fail).
                                        ActiveHaptics = Haptics > 0 ? Haptics : 1;

                                        // Controls the toy.
                                        await CommandToy(toy, message);

                                        // Message processed, the next N messages will be skipped for performance.
                                        CountMessages();
                                    }
                                    else
                                    {
#if DEBUG
                                        Logger.LogDebugInfo($"Message has a negating value: {message} (retries: {retries[toy.Name]}, active haptics: {ActiveHaptics}, is boolean contact: {IsBooleanContact})");
#endif
                                        // Message has a negating value, this counts as a no-concern.
                                        await CountRetries(toy);
                                    }
                                }
                                else
                                {
                                    // Skipping N messages for performance.
                                    CountMessages();
                                }
                            }
                            else
                            {
#if DEBUG
                                Logger.LogDebugInfo($"Message is not a toy command: {message} (retries: {retries[toy.Name]}, active haptics: {ActiveHaptics}, is boolean contact: {IsBooleanContact})");
#endif
                                // The received message doesn't concern this toy.
                                await CountRetries(toy);
                            }
                        }
                    }
                    else
                    {
                        // Waits between two listenings to reduce CPU usage.
                        Thread.Sleep(config.SleepTime);
                    }
                }
                catch (Exception ex)
                {
                    // No critical error that requires a full stop.
#if DEBUG
                    ConsoleHelper.PrintError(ex.Message);
#endif
                }
            }
        }

        internal async Task StopToy(ToyConfig toy)
        {
            var toyManager = toy.Protocol == "Lovense" ? lovenseManager : buttplugManager;

            IsBooleanContact = false;
            Haptics = 0;
            ActiveHaptics = 0;
            retries[toy.Name] = 0;

            await toyManager.Vibrate(toy.Name, 0);
            await toyManager.Pump(toy.Name, 0);
            await toyManager.Rotate(toy.Name, 0);
            
#if DEBUG
            Console.WriteLine("Toy stopped.");
#endif
        }

        private async Task CountRetries(ToyConfig toy)
        {
            // Counts the number of retries for this toy.
            retries[toy.Name]++;

            // No command received for a moment, pauses the toy if started.
            if (retries[toy.Name] >= config.RetriesLimit && ActiveHaptics > 0)
            {
#if DEBUG
                Logger.LogDebugInfo($"Stopping toy after {retries[toy.Name]} (Haptics value before stopping: {Haptics})");
#endif
                await StopToy(toy);
            }
        }

        private void CountMessages()
        {
            // Counts the number of OSC messages received since the last process.
            nbrMessages++;

            // Resets the number of messages read when limit is reached.
            if (nbrMessages > config.Limit)
                nbrMessages = 0;
        }

        private async Task CommandToy(ToyConfig toy, OscMessage message)
        {
            var toyManager = toy.Protocol == "Lovense" ? lovenseManager : buttplugManager;

            if (message.Address == toy.VibrateAddress)
            {
                await toyManager.Vibrate(toy.Name, IsBooleanContact ? toy.VibrateIntensity : Haptics);
            }
            if (message.Address == toy.PumpAddress)
            {
                await toyManager.Pump(toy.Name, IsBooleanContact ? toy.PumpIntensity : Haptics);
            }
            if (message.Address == toy.RotateAddress)
            {
                await toyManager.Rotate(toy.Name, IsBooleanContact ? toy.RotateIntensity : Haptics);
            }
        }
    }
}