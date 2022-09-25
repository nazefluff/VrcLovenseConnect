using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VrcLovenseConnect.Helpers {
    internal class Config {
        public int OscPort { get; set; }

        public string Address { get; set; }

        public int StopDelay { get; set; }

        public List<ToyConfig> Toys { get; set; }

        public Config() {
            Address = string.Empty;
            Toys = new List<ToyConfig>();
        }

        public bool ControlParameters() {
            if (OscPort <= 0) {
                ConsoleHelper.PrintError("Port error in configuration file. Pleaser enter a valid port number.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            if (string.IsNullOrWhiteSpace(Address)) {
                ConsoleHelper.PrintError("Address error in configuration file. Please enter the address provided by the Lovense Connect app on your phone or enter any valid address if unused.");
                ConsoleHelper.AwaitUserKeyPress();
                return false;
            }

            foreach (var toy in Toys)
                if (!toy.ControlParameters())
                    return false;

            return true;
        }
    }
}
