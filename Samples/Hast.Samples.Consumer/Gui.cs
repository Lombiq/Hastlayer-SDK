using Hast.Layer;
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
        private readonly ListView _propertiesListView = new ListView { CanFocus = true }.Fill();

        private readonly TextField _optionsTextField =
            new()
            {
                X = 1,
                Y = Pos.Center(),
                Width = Dim.Fill(),
                Height = 1,
                CanFocus = true,
                Visible = false,
            };
        private Action<string> _currentOptionsTextFieldEventHandler;

        private readonly ListView _optionsListView = new ListView { CanFocus = true, Visible = false }.Fill();
        private Action<object> _currentOptionsListViewEventHandler;

        private ConsumerConfiguration _configuration;

        private Task<List<string>> _deviceNamesTask;

        public Dictionary<string, ConsumerConfiguration> SavedConfigurations { get; }

        public Gui(Dictionary<string, ConsumerConfiguration> savedConfigurations) =>
            SavedConfigurations = savedConfigurations;

        public ConsumerConfiguration BuildConfiguration()
        {
            _configuration = new ConsumerConfiguration();

            _deviceNamesTask = Task.Run(() =>
                Hastlayer
                    .Create(new HastlayerConfiguration())
                    .GetSupportedDevices()?
                    .Select(device => device.Name)
                    .ToList() ?? new List<string>());

            Application.UseSystemConsole = true;

            Application.Init();
            Application.HeightAsBuffer = true;

            var menu = new MenuBar (new[] {
                new MenuBarItem ("_File", new[] {
                    new MenuItem (
                        "_Load",
                        "Selects a saved configuration.",
                        SetConfigurationAndStop(LoadConfiguration),
                        shortcut: Key.Q | Key.L),
                    new MenuItem (
                        "_Save",
                        "Saves the current configuration.",
                        () => SaveConfiguration(_configuration),
                        shortcut: Key.Q | Key.S),
                    new MenuItem (
                        "_Quit (Ctrl + Q)",
                        "Closes the application.",
                        SetConfigurationAndStop(null),
                        shortcut: Key.Q | Key.CtrlMask),
                }),
                new MenuBarItem (
                    "_Start",
                    string.Empty,
                    () => Application.RequestStop()),
            });

            var leftPane = new FrameView("Properties");
            var rightPane = new FrameView("Options");

            var confiurationDictionary =
                JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(_configuration));
            var sidebarWidth = Math.Max(
                confiurationDictionary.Select(pair => pair.Key.Length).Max() + 2,
                leftPane.Title.Length + 5
            );

            _propertiesListView.SetSource(confiurationDictionary.Keys.OrderBy(key => key).ToList());
            _propertiesListView.SelectedItemChanged += PropertiesListView_SelectedChanged;
            _propertiesListView.OpenSelectedItem += _ =>
            {
                if (_optionsListView.Visible) _optionsListView.SetFocus();
                if (_optionsTextField.Visible) _optionsTextField.SetFocus();
            };

            var top = Application.Top;
            top.Add(menu);
            top.Add(leftPane);
            top.Add(rightPane);
            top.TileHorizontally(leftPane, rightPane, sidebarWidth, (1, 0, 0, 0));
            leftPane.Add(_propertiesListView);
            rightPane.Add(_optionsTextField);
            rightPane.Add(_optionsListView);

            _optionsTextField.KeyPress += CreateKeyboardEventHandler(
                _optionsListView,
                () => _currentOptionsTextFieldEventHandler?.Invoke(_optionsTextField.Text.ToString()));

            _optionsListView.KeyPress += CreateKeyboardEventHandler(
                _optionsListView,
                () =>
                {
                    var item = _optionsListView.Source.ToList()[_optionsListView.SelectedItem];
                    _currentOptionsListViewEventHandler?.Invoke(item);
                });

            Application.Run (top);

            Application.Shutdown();
            var result = _configuration;
            _configuration = null;
            return result;
        }

        private void PropertiesListView_SelectedChanged(ListViewItemEventArgs obj)
        {
            switch (obj.Value?.ToString())
            {
                case nameof(ConsumerConfiguration.AppName):
                    _optionsTextField.Text = _configuration.AppName ?? string.Empty;
                    _currentOptionsTextFieldEventHandler = text => { _configuration.AppName = text; };
                    ShowTextField(true);
                    break;
                case nameof(ConsumerConfiguration.AppSecret):
                    _optionsTextField.Text = _configuration.AppSecret ?? string.Empty;
                    _currentOptionsTextFieldEventHandler = text => { _configuration.AppSecret = text; };
                    ShowTextField(true);
                    break;
                case nameof(ConsumerConfiguration.BuildLabel):
                    _optionsTextField.Text = _configuration.BuildLabel ?? string.Empty;
                    _currentOptionsTextFieldEventHandler = text => { _configuration.BuildLabel = text; };
                    ShowTextField(true);
                    break;
                case nameof(ConsumerConfiguration.DeviceName):
                    ShowDeviceNames();
                    break;
                case nameof(ConsumerConfiguration.DontRun):
                    _optionsListView.Source = new ListWrapper(new object[] { true, false });
                    _optionsListView.SelectedItem = _configuration.DontRun ? 0 : 1;
                    _currentOptionsListViewEventHandler = item => { _configuration.DontRun = item.IsTrueString(); };
                    ShowTextField(false);
                    break;
                case nameof(ConsumerConfiguration.Endpoint):
                    _optionsTextField.Text = _configuration.Endpoint ?? string.Empty;
                    _currentOptionsTextFieldEventHandler = text => { _configuration.Endpoint = text; };
                    ShowTextField(true);
                    break;
                case nameof(ConsumerConfiguration.HardwareFrameworkPath):
                    _optionsTextField.Text = _configuration.HardwareFrameworkPath ?? string.Empty;
                    _currentOptionsTextFieldEventHandler = text => { _configuration.HardwareFrameworkPath = text; };
                    ShowTextField(true);
                    break;
                case nameof(ConsumerConfiguration.SampleToRun):
                    var sampleNames = Enum.GetNames(typeof(Sample)).ToList();
                    _optionsListView.Source = new ListWrapper(sampleNames);
                    _optionsListView.SelectedItem = sampleNames.IndexOf(_configuration.SampleToRun.ToString());
                    _currentOptionsListViewEventHandler = item => { _configuration.SampleToRun = Enum.Parse<Sample>(item.ToString()!); };
                    ShowTextField(false);
                    break;
                case nameof(ConsumerConfiguration.VerifyResults):
                    _optionsListView.Source = new ListWrapper(new object[] { true, false });
                    _optionsListView.SelectedItem = _configuration.VerifyResults ? 0 : 1;
                    _currentOptionsListViewEventHandler = item => { _configuration.VerifyResults = item.IsTrueString(); };
                    ShowTextField(false);
                    break;
                default:
                    throw new InvalidOperationException($"Unknown menu item selected ({obj.Value}).");
            }
        }

        private void ShowDeviceNames()
        {
            var deviceNames = _deviceNamesTask.Result;
            _optionsListView.Source = new ListWrapper(deviceNames);
            if (_configuration.DeviceName is { } deviceName)
            {
                var index = deviceNames.IndexOf(deviceName);
                if (index >= 0) _optionsListView.SelectedItem = index;
            }
            _currentOptionsListViewEventHandler = item => { _configuration.DeviceName = item.ToString(); };
            ShowTextField(false);
        }

        private void SaveConfiguration(ConsumerConfiguration configuration)
        {
            throw new NotImplementedException();
        }

        private ConsumerConfiguration LoadConfiguration()
        {
            throw new NotImplementedException();
        }

        private void ShowTextField(bool visible)
        {
            _optionsTextField.Visible = visible;
            _optionsListView.Visible = !visible;
            if (visible) _optionsTextField.CursorPosition = _optionsTextField.Text.Length;
        }

        private Action SetConfigurationAndStop(Func<ConsumerConfiguration> set) =>
            () =>
            {
                _configuration = set?.Invoke();
                Application.RequestStop();
            };

        private Action<View.KeyEventEventArgs> CreateKeyboardEventHandler(View view, Action onEnter) =>
            args =>
            {
                if (!view.HasFocus || args.KeyEvent.Key is not (Key.Esc or Key.Enter)) return;

                if (args.KeyEvent.Key == Key.Enter) onEnter();

                _propertiesListView.SetFocus();
                args.Handled = true;
            };
    }
}
