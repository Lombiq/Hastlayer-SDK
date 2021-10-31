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
                    "_Start (F5)",
                    string.Empty,
                    () => Application.RequestStop()),
            });

            var leftPane = new FrameView("Properties") { ColorScheme = Colors.Base };
            var rightPane = new FrameView("Options") { ColorScheme = Colors.Base };

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
            top.Add(menu);
            top.Add(leftPane);
            top.Add(rightPane);
            top.TileHorizontally(leftPane, rightPane, sidebarWidth, (1, 0, 0, 0));
            leftPane.Add(_propertiesListView);
            rightPane.Add(_optionsTextField);
            rightPane.Add(_optionsListView);

            top.KeyPress += args =>
            {
                if (args.KeyEvent.Key == Key.F5) Application.RequestStop();
            };

            AddKeyboardEventHandler(
                _optionsTextField,
                () => _currentOptionsTextFieldEventHandler?.Invoke(_optionsTextField.Text.ToString()));

            AddKeyboardEventHandler(
                _optionsListView,
                () =>
                {
                    var item = _optionsListView.Source.ToList()[_optionsListView.SelectedItem];
                    _currentOptionsListViewEventHandler?.Invoke(item);
                });

            Application.Run(
                top,
                exception =>
                {
                    _hastlayerTask
                        .Result
                        .GetLogger<Gui>()
                        .LogError(exception, "an error during GUI operation");
                    _configuration = null;
                    Console.WriteLine(
                        "An error occurred while in GUI mode. Please check the logs in the App_Data/logs directory.");
                    Application.RequestStop();
                    return true;
                });

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

        private (Button Ok, FrameView Dialog, Action Close) CreateDialog(
            string title,
            string label,
            float widthPercent,
            float heightPercent)
        {
            var dialogFrame = new FrameView(title)
            {
                ColorScheme = Colors.Dialog,
                X = Pos.Center() - Pos.Percent(widthPercent / 2),
                Y = Pos.Center() - Pos.Percent(heightPercent / 2),
                Width = Dim.Percent(widthPercent),
                Height = Dim.Percent(heightPercent),
            };
            Application.Top.Add(dialogFrame);

            void Close() => Application.Top.Remove(dialogFrame);

            var buttonOk = new Button("_Ok")
            {
                X = 1,
                Y = Pos.Percent(100) - 1,
                Width = Dim.Percent(50),
                Height = 1,
            };
            var buttonCancel = new Button("_Cancel")
            {
                X = Pos.Center() + 1,
                Y = Pos.Percent(100) - 1,
                Width = Dim.Percent(50),
                Height = 1,
            };
            buttonCancel.Clicked += Close;

            dialogFrame.Add(
                new Label(label) { Y = 1, CanFocus = false }.FillHorizontally(verticalCenter: false),
                buttonOk,
                buttonCancel);

            return (buttonOk, dialogFrame, Close);
        }

        private void SaveConfiguration()
        {
            var title = "Save";
            var label = "Please enter the name of the configuration!";
            var widthPercent = 60f;
            var heightPercent = 30f;

            var (buttonOk, dialogFrame, close) = CreateDialog(title, label, widthPercent, heightPercent);

            var input = new TextField().FillHorizontally();

            void OkClicked()
            {
                var text = input.Text.ToString();
                if (string.IsNullOrWhiteSpace(text))
                {
                    input.SetFocus();
                    return;
                }

                _savedConfigurations[text] = _configuration;
                close();
                ConsumerConfiguration.SaveConfigurations(_savedConfigurations);
            }

            buttonOk.Clicked += OkClicked;
            input.OnEnterKeyPressed(OkClicked);
            input.OnEscKeyPressed(close);

            dialogFrame.Add(input);
            input.SetFocus();
        }

        private void LoadConfiguration()
        {
            var title = "Load";
            var label = "Please select the configuration to load!";
            var widthPercent = 60f;
            var heightPercent = 60f;

            var (buttonOk, dialogFrame, close) = CreateDialog(title, label, widthPercent, heightPercent);

            var names = _savedConfigurations.Keys.ToList();
            var list = new ListView(names)
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(100),
                Height = Dim.Percent(100),
            };

            var scrollView = new ScrollView
            {
                X = 1,
                Y = 3,
                Width = Dim.Percent(100) - 2,
                Height = Dim.Percent(100) - 2,
                CanFocus = false,
                ContentSize = new Size(names.Max(name => name.Length), names.Count),
            };

            void OkClicked()
            {
                _configuration = _savedConfigurations[names[list.SelectedItem]];
                close();
                PropertiesListView_SelectedChanged(_propertiesListView.Source.ToList()[0]?.ToString());
            }

            buttonOk.Clicked += OkClicked;
            list.OnEnterKeyPressed(OkClicked);
            list.OnEscKeyPressed(close);

            dialogFrame.Add(scrollView);
            scrollView.Add(list);
            list.SetFocus();
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
