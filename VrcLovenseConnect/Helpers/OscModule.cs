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
        readonly Dictionary<string, int> intensityVib;
        readonly Dictionary<string, int> intensityPum;
        readonly Dictionary<string, int> intensityRot;
        internal bool Play { get; set; } = true;
        internal int Msgs { get; set; } = 0;
        internal OscModule(Config config, IToyManager lovenseManager) {
            this.config = config;
            this.lovenseManager = lovenseManager;
            intensityVib = new Dictionary<string, int>();
            intensityPum = new Dictionary<string, int>();
            intensityRot = new Dictionary<string, int>();
        }

        internal async Task Listen() {
            OscMessage? message;
            using var oscReceiver = new OscReceiver(IPAddress.Loopback, config.OscPort);
            oscReceiver.Connect();
            Console.WriteLine($"Connected and listening to {oscReceiver.LocalAddress}:{oscReceiver.RemoteEndPoint.Port}...");
            foreach (var toyName in config.Toys.Select(x => x.Name)) {
                intensityVib[toyName] = 0;
                intensityPum[toyName] = 0;
                intensityRot[toyName] = 0;
            }

            while (Play) {
                try {
                    if (oscReceiver.TryReceive(out OscPacket packet)) {
                        message = packet as OscMessage;
                        if (Msgs >= 6 && message != null) {
                            foreach (var toy in config.Toys) {
                                if (message.Address == toy.VibrateAddress) {
                                    var intens = (int)(Math.Round((message.FirstOrDefault() as float? ?? 0.0f) * 20.0f));
                                    if (intensityVib[toy.Name] != intens) {
                                        intensityVib[toy.Name] = intens;
                                        await lovenseManager.Vibrate(toy.Name, intens);
                                    }
                                }

                                if (message.Address == toy.PumpAddress) {
                                    var intens = (int)(Math.Ceiling((message.FirstOrDefault() as float? ?? 0.0f) * 3.0f));
                                    if (intensityPum[toy.Name] != intens) {
                                        intensityPum[toy.Name] = intens;
                                        await lovenseManager.Pump(toy.Name, intens);
                                    }
                                }

                                if (message.Address == toy.RotateAddress) {
                                    var intens = (int)(Math.Round((message.FirstOrDefault() as float? ?? 0.0f) * 20.0f));
                                    if (intensityRot[toy.Name] != intens) {
                                        intensityRot[toy.Name] = intens;
                                        await lovenseManager.Rotate(toy.Name, intens);
                                    }
                                }
                            }
                            Msgs = 0;
                        }
                        Msgs++;
                    } else {
                        Thread.Sleep(100);
                    }
                } catch (Exception ex) {
                    ConsoleHelper.PrintError(ex.Message);
                }
            }
        }
    }
}