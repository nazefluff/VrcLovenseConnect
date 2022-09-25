using Rug.Osc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using VrcLovenseConnect.ToyManagers;

namespace VrcLovenseConnect.Helpers {
    internal class OscModule {
        readonly Config config;
        readonly IToyManager lovenseManager;
        readonly Dictionary<string, int> intensity;
        internal bool Play { get; set; } = true;
        internal OscModule(Config config, IToyManager lovenseManager) {
            this.config = config;
            this.lovenseManager = lovenseManager;
            intensity = new Dictionary<string, int>();
        }

        internal async Task Listen() {
            OscMessage? message;
            using var oscReceiver = new OscReceiver(IPAddress.Loopback, config.OscPort);
            oscReceiver.Connect();
            Console.WriteLine($"Connected and listening to {oscReceiver.LocalAddress}:{oscReceiver.RemoteEndPoint.Port}...");

            foreach (var toyName in config.Toys.Select(x => x.Name))
                intensity[toyName] = 0;

            while (Play) {
                try {
                    if (oscReceiver.TryReceive(out OscPacket packet)) {
                        message = packet as OscMessage;
                        foreach (var toy in config.Toys) {
                            if (message != null && (message.Address == toy.VibrateAddress || message.Address == toy.PumpAddress || message.Address == toy.RotateAddress)) {
                                var intens = (int)(Math.Round((message.FirstOrDefault() as float? ?? 0.0f) * 10.0f));
                                if (intensity[toy.Name] != intens) {
                                    intensity[toy.Name] = intens;
                                    await CommandToy(toy, message.Address, intens);
                                }
                            }
                        }
                    } else {
                        Thread.Sleep(50);
                    }
                } catch (Exception ex) {
                    ConsoleHelper.PrintError(ex.Message);
                }
            }
        }

        private async Task CommandToy(ToyConfig toy, String address, int intens) {
            if (address == toy.VibrateAddress)
                await lovenseManager.Vibrate(toy.Name, intens * 2);
            if (address == toy.PumpAddress)
                await lovenseManager.Pump(toy.Name, (int)Math.Round((decimal)(intens / 3)));
            if (address == toy.RotateAddress)
                await lovenseManager.Rotate(toy.Name, intens * 2);
        }
    }
}