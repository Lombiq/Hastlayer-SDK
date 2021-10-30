using Hast.Samples.Consumer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Terminal.Gui;

namespace Hast.Samples.Consumer
{
    public class Gui
    {
        public Dictionary<string, ConsumerConfiguration> SavedConfigurations { get; }

        public Gui(Dictionary<string, ConsumerConfiguration> savedConfigurations) =>
            SavedConfigurations = savedConfigurations;

        public ConsumerConfiguration BuildConfiguration()
        {
            var configuration = new ConsumerConfiguration();

            Application.UseSystemConsole = true;

            Application.Init();
            Application.HeightAsBuffer = true;

            Action SetConfigurationAndStop(Func<ConsumerConfiguration> set)
            {
                return () =>
                {
                    configuration = set?.Invoke();
                    Application.RequestStop();
                };
            }

            var menu = new MenuBar (new[] {
                new MenuBarItem ("_File", new[] {
                    new MenuItem (
                        "_Load",
                        "Selects a saved configuration.",
                        SetConfigurationAndStop(LoadConfiguration)),
                    new MenuItem (
                        "_Save",
                        "Saves the current configuration.",
                        () => SaveConfiguration(configuration)),
                    new MenuItem (
                        "_Quit (Ctrl + Q)",
                        "Closes the application.",
                        SetConfigurationAndStop(null),
                        shortcut: Key.Q | Key.CtrlMask),
                }),
            });

            var confiurationDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(configuration));
            var sidebarWidth = confiurationDictionary.Select(pair => pair.Key.Length).Max();

            var leftPane = new FrameView("Properties") { CanFocus = false }.WithShortcut(Key.CtrlMask | Key.C);
            var rightPane = new FrameView("Options") { CanFocus = true }.WithShortcut(Key.CtrlMask | Key.S);

            var propertiesListView = new ListView (confiurationDictionary.Keys.ToList()) { CanFocus = true }.Fill();
            propertiesListView.OpenSelectedItem += _ => { rightPane.SetFocus (); };
            propertiesListView.SelectedItemChanged += PropertiesListView_SelectedChanged;
            leftPane.Add (propertiesListView);

            var top = Application.Top;
            top.Add(menu);
            top.Add(leftPane);
            top.Add(rightPane);
            top.TileHorizontally(leftPane, rightPane, sidebarWidth, (1, 0, 0, 0));

            Application.Run (top);

            Application.Shutdown();
            return configuration;
        }

        private void PropertiesListView_SelectedChanged(ListViewItemEventArgs obj)
        {
            throw new NotImplementedException();
        }

        private void SaveConfiguration(ConsumerConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        private ConsumerConfiguration LoadConfiguration()
        {
            throw new System.NotImplementedException();
        }
    }
}
