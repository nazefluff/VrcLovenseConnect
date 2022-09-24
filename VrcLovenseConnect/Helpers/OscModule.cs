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
        readonly Dictionary<string, int> intensivity;
        readonly Dictionary<string, int> running;
        int nbrMessages;

        internal bool Play { get; set; } = true;

        internal int Haptics { get; set; }

        internal float ActiveHaptics { get; set; }

        internal int RunningToys { get; set; }

        internal bool IsBooleanContact { get; set; }

        internal OscModule(Config config, IToyManager lovenseManager, IToyManager buttplugManager)
        {
            this.config = config;
            this.lovenseManager = lovenseManager;
            this.buttplugManager = buttplugManager;
            retries = new Dictionary<string, double>();
            intensivity = new Dictionary<string, int>();
            running = new Dictionary<string, int>();
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
            {
                retries[toy.Name] = 0;
                intensivity[toy.Name] = 0;
                running[toy.Name] = 0;
            }

            // Loops until the program is closed.
            while (Play)
            {
                try
                {
                    // Listens for one tick. Non-blocking.
                    messageReceived = oscReceiver.TryReceive(out OscPacket packet);

                    // Message received, sends intensity to command the toy.
                    if (messageReceived)
                    {
                        message = packet as OscMessage;
                        // Browses all connected toys.
                        foreach (var toy in config.Toys)
                        {
                            // If an Avatar Parameter for controlling toys is received, fetches its value.
                            if (message != null && (message.Address == toy.VibrateAddress || message.Address == toy.PumpAddress || message.Address == toy.RotateAddress))
                            {
                                running[toy.Name] = 2;

                                // Reads the message's value and determines its type.
                                IsBooleanContact = message.FirstOrDefault() as bool? ?? false;
                                var inte = (int)(Math.Round(IsBooleanContact ? 0.0f : (message.FirstOrDefault() as float? ?? 0.0f) * 10.0f));
                                
                                if (message.Address == toy.VibrateAddress)
                                {
                                    inte = inte * 2;
                                }
                                if (message.Address == toy.PumpAddress)
                                {
                                    inte = (int)Math.Round((decimal)(Haptics / 3));
                                }
                                if (message.Address == toy.RotateAddress)
                                {
                                    inte = inte * 2;
                                }

                                Haptics = inte;

                                if (intensivity[toy.Name] != inte || IsBooleanContact)
                                {

                                    
#if DEBUG
                                    Console.WriteLine($"Processing: {message}   - " + Haptics+ "");
                                    Logger.LogDebugInfo($"Processing: {message}");
#endif
                                    intensivity[toy.Name] = inte;

                                    // Stores current haptics for retries (prevents stopping the toy to fail).
                                    ActiveHaptics = Haptics > 0 ? Haptics : 1;

                                    // Resets retries for this toy.
                                    retries[toy.Name] = 0;

                                    // Controls the toy.
                                    CommandToy(toy, message.Address, inte);

                                }

                                running[toy.Name] = 1;

                            }
                            else
                            {
#if DEBUG
                                //  Console.WriteLine($"Message is not a toy command: {message} (retries: {retries[toy.Name]}, active haptics: {ActiveHaptics}, is boolean contact: {IsBooleanContact})");
                                Logger.LogDebugInfo($"Message is not a toy command: {message} (retries: {retries[toy.Name]}, active haptics: {ActiveHaptics}, is boolean contact: {IsBooleanContact})");
#endif

                                retries[toy.Name]++;
                                // The received message doesn't concern this toy.
                                await CountRetries(toy);

                            }

                            
                        }
                    }
                    else
                    {
                        // Waits between two listenings to reduce CPU usage.
                        Thread.Sleep(50);
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
            running[toy.Name] = 2;

            IsBooleanContact = false;
            Haptics = 0;
            ActiveHaptics = 0;
            retries[toy.Name] = 0;
            intensivity[toy.Name] = 0;

#if DEBUG
            Console.WriteLine("Stopping Toy " + toy.Name);
#endif

            CommandToy(toy, "all", 0);

            running[toy.Name] = 1;

        }

        private async Task CountRetries(ToyConfig toy)
        {
            // Counts the number of retries for this toy.
            // retries[toy.Name]++;

            // No command received for a moment, pauses the toy if started.
            if (retries[toy.Name] > 100 && running[toy.Name] == 0 && intensivity[toy.Name] > 0)
            {
                retries[toy.Name] = 0;

                Console.WriteLine("CHeck " + running[toy.Name] + " " + intensivity[toy.Name]);
#if DEBUG
                Logger.LogDebugInfo($"Stopping toy after {retries[toy.Name]} (Haptics value before stopping: {Haptics})");
#endif
                await StopToy(toy);
            }
        }

        private async Task CommandToy(ToyConfig toy, String address, int inte)
        {
            var toyManager = toy.Protocol == "Lovense" ? lovenseManager : buttplugManager;
            if(running[toy.Name] != 0 && address != "all")
            {
                if (address == toy.VibrateAddress)
                {
                    await toyManager.Vibrate(toy.Name, IsBooleanContact ? 1 : inte);
                }
                else if (address == toy.PumpAddress)
                {
                    await toyManager.Pump(toy.Name, IsBooleanContact ? 1 : inte);
                }
                else if (address == toy.RotateAddress)
                {
                    await toyManager.Rotate(toy.Name, IsBooleanContact ? 1 : inte);
                }

                if (running[toy.Name] != 2)
                    running[toy.Name] = 0;
            }
            else if (address == "all")
            {
                await toyManager.All(toy.Name, IsBooleanContact ? 1 : inte);
                running[toy.Name] = 0;
            }
        }
    }
}