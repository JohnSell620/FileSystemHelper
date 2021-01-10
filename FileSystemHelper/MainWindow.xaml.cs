﻿using System;
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
        private string activePluginName;

        public MainWindow()
        {
            InitializeComponent();
            Title = "File System Helper v" + ConfigurationManager.AppSettings.Get("Version");
            contentControl.Content = new FileSystemHelper.FileSystemHelperControl();
            LoadComponents("C:\\Users\\jsell\\source\\repos\\FileSystemHelper\\FileSystemHelper\\bin\\Debug\\");
            AddPluginToolbarContent();
            activePluginName = "";
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
                { 0, "MaterialDesignFloatingActionLightButton" },
                { 1, "MaterialDesignFloatingActionDarkButton" }
            };
            int it = 0;
            foreach (KeyValuePair<string, IComponent> component in _components)
            {
                Button button = new Button();
                //ControlTemplate templateButton = new ControlTemplate(typeof(Button));
                //DataTemplate dt = new DataTemplate();
                //FrameworkElementFactory fef = new FrameworkElementFactory(typeof(Viewbox));
                //templateButton.VisualTree = fef;
                //fef.AppendChild(new FrameworkElementFactory(typeof(Viewbox)));
                //dt.VisualTree = fef;


                Viewbox vb = new Viewbox();
                TextBlock tb = new TextBlock();
                tb.Text = component.Value.Function;
                tb.FontSize = 16;
                tb.TextAlignment = TextAlignment.Center;
                tb.FontWeight = FontWeights.UltraBold;
                tb.FontStretch = FontStretches.UltraExpanded;
                vb.Child = tb;
                vb.Stretch = Stretch.Uniform;


                //button.Content = tb;
                button.Content = vb;
                button.Name = component.Key;
                //dt.Resources.Add(button, vb);
                //button.Content = component.Key;
                //button.Name = component.Value.Function;
                button.Style = (Style)Application.Current.Resources[styleMap[it++ % styleMap.Count]];
                button.Padding = new Thickness(16);
                button.Margin = new Thickness(16);
                button.Click += new RoutedEventHandler(PanelComponent_ButtonClick);
                PluginsPanel.Children.Add(button);
            }
        }

        private void PanelComponent_ButtonClick(object sender, EventArgs e)
        {
            Button panelButton = sender as Button;
            var clickedComponent = _components[panelButton.Name];
            string clickedComponentName = clickedComponent.Name;
            //Console.WriteLine("ButtonComponentName: " + clickedPluginName);

            if (activePluginName != clickedComponentName)
            {

                foreach (UIElement uie in PluginsPanel.Children.OfType<UIElement>().ToList())
                {
                    string pluginName = uie.GetValue(NameProperty).ToString();
                    //Type controlType = _componentControls[clickedComponent.Name];

                    if (pluginName == clickedComponentName)
                    {
                        //Console.WriteLine(pluginName + "Plugin");
                        //Assembly asm = Assembly.Load(pluginName + "Plugin");
                        //Type ct = asm.GetType(controlName);
                        //contentControl.Content = Activator.CreateInstance(ct) as UserControl;
                        //contentControl.Content = new _componentControls[clickedComponent.Name];

                        contentControl.Content = Activator.CreateInstance(clickedComponent.Control) as UserControl;

                        //contentControl.Content = new SummarizerPlugin.SummarizerControl();
                        panelButton.Height = panelButton.Height * 1.1;
                        panelButton.Width = panelButton.Width * 1.1;
                    }
                    else if (pluginName == activePluginName)
                    {
                        int buttonHeight = Int32.Parse(uie.GetValue(HeightProperty).ToString());
                        int buttonWidth = Int32.Parse(uie.GetValue(WidthProperty).ToString());
                        uie.SetValue(HeightProperty, buttonHeight / 1.1);
                        uie.SetValue(WidthProperty, buttonWidth / 1.1);
                    }
                }
                activePluginName = clickedComponentName;
                
                //try
                //{
                //    this.Cursor = Cursors.Wait;
                //}
                //catch (Exception exception)
                //{
                //    // Handle bug(s) in plugin.
                //    Console.WriteLine(exception.ToString());
                //}
                //finally
                //{
                //    this.Cursor = Cursors.AppStarting;
                //}

            }
            else
            {
                panelButton.Height = panelButton.Height / 1.1;
                panelButton.Width = panelButton.Width / 1.1;
                contentControl.Content = new FileSystemHelper.FileSystemHelperControl();
                activePluginName = "";
            }
        }
    }
}
