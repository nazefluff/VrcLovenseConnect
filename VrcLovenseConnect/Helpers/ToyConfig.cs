using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VrcLovenseConnect.Helpers {
    internal class ToyConfig {
        public string Name { get; set; }
        public string Protocol { get; set; }
        public string VibrateParameter { get; set; }
        public string PumpParameter { get; set; }
        public string RotateParameter { get; set; }
        public float VibrateIntensity { get; set; }
        public float PumpIntensity { get; set; }
        public float RotateIntensity { get; set; }

        [JsonIgnore]
        public string VibrateAddress => $"/avatar/parameters/{VibrateParameter}";
        [JsonIgnore]
        public string PumpAddress => $"/avatar/parameters/{PumpParameter}";
        [JsonIgnore]
        public string RotateAddress => $"/avatar/parameters/{RotateParameter}";

        public ToyConfig() {
            Name = string.Empty;
            Protocol = string.Empty;
            VibrateParameter = string.Empty;
            PumpParameter = string.Empty;
            RotateParameter = string.Empty;
            VibrateIntensity = 1;
            PumpIntensity = 1;
            RotateIntensity = 1;
        }

        public bool ControlParameters() {
            if (string.IsNullOrWhiteSpace(VibrateParameter)) {
                ConsoleHelper.PrintError("Vibration Avatar Parameter error in configuration file. Please enter a valid name.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            if (VibrateIntensity <= 0) {
                ConsoleHelper.PrintError("Vibration intensity error in configuration file. Pleaser enter a non-zero, positive value.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            if (string.IsNullOrWhiteSpace(PumpParameter)) {
                ConsoleHelper.PrintError("Pumping Avatar Parameter error in configuration file. Please enter a valid name.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            if (PumpIntensity <= 0) {
                ConsoleHelper.PrintError("Pumping intensity error in configuration file. Pleaser enter a non-zero, positive value.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            if (string.IsNullOrWhiteSpace(RotateParameter)) {
                ConsoleHelper.PrintError("Rotation Avatar Parameter error in configuration file. Please enter a valid name.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            if (RotateIntensity <= 0) {
                ConsoleHelper.PrintError("Rotation intensity error in configuration file. Pleaser enter a non-zero, positive value.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            return true;
        }
    }
}
