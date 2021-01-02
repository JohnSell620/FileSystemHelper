using System;
using System.IO;
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
using PluginBase;
using System.Reflection;
using System.Configuration;
using System.Collections.Specialized;

namespace FileSystemHelper
{
    public partial class MainWindow : Window
    {
        private Dictionary<string, IComponent> _components = new Dictionary<string, IComponent>();
        public MainWindow()
        {
            InitializeComponent();

            Title = "File System Helper v" + ConfigurationManager.AppSettings.Get("Version");

            this.contentControl.Content = new FileSystemHelper.FileSystemHelperControl();
            LoadComponents("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\FileSystemHelper\\bin\\Debug\\");
            AddPluginToolbarContent();
        }

        private void LoadComponents(string directory)
        {
            _components.Clear();
            foreach (var dll in Directory.GetFiles(directory, "*.dll"))
            {
                var asm = Assembly.LoadFrom(dll);
                foreach (var type in asm.GetTypes())
                {
                    if (type.GetInterface("IComponent") == typeof(IComponent))
                    {
                        var component = Activator.CreateInstance(type) as IComponent;
                        _components[component.Name] = component;
                    }
                }
                
            }
        }

        private void AddPluginToolbarContent()
        {
            foreach (KeyValuePair<string, IComponent> component in _components)
            {
                Button button = new Button();
                button.Content = component.Value.Function;
                button.Name = component.Value.Function;
                button.Click += new RoutedEventHandler(ToolbarComponent_ButtonClick);
                this.pluginsToolbar.Items.Add(button);
                Separator separator = new Separator();
                this.pluginsToolbar.Items.Add(separator);
            }
        }

        private void ToolbarComponent_ButtonClick(object sender, EventArgs e)
        {
            this.contentControl.Content = new SummarizerPlugin.SummarizerControl();
            var toolbarButton = sender as Button;
            //var component = _components[toolbarButton.Name];

            try
            {
                this.Cursor = Cursors.Wait;
                //this.contentControl.Content = component.Execute();
            }
            catch (Exception exception)
            {
                // Handle bug in plugin.
                Console.WriteLine(exception.ToString());
            }
            finally
            {
                this.Cursor = Cursors.AppStarting;
            }
        }
    }
}
