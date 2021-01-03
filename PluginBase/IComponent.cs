using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginBase
{
    public interface IComponent
    {
        string Name { get; }
        string Description { get; }
        string Function { get; }
        string Author { get; }
        string Version { get; }
        Type Control { get; }
        int Execute();
    }
}
