using PluginBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace FileSystemHelper
{
    public partial class FileSystemHelperControl : UserControl
    {
        public FileSystemHelperControl()
        {
            InitializeComponent();
        }

        public FileSystemHelperControl(Dictionary<string, IComponent> components)
        {
            InitializeComponent();
            PluginInfo.Inlines.Clear();
            PluginHeader.Visibility = Visibility.Visible;
            foreach (var component in components)
            {
                PluginInfo.Inlines.Add(
                    new Run(component.Key + " v" + component.Value.Version + ": " + component.Value.Description));
                PluginInfo.Inlines.Add(new LineBreak());
            }
            PluginInfo.Visibility = Visibility.Visible;
        }
    }
}
