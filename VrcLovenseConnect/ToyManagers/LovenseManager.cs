using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VrcLovenseConnect.Helpers;
using static LovenseConnect.LovenseConnectApi;

namespace VrcLovenseConnect.ToyManagers {
    internal class LovenseManager : IToyManager {
        private bool disposedValue;
        readonly string address;
        List<LovenseToy> toys;

        public IEnumerable<string> ToyNames => toys.Select(toy => toy.Name);

        public bool IsToyFound => toys.Any();

        internal LovenseManager(string address) {
            this.address = address.ToLower();
            toys = new List<LovenseToy>();
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue)
                disposedValue = true;
        }

        public void Dispose() {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task FindToy(int stopDelay = 2) {
            try {
                toys = await GetToys(address) ?? new List<LovenseToy>();
            } catch (Exception ex) {
                Console.WriteLine(ex.ToString());
                toys = new List<LovenseToy>();
            }
        }

        public async Task Vibrate(string toyName, int intensity) {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);
            if (toy != null && !toy.VibrateUnsupported) {
                try {
                    await VibrateToy(address, toy, intensity, true);
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    toy.VibrateUnsupported = true;
                }
            }
        }

        public async Task Rotate(string toyName, int intensity) {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);
            if (toy != null && !toy.RotateUnsupported) {
                try {
                    await RotateToy(address, toy, intensity, true);
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    toy.RotateUnsupported = true;
                }
            }
        }

        public async Task Pump(string toyName, int intensity) {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);
            if (toy != null && !toy.LinearUnsupported) {
                try {
                    await PumpToy(address, toy, intensity, true);
                } catch (Exception ex) {
                    Console.WriteLine(ex.ToString());
                    toy.LinearUnsupported = true;
                }
            }
        }
    }
}
