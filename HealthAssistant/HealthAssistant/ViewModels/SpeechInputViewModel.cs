using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthAssistant.Models;
using HealthAssistant.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;

namespace HealthAssistant.ViewModels
{
    public class SpeechInputViewModel : ObservableObject
    {
        IDataStore<MeasuredItem> _dataStore;
        ISpeechRecognizer _recognizer;
        TextEvaluation _evaluator;
        private InputItemViewModel _inputItem = new InputItemViewModel() { MeasurementType = Measurement.NotSet, MeasurementDateTime = DateTime.MinValue };
        public ObservableCollection<MessageDetailViewModel> Messages { get; }
        public ObservableCollection<InputItemViewModel> BloodPressureList { get; }
        public ObservableCollection<InputItemViewModel> PulseList { get; }
        public ObservableCollection<InputItemViewModel> GlucoseList { get; }
        public ObservableCollection<InputItemViewModel> TemperatureList { get; }
        public ObservableCollection<InputItemViewModel> WeightList { get; }

        public IAsyncRelayCommand StartRecognitionCommand { get; }
        public IAsyncRelayCommand<string> ProcessTextCommand { get; }


        private string WelcomeMessage = $"Hello. {Environment.NewLine}You can enter your heart rate, glucose, weight, or blood pressure by voice. {Environment.NewLine}For example, say  'My pulse was 73 at 9:30 am today.";
        public SpeechInputViewModel()
        {
            Debug.WriteLine("SPeechINput Constrcutor");
            StartRecognitionCommand = new AsyncRelayCommand(StartRecognitionAsync);
            ProcessTextCommand = new AsyncRelayCommand<string>(ProcessCommandAsync);
            _recognizer = new SpeechRecognizer();
            WireUpRecognizerEvents();
            Messages = new ObservableCollection<MessageDetailViewModel>();
            BloodPressureList = new ObservableCollection<InputItemViewModel>();
            PulseList = new ObservableCollection<InputItemViewModel>();
            TemperatureList = new ObservableCollection<InputItemViewModel>();
            WeightList = new ObservableCollection<InputItemViewModel>();
            _evaluator = new TextEvaluation();
            _dataStore = new HealthDataStore();
        }


        private async Task StartRecognitionAsync()
        {
            Debug.WriteLine("Start lsitening command");
            await _recognizer.StarListening();
            IsListening = true;
        }
        private async Task ProcessCommandAsync(string TextInput)
        {
            Debug.WriteLine($"Text Inut is: {TextInput} ");
            AnalyzeText(TextInput);
        }

        //// compare new info with already gathered info and ask for missing info

        private void WireUpRecognizerEvents()
        {
            _recognizer.RecognizerStartedListening += _recognizer_RecognizerStartedListening;
            _recognizer.RecognitionResult += _recognizer_RecognitionResult;
            _recognizer.RecognizerException += _recognizer_RecognizerException;
            _recognizer.RecognizerStoppedListening += _recognizer_RecognizerStoppedListening;
        }

        private void _recognizer_RecognizerStoppedListening(object sender, EventArgs e)
        {
            IsListening = false;
        }

        private void _recognizer_RecognizerException(object sender, RecognizerError e)
        {
            Debug.WriteLine($"_recognizer_RecognizerException {e}");
        }
        private void AddSeverMessage(string Message)
        {
            Messages.Add(new MessageDetailViewModel() { Sender = MessageSender.Server, Message = $"{Message}" });
        }
        private void AddClientMessage(string Message)
        {
            Messages.Add(new MessageDetailViewModel() { Sender = MessageSender.User, Message = $"{Message}" });
        }

        private void LogState()
        {
            Debug.WriteLine("State");
            Debug.WriteLine($"  Actvie Command: {ActiveCommand}");
            Debug.WriteLine($"  Input Item Type: {_inputItem.MeasurementType}");
            Debug.WriteLine($"  Input DateTime: {_inputItem.MeasurementDateTime}");
            Debug.WriteLine($"  Input Dia: {_inputItem.DiaValue}");
            Debug.WriteLine($"  Input Sys: {_inputItem.SysValue}");
            Debug.WriteLine($"  Input Value: {_inputItem.MeasuredValue}");
            Debug.WriteLine($"  Input HasValue: {_inputItem.HasValue}");
            Debug.WriteLine($"  Input Date is Set: {_inputItem.TimeIsSet}");
            Debug.WriteLine($"  Input Item complete: {_inputItem.IsComplete}");
        }
        private Commands ActiveCommand;
        private bool _dataInInputMode = false;
        private bool _askForConfirmation = false;

