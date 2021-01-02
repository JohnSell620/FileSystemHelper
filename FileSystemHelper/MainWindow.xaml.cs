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
            Dictionary<int, string> styleMap = new Dictionary<int, string>
            {
                { 0, "MaterialDesignFloatingActionLightButton" },
                { 1, "MaterialDesignFloatingActionDarkButton" }
            };
            int it = 0;
            foreach (KeyValuePair<string, IComponent> component in _components)
            {
                Button button = new Button();
                button.Content = component.Key;
                button.Name = component.Value.Function;
                button.Style = (Style)Application.Current.Resources[styleMap[it++ % styleMap.Count]];
                button.Padding = new Thickness(16);
                button.Margin = new Thickness(16);
                button.Click += new RoutedEventHandler(PanelComponent_ButtonClick);
                PluginsPanel.Children.Add(button);
            }
        }

        private void PanelComponent_ButtonClick(object sender, EventArgs e)
        {
            this.contentControl.Content = new SummarizerPlugin.SummarizerControl();
            var panelButton = sender as Button;
            //panelButton.RenderTransform.SetCurrentValue(ScaleTransform, 2);
            //var component = _components[toolbarButton.Name];

            try
            {
                this.Cursor = Cursors.Wait;
            }
            catch (Exception exception)
            {
                // Handle bug(s) in plugin.
                Console.WriteLine(exception.ToString());
            }
            finally
            {
                this.Cursor = Cursors.AppStarting;
            }
        }
    }
}
