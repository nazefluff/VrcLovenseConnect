﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VrcLovenseConnect.Helpers;
using static LovenseConnect.LovenseConnectApi;

namespace VrcLovenseConnect.ToyManagers
{
    internal class LovenseManager : IToyManager
    {
        private bool disposedValue;
        readonly string address;
        List<LovenseToy> toys;

        public IEnumerable<string> ToyNames => toys.Select(toy => toy.Name);

        public bool IsToyFound => toys.Any();

        internal LovenseManager(string address)
        {
            this.address = address;
            toys = new List<LovenseToy>();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
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
            try
            {
                // Finds connected toys.
                toys = await GetToys(address) ?? new List<LovenseToy>();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);

                toys = new List<LovenseToy>();
            }
        }

        public async Task All(string toyName, int intensity)
        {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);

            // Scales the received value to Lovense's Vibration scale (0-20).
            // Source: https://fr.lovense.com/sextoys/developer/doc#solution-3-cam-kit-step3
            // int intensity = (int)Math.Round(haptics * 20.0f);

            // Vibrates the toy with the set intensity.
            if (toy != null && !toy.AllUnsupported)
            {
                try
                {
                    await AllToy(address, toy.Id ?? string.Empty, intensity, true);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.AllUnsupported = true;
                }
            }
        }

        public async Task Vibrate(string toyName, int intensity)
        {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);

            // Scales the received value to Lovense's Vibration scale (0-20).
            // Source: https://fr.lovense.com/sextoys/developer/doc#solution-3-cam-kit-step3
            // int intensity = (int)Math.Round(haptics * 20.0f);

            // Vibrates the toy with the set intensity.
            if (toy != null && !toy.VibrateUnsupported)
            {
                try
                {
                    await VibrateToy(address, toy.Id ?? string.Empty, intensity, true);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.VibrateUnsupported = true;
                }
            }
        }

        public async Task Rotate(string toyName, int intensity)
        {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);

            // Scales the received value to Lovense's Rotation scale (0-20).
            // Source: https://fr.lovense.com/sextoys/developer/doc#solution-3-cam-kit-step3
            // int intensity = (int)Math.Ceiling(haptics * 20.0f);

            // Vibrates the toy with the set intensity.
            if (toy != null && !toy.RotateUnsupported)
            {
                try
                {
                    await RotateToy(address, toy?.Id ?? string.Empty, intensity, true);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex);

                    // If any error happens, disables the feature for safety.
                    toy.RotateUnsupported = true;
                }
            }
        }

        public async Task Pump(string toyName, int intensity)
        {
            var toy = toys.FirstOrDefault(t => t.Name == toyName);

            // Scales the received value to Lovense's AutoAir scale (0-3).
            // Source: https://fr.lovense.com/sextoys/developer/doc#solution-3-cam-kit-step3
            // int intensity = (int)Math.Ceiling(haptics * 3.0f);

            // Vibrates the toy with the set intensity.
            if (toy != null && !toy.LinearUnsupported)
            {
                try
                {
                    await PumpToy(address, toy?.Id ?? string.Empty, intensity, true);
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