        private string GetMeasurementDisplayText(Measurement Tye)
        {
            switch (_inputItem.MeasurementType)
            {
                case Measurement.BloodPressure:
                    return "Blood Pressurre";
                case Measurement.Glucose:
                    return "Glucose";
                case Measurement.Pulse:
                    return "Pulse";
                case Measurement.Temperature:
                    return "Temperature";
                case Measurement.Weight:
                    return "Weight";
                case Measurement.NotSet:
                    return "";
                default:
                    return "";
            }
        }
        private void AnalyzeText(string InputText)
        {
            var command = _evaluator.GetCommandInText(InputText);
            if (ActiveCommand == Commands.Pause)
            {
                IsListening = false;
                return;
            }
            if (command == Commands.Continue)
            {
                Debug.WriteLine("Restart processing.");
                IsListening = true;
                ActiveCommand = Commands.Undefined;
            }
            if (_askForConfirmation)
            {
                Debug.WriteLine("ask for confirmation");
                if (_evaluator.IsConfirmationText(InputText))
                {
                    LogState();
                    Debug.WriteLine("Store the item");
                    _dataStore.AddItemAsync(_inputItem.Item);
                    AddSeverMessage($"I saved your measuremnt data for {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                    ActiveCommand = Commands.Undefined;
                    _askForConfirmation = false;
                    return;
                }
                else
                {
                    AddSeverMessage($"Nothing saved. You can start another input");
                    ActiveCommand = Commands.Undefined;
                    _askForConfirmation = false;
                    return;
                }
            }

            if ((ActiveCommand == Commands.Undefined) || ((ActiveCommand != command) && _dataInInputMode == false))
            {
                Debug.WriteLine($"Command set from {ActiveCommand} to {command}");
                ActiveCommand = command;
                // Check if chart or list should be shown

                switch (ActiveCommand)
                {
                    case Commands.Show:
                        break;
                    case Commands.ShowList:
                        Debug.WriteLine("Show list command");
                        ShowInput = false;
                        ShowBloodPressureList = true;
                        ShowGlucoseList = true;
                        ShowPulseList = true;
                        ShowTemperatureList = true;
                        ShowWeightList = true;
                        break;
                    case Commands.ShowListWeight:
                        ShowInput = false;
                        ShowBloodPressureList = false;
                        ShowGlucoseList = false;
                        ShowPulseList = false;
                        ShowTemperatureList = false;
                        ShowWeightList = true;
                        break;
                    case Commands.ShowLIstGlucose:
                        ShowInput = false;
                        ShowBloodPressureList = false;
                        ShowGlucoseList = true;
                        ShowPulseList = false;
                        ShowTemperatureList = false;
                        ShowWeightList = false;
                        Debug.WriteLine("Show list command");
                        break;
                    case Commands.ShowListBloodPressure:
                        ShowInput = false;
                        ShowBloodPressureList = true;
                        ShowGlucoseList = false;
                        ShowPulseList = false;
                        ShowTemperatureList = false;
                        ShowWeightList = false;
                        Debug.WriteLine("Show list command");
                        break;
                    case Commands.ShowListTemperature:
                        ShowInput = false;
                        ShowBloodPressureList = false;
                        ShowGlucoseList = false;
                        ShowPulseList = false;
                        ShowTemperatureList = true;
                        ShowWeightList = false;
                        Debug.WriteLine("Show list command");
                        break;
                    case Commands.ShowChart:
                        Debug.WriteLine("Show chart command");
                        break;
                    case Commands.ShowLChartWeight:
                        Debug.WriteLine("Show chart command");
                        break;
                    case Commands.ShowChartGlucose:
                        Debug.WriteLine("Show chart command");
                        break;
                    case Commands.ShowChartBloodPressure:
                        Debug.WriteLine("Show chart command");
                        break;
                    case Commands.ShowChartTemperature:
                        Debug.WriteLine("Show chart command");
                        break;
                    case Commands.Input:
                        ShowInput = true;
                        _dataInInputMode = true;
                        break;
                    case Commands.InputWeight:
                        ShowInput = true;
                        Debug.WriteLine("Weight input");
                        _dataInInputMode = true;
                        _inputItem.MeasurementType = Measurement.Weight;
                        break;
                    case Commands.InputGlucose:
                        ShowInput = true;
                        Debug.WriteLine("Glucse input");
                        _dataInInputMode = true;
                        _inputItem.MeasurementType = Measurement.Glucose;
                        break;
                    case Commands.InputBloodPressure:
                        ShowInput = true;
                        Debug.WriteLine("Blood presure");
                        _dataInInputMode = true;
                        _inputItem.MeasurementType = Measurement.BloodPressure;
                        break;
                    case Commands.InputTemperature:
                        ShowInput = true;
                        Debug.WriteLine("Temperature input");
                        _dataInInputMode = true;
                        _inputItem.MeasurementType = Measurement.Temperature;
                        break;
                    case Commands.Pause:
                        ShowInput = true;
                        break;
                    case Commands.Continue:
                        ShowInput = true;
                        break;
                    case Commands.Undefined:
                        ShowInput = true;
                        break;
                    default:
                        ShowInput = true;
                        break;
                }
            }
            // we are in input mode so we have to analyze the input for data
            if (_dataInInputMode)
            {
                Debug.WriteLine($"Data Input is {_dataInInputMode} with Comand {ActiveCommand}");
                var firstValue = _evaluator.GetFirstValue(InputText);
                var secondValue = _evaluator.GetSecondValue(InputText);
                var retTime = _evaluator.GetTime(InputText);
                var retDate = _evaluator.GetDateValue(InputText);
                if (firstValue != null)
                {
                    if (_inputItem.MeasurementType == Measurement.BloodPressure)
                    {
                        _inputItem.SysValue = firstValue;
                    }
                    else
                    {
                        _inputItem.MeasuredValue = firstValue;
                    }
                }
                if (secondValue != null)
                {
                    if (_inputItem.MeasurementType == Measurement.BloodPressure)
                    {
                        _inputItem.DiaValue= secondValue;
                    }
                }
                if (retDate != null)
                {
                    _inputItem.MeasurementDateTime = retDate.Value.Date;
                }
                if (retTime != null)
                {
                    if (retTime.Value.Date == DateTime.MinValue)
                    {

                        _inputItem.MeasurementDateTime = _inputItem.MeasurementDateTime.Date + new TimeSpan(retTime.Value.Hour, retTime.Value.Minute, 0);
                    }
                    else
                    {
                        _inputItem.MeasurementDateTime = retTime.Value;
                    }
                }
                Debug.WriteLine($"Data is First: {firstValue} second: {secondValue} Time: {retTime} Date: {retDate}");
            }
            LogState();
            if (!_inputItem.HasValue)
            {
                Debug.WriteLine("Ask for values");

                AddSeverMessage($"Please tell me which values you have measured for {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                return;
            }
            if (!_inputItem.DateIsSet)
            {
                Debug.WriteLine("Ask for date time");
                
                AddSeverMessage($"When have you measured your {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                return;
            }
            if (!_inputItem.TimeIsSet)
            {
                Debug.WriteLine("Ask for time");

                AddSeverMessage($"At which time did you measure {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                return;
            }
            if (_inputItem.IsComplete)
            {
                Debug.WriteLine("Ask for confirmation to store");
                _askForConfirmation = true;
                if (_inputItem.MeasurementType == Measurement.BloodPressure)
                {
                    string msg = $"Do you want to save your {GetMeasurementDisplayText(_inputItem.MeasurementType)} Systolic {_inputItem.SysValue} and Diastlic {_inputItem.DiaValue} measured at {_inputItem.MeasurementDateTime.ToString("g")}?";
                    AddSeverMessage(msg);
                }
                else
                {
                    string msg = $"Do you want to save your {GetMeasurementDisplayText(_inputItem.MeasurementType)} value {_inputItem.MeasuredValue} measured at {_inputItem.MeasurementDateTime.ToString("g")}?";
                    AddSeverMessage(msg);
                }
                return;
            }
        }
        private void _recognizer_RecognitionResult(object sender, string e)
        {
            Debug.WriteLine($"_recognizer_RecognitionResult {e}");
            AddClientMessage(e);
            AnalyzeText(e);
        }

        private void _recognizer_RecognizerStartedListening(object sender, EventArgs e)
        {
            Debug.WriteLine("RecognizerStartListenin");
        }

        public async void OnAppearing()
        {

            Debug.WriteLine("Viewodel on paeragin");
            ActiveCommand = Commands.Undefined;
            IsBusy = true;
            IsListening = false;
            AppState = "Click Mic to start listening.";
            Messages.Clear();
            Messages.Add(new MessageDetailViewModel() { Sender = MessageSender.Server, Message = $"{WelcomeMessage}" });

            IsBusy = false;
        }

        public InputItemViewModel InputItem
        {
            get => _inputItem;
            set
            {
                SetProperty(ref _inputItem, value);
            }
        }

        private string _appState;
        public string AppState
        {
            get => _appState;
            private set
            {
                SetProperty(ref _appState, value);
            }
        }

        private bool _isListening;

        public bool IsListening
        {
            get => _isListening;
            private set
            {
                SetProperty(ref _isListening, value);
            }
        }
        private bool _isBusy;

        public bool IsBusy
        {
            get => _isBusy;
            private set
            {
                SetProperty(ref _isBusy, value);
            }
        }

        public void SetInputAsText(string Text)
        {
            Debug.WriteLine($"GetTextInput {Text}");
            AddClientMessage(Text);
            AnalyzeText(Text);
        }

        private bool _showBloodPressureList = false;

        public bool ShowBloodPressureList
        {
            get => _showBloodPressureList;
            private set
            {
                SetProperty(ref _showBloodPressureList, value);
            }
        }
        private bool _showGlucoseList = false;

        public bool ShowGlucoseList
        {
            get => _showGlucoseList;
            private set
            {
                SetProperty(ref _showGlucoseList, value);
            }
        }

        private bool _showPulseList = false;

        public bool ShowPulseList
        {
            get => _showPulseList;
            private set
            {
                SetProperty(ref _showPulseList, value);
            }
        }

        private bool _showTemperatureList = false;

        public bool ShowTemperatureList
        {
            get => _showTemperatureList;
            private set
            {
                SetProperty(ref _showTemperatureList, value);
            }
        }

        private bool _showWeightList = false;

        public bool ShowWeightList
        {
            get => _showWeightList;
            private set
            {
                SetProperty(ref _showWeightList, value);
            }
        }

        private bool _showInput = true;

        public bool ShowInput
        {
            get => _showInput;
            private set
            {
                SetProperty(ref _showInput, value);
            }
        }


    }
}