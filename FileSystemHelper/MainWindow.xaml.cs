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
        private string s_activePluginName;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon("Resources/FSH.ico");
            ni.Visible = true;
            ni.DoubleClick +=
                delegate (object? sender, EventArgs args)
                {
                    Show();
                    WindowState = WindowState.Normal;
                };

            Title = "File System Helper v" + ConfigurationManager.AppSettings.Get("Version");
            LoadComponents(".");
            AddPluginToolbarContent();
            contentControl.Content = new FileSystemHelperControl(_components);
            s_activePluginName = "";
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
                this.Hide();

            base.OnStateChanged(e);
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
                        if (component != null)
                        {
                            _components[component.Name] = component;
                            _componentControls[component.Name] = component.Control;
                        }
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
                    Height = 30,
                    Width = 70,
                    Style = (Style)Application.Current.Resources[styleMap[it++ % styleMap.Count]],
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
            if (sender != null)
            {
                Button? panelButton = sender as Button;
                var clickedComponent = panelButton != null ? _components[panelButton.Name] : null;
                string? clickedComponentName = clickedComponent != null ? clickedComponent.Name : null;

                if (s_activePluginName != clickedComponentName)
                {
                    foreach (UIElement uie in PluginsPanel.Children.OfType<UIElement>().ToList())
                    {
                        string? pluginName = uie.GetValue(NameProperty).ToString();
                        if (pluginName == clickedComponentName)
                        {
                            try
                            {
                                Cursor = Cursors.Wait;
                                if (clickedComponent != null)
                                    contentControl.Content = Activator.CreateInstance(clickedComponent.Control) as UserControl;
                                if (panelButton != null)
                                {
                                    panelButton.Height = panelButton.Height * 1.1;
                                    panelButton.Width = panelButton.Width * 1.1;
                                }
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
                            if (uie != null)
                            {
                                string? buttonHeight = uie.GetValue(HeightProperty).ToString();
                                string? buttonWidth = uie.GetValue(WidthProperty).ToString();
                                if (buttonHeight != null && buttonWidth != null)
                                {
                                    int buttonHeightInt = int.Parse(buttonHeight.ToString());
                                    int buttonWidthInt = int.Parse(buttonWidth.ToString());
                                    uie.SetValue(HeightProperty, buttonHeightInt / 1.1);
                                    uie.SetValue(WidthProperty, buttonWidthInt / 1.1);
                                }
                            }
                        }
                    }
                    if (clickedComponentName != null)
                        s_activePluginName = clickedComponentName;
                }
                else
                {
                    if (panelButton != null)
                    {
                        panelButton.Height = panelButton.Height / 1.1;
                        panelButton.Width = panelButton.Width / 1.1;
                    }
                    contentControl.Content = new FileSystemHelperControl(_components);
                    s_activePluginName = "";
                }
            }
        }

        private void MinimizeToSystemTray(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {

        }
    }
}
