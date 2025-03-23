namespace NotifyIcon;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

abstract class NotifyContextMenuItem
{
    public abstract string Text { get; set; }

    public abstract bool IsEnabled { get; set; }

    public EventHandler<EventArgs>? Click;
}
