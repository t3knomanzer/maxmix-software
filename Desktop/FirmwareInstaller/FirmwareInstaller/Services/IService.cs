using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FirmwareInstaller.Services
{
    internal interface IService
    {
        void Start();
        void Stop();

        event EventHandler<string> Error;
    }
}
