

namespace NotifyIcon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IIconFile : IDisposable
{
    nint Handle { get; }
}
