using Buttplug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using VrcLovenseConnect.Helpers;

namespace VrcLovenseConnect.ToyManagers
{
    internal class ButtplugManager : IToyManager
    {
        private bool disposedValue;
        readonly ButtplugClient client;
        List<ButtplugToy> toys;
        readonly int scanTime;
        readonly uint moveSpeed;
        readonly Dictionary<(string, string), float> currentHaptics = new Dictionary<(string, string), float>();

        public IEnumerable<string> ToyNames => toys.Select(toy => toy.Toy.Name);

        public bool IsToyFound => toys.Any();

        internal ButtplugManager(int scanTime, uint moveSpeed)
        {
            // Converts to milliseconds.
            this.scanTime = scanTime;
            this.moveSpeed = moveSpeed;

            client = new ButtplugClient("MainClient");

            toys = new List<ButtplugToy>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    client.Dispose();
                
                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task FindToy()
        {
            await client.ConnectAsync(new ButtplugEmbeddedConnectorOptions());

            client.StartScanningAsync().Wait();
            Thread.Sleep(scanTime);
            client.StopScanningAsync().Wait();

            toys = client.Devices.Select(toy => new ButtplugToy(toy)).ToList();
        }

        public async Task All(string toyName, int intensivity)
        {
            var toy = toys.FirstOrDefault(t => t.Toy.Name == toyName);

            if (toy != null && !toy.AllUnsupported)
            {
                currentHaptics[(toyName, "Vibrate")] = intensivity;
                currentHaptics[(toyName, "Rotate")] = intensivity;
                currentHaptics[(toyName, "Pump")] = intensivity;

                try
                {
                    await toy.Toy.SendVibrateCmd(0.0f);
                    await toy.Toy.SendRotateCmd(0.0f, true);
                    await toy.Toy.SendLinearCmd(1, 0.0f);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.AllUnsupported = true;
                }
            }
        }

        public async Task Vibrate(string toyName, int intensivity)
        {
            var toy = toys.FirstOrDefault(t => t.Toy.Name == toyName);

            if (toy != null && !toy.VibrateUnsupported
                && (!currentHaptics.ContainsKey((toyName, "Vibrate")) || currentHaptics[(toyName, "Vibrate")] != intensivity))
            {
                currentHaptics[(toyName, "Vibrate")] = intensivity;

                try
                {
                    await toy.Toy.SendVibrateCmd(intensivity);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.VibrateUnsupported = true;
                }
            }
        }

        public async Task Rotate(string toyName, int intensivity)
        {
            var toy = toys.FirstOrDefault(t => t.Toy.Name == toyName);

            if (toy != null && !toy.RotateUnsupported
                && (!currentHaptics.ContainsKey((toyName, "Rotate")) || currentHaptics[(toyName, "Rotate")] != intensivity))
            {
                currentHaptics[(toyName, "Rotate")] = intensivity;

                try
                {
                    await toy.Toy.SendRotateCmd(intensivity, true);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.RotateUnsupported = true;
                }
            }
        }

        public async Task Pump(string toyName, int intensivity)
        {
            var toy = toys.FirstOrDefault(t => t.Toy.Name == toyName);

            if (toy != null && !toy.LinearUnsupported
                && (!currentHaptics.ContainsKey((toyName, "Pump")) || currentHaptics[(toyName, "Pump")] != intensivity))
            {
                currentHaptics[(toyName, "Pump")] = intensivity;

                try
                {
                    await toy.Toy.SendLinearCmd(moveSpeed, intensivity);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.LinearUnsupported = true;
                }
            }
        }
    }
}
