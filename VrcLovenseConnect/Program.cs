using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using VrcLovenseConnect.Helpers;
using VrcLovenseConnect.ToyManagers;

OscModule oscModule;
Task task;

ConsoleHelper.DefaultColor = Console.ForegroundColor;
ConsoleHelper.PrintInfo("VRCLovenseConnect (beta)");

string configFile = File.ReadAllText("config.json");
Config? config = JsonConvert.DeserializeObject<Config>(configFile);

if (config == null) {
    ConsoleHelper.PrintError("Error in configuration file. Please check its format.");
    ConsoleHelper.AwaitUserKeyPress();
    return;
}

if (!config.ControlParameters())
    return;

using IToyManager lovenseManager = new LovenseManager(config.Address);

Console.WriteLine("Scanning toys through Lovense Connect...");
await lovenseManager.FindToy(config.StopDelay);

if (!lovenseManager.IsToyFound) {
    ConsoleHelper.PrintError("ERROR: Cannot find a toy. Please make sure Bluetooth is active and your toy is connected to your computer.");
    ConsoleHelper.PrintError("For detection through Lovense Connect, check the address in the config.json file and make sure your toy is connected to Lovense Connect on your phone.");
    ConsoleHelper.AwaitUserKeyPress();
    return;
}

try {
    SaveToys(lovenseManager, "Lovense");
    File.WriteAllText("config.json", JsonConvert.SerializeObject(config, Formatting.Indented));
} catch (Exception ex) {
    Console.WriteLine(ex.ToString());
    return;
}

config.Toys.RemoveAll(toy => string.IsNullOrWhiteSpace(toy.Name)
|| (toy.Protocol == "Lovense" && !lovenseManager.ToyNames.Contains(toy.Name)));

oscModule = new OscModule(config, lovenseManager);
task = Task.Run(oscModule.Listen);
ConsoleHelper.AwaitUserKeyPress();

oscModule.Play = false;
task.Wait();

void SaveToys(IToyManager toyManager, string protocol) {
    foreach (var toyName in toyManager.ToyNames) {
        ConsoleHelper.PrintSuccess($"Toy found: {toyName} ({protocol})");
        if (!config.Toys.Exists(toy => toy.Name == toyName)) {
            try {
                var toy = config.Toys.First(toy => string.IsNullOrWhiteSpace(toy.Name));
                toy.Name = toyName;
                toy.Protocol = protocol;
            } catch {
                ConsoleHelper.PrintError("No empty space to save the detected toy. Please make sure to have an empty slot by leaving a name and protocol blank.");
                ConsoleHelper.AwaitUserKeyPress();
                throw;
            }
        }
    }
}