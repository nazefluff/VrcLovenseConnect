using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace VrcLovenseConnect.ToyManagers
{
    internal interface IToyManager : IDisposable
    {
        IEnumerable<string> ToyNames { get; }

        bool IsToyFound { get; }

        Task FindToy();

        Task All(string toyName, int intensity);

        Task Vibrate(string toyName, int intensity);

        Task Rotate(string toyName, int intensity);

        Task Pump(string toyName, int intensity);
    }
}
