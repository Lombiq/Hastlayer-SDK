using Hast.Layer;
using Hast.Samples.Consumer.Models;
using Microsoft.Extensions.Logging;
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
        private readonly Dictionary<string, ConsumerConfiguration> _savedConfigurations;
        private readonly ListView _propertiesListView = new ListView { CanFocus = true }.Fill();

        private readonly TextField _optionsTextField =
            new TextField { CanFocus = true, Visible = false }.FillHorizontally();
        private Action<string> _currentOptionsTextFieldEventHandler;

        private readonly ListView _optionsListView = new ListView { CanFocus = true, Visible = false }.Fill();
        private Action<object> _currentOptionsListViewEventHandler;

        private ConsumerConfiguration _configuration;

        private Task<Hastlayer> _hastlayerTask;
        private Task<List<string>> _deviceNamesTask;

        public Gui(Dictionary<string, ConsumerConfiguration> savedConfigurations) =>
            _savedConfigurations = savedConfigurations;

        public ConsumerConfiguration BuildConfiguration()
        {
            _configuration = new ConsumerConfiguration();

            _hastlayerTask = Task.Run(() => (Hastlayer)Hastlayer.Create(new HastlayerConfiguration()));
            _deviceNamesTask = _hastlayerTask.ContinueWith(hastlayer =>
                hastlayer
                    .Result
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
                        LoadConfiguration,
                        shortcut: Key.Q | Key.L),
                    new MenuItem (
                        "_Save",
                        "Saves the current configuration.",
                        SaveConfiguration,
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
            _propertiesListView.SelectedItemChanged += args => PropertiesListView_SelectedChanged(args.Value?.ToString());
            _propertiesListView.OpenSelectedItem += _ =>
            {
                if (_optionsListView.Visible) _optionsListView.SetFocus();
                if (_optionsTextField.Visible) _optionsTextField.SetFocus();
            };

            var top = Application.Top;
            top.ColorScheme = Colors.Base;
            top.Add(menu);
            top.Add(leftPane);
            top.Add(rightPane);
            top.TileHorizontally(leftPane, rightPane, sidebarWidth, (1, 0, 0, 0));
            leftPane.Add(_propertiesListView);
            rightPane.Add(_optionsTextField);
            rightPane.Add(_optionsListView);

            AddKeyboardEventHandler(
                _optionsListView,
                () => _currentOptionsTextFieldEventHandler?.Invoke(_optionsTextField.Text.ToString()));

            AddKeyboardEventHandler(
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

        private void PropertiesListView_SelectedChanged(string value)
        {
            switch (value)
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
                    throw new InvalidOperationException($"Unknown menu item selected ({value}).");
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

        private (Button Ok, Dialog Dialog) CreateDialog(string title, string label)
        {
            var cancel = new Button("_Cancel");
            var ok = new Button("_Ok");
            var dialog = new Dialog(title, ok, cancel) { ColorScheme = Colors.Dialog };
            dialog.Add(new Label(1, 1, label));

            cancel.Clicked += () => { Application.RequestStop(dialog); };

            return (ok, dialog);
        }

        private void AddAndStart(Dialog dialog, View view)
        {
            dialog.Add(view);
            view.SetFocus();

            Application.Run(
                dialog,
                exception =>
                {
                    _hastlayerTask
                        .Result
                        .GetLogger<Gui>()
                        .LogError(exception, "an error in {0} dialog", dialog.Title);
                    Application.RequestStop(dialog);
                    return true;
                });
        }

        private void SaveConfiguration()
        {
            var (ok, dialog) = CreateDialog("Save", "Please enter the name of the configuration!");

            var input = new TextField().FillHorizontally();
            ok.Clicked += () =>
            {
                var text = input.Text.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    input.SetFocus();
                    return;
                }

                _savedConfigurations[text] = _configuration;
                Application.RequestStop(dialog);
                ConsumerConfiguration.SaveConfigurations(_savedConfigurations);
            };

            AddAndStart(dialog, input);
        }

        private void LoadConfiguration()
        {
            var (ok, dialog) = CreateDialog("Load", "Please select the configuration to load!");

            var names = _savedConfigurations.Keys.ToList();
            var list = new ListView(names)
            {
                X = 1,
                Y = 3,
                Width = Dim.Fill(1),
                Height = names.Count,
            };
            dialog.Height = names.Count + 10;

            void OkClicked()
            {
                _configuration = _savedConfigurations[names[list.SelectedItem]];
                Application.RequestStop(dialog);
                PropertiesListView_SelectedChanged(_propertiesListView.Source.ToList()[0]?.ToString());
            }

            ok.Clicked += OkClicked;
            list.KeyPress += args =>
            {
                if (args.KeyEvent.Key == Key.Enter) OkClicked();
            };

            AddAndStart(dialog, list);

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

        private void AddKeyboardEventHandler(View view, Action onEnter)
        {
            void EventHandler(View.KeyEventEventArgs args)
            {
                if (!view.HasFocus || args.KeyEvent.Key is not (Key.Esc or Key.Enter)) return;

                if (args.KeyEvent.Key == Key.Enter) onEnter();

                _propertiesListView.SetFocus();
                args.Handled = true;
            }

            view.KeyPress += EventHandler;
        }
    }
}
