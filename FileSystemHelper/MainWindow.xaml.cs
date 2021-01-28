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
        private Dictionary<string, Type> _componentControls = new Dictionary<string, Type>();
        private string s_activePluginName;

        public MainWindow()
        {
            InitializeComponent();
            Title = "File System Helper v" + ConfigurationManager.AppSettings.Get("Version");
            contentControl.Content = new FileSystemHelper.FileSystemHelperControl();
            LoadComponents(".");
            //LoadComponents("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\FileSystemHelper\\bin\\Debug\\");
            AddPluginToolbarContent();
            s_activePluginName = "";
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
                        _componentControls[component.Name] = component.Control;
                    }
                }                
            }
        }

        private void AddPluginToolbarContent()
        {
            Dictionary<int, string> styleMap = new Dictionary<int, string>
            {
                { 0, "MaterialDesignRaisedLightButton" },
                { 1, "MaterialDesignRaisedAccentButton" }
            };
            
            int it = 0;
            foreach (KeyValuePair<string, IComponent> component in _components)
            {
                Button button = new Button
                {
                    Content = new Viewbox
                    {
                        Child = new TextBlock
                        {
                            Text = component.Value.Function,
                            FontSize = 20.0,
                            FontFamily = new FontFamily("Comic Sans MS"), // Geneva
                            TextWrapping = TextWrapping.Wrap,
                            TextAlignment = TextAlignment.Center,
                            FontWeight = FontWeights.UltraBold,
                            FontStretch = FontStretches.UltraExpanded,
                            ToolTip = component.Value.Description,
                            Margin = new Thickness(4)
                        },
                        Stretch = Stretch.Uniform
                    },
                    ToolTip = component.Value.Description,
                    Name = component.Key,
                    Style = (Style)Application.Current.Resources[styleMap[it++ % styleMap.Count]],
                    Height = 30,
                    Background = new SolidColorBrush(Colors.GhostWhite),
                    Padding = new Thickness(4),
                    Margin = new Thickness(16)
                };
                MaterialDesignThemes.Wpf.ButtonAssist.SetCornerRadius(button, new CornerRadius(15));

                button.Click += new RoutedEventHandler(PanelComponent_ButtonClick);
                PluginsPanel.Children.Add(button);
            }
        }
        
        private void PanelComponent_ButtonClick(object sender, EventArgs e)
        {
            Button panelButton = sender as Button;
            var clickedComponent = _components[panelButton.Name];
            string clickedComponentName = clickedComponent.Name;

            if (s_activePluginName != clickedComponentName)
            {
                foreach (UIElement uie in PluginsPanel.Children.OfType<UIElement>().ToList())
                {
                    string pluginName = uie.GetValue(NameProperty).ToString();
                    if (pluginName == clickedComponentName)
                    {
                        try
                        {
                            Cursor = Cursors.Wait;
                            contentControl.Content = Activator.CreateInstance(clickedComponent.Control) as UserControl;
                            panelButton.Height = panelButton.Height * 1.1;
                            panelButton.Width = panelButton.Width * 1.1;
                            Cursor = Cursors.AppStarting;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.ToString());
                        }
                        finally
                        {
                            Cursor = Cursors.Arrow;
                        }
                    }
                    else if (pluginName == s_activePluginName)
                    {
                        int buttonHeight = int.Parse(uie.GetValue(HeightProperty).ToString());
                        int buttonWidth = int.Parse(uie.GetValue(WidthProperty).ToString());
                        uie.SetValue(HeightProperty, buttonHeight / 1.1);
                        uie.SetValue(WidthProperty, buttonWidth / 1.1);
                    }
                }
                s_activePluginName = clickedComponentName;
            }
            else
            {
                panelButton.Height = panelButton.Height / 1.1;
                panelButton.Width = panelButton.Width / 1.1;
                contentControl.Content = new FileSystemHelperControl();
                s_activePluginName = "";
            }
        }
    }
}
