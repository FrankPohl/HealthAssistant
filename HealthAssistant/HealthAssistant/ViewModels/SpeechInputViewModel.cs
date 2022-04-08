using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HealthAssistant.Models;
using HealthAssistant.Services;
using Microsoft.Maui.Dispatching;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace HealthAssistant.ViewModels
{
    public class SpeechInputViewModel : ObservableObject
    {
        private bool _askForConfirmation = false;
        private bool _askForDate = false;
        private bool _askForTime = false;
        private bool _askForValue = false;
        private bool _dataInInputMode = false;
        private IDataStore<MeasuredItem> _dataStore;
        private string _directTextInput;
        private TextEvaluation _evaluator;
        private InputItemViewModel _inputItem = new InputItemViewModel() { MeasurementType = Measurement.NotSet, MeasurementDateTime = DateTime.MinValue };
        private ISpeechRecognizer _recognizer;
        private SpeechCommand ActiveCommand;
        private string WelcomeMessage = $"Hello. {Environment.NewLine}You can enter your blood pressure, glucose, heart rate, weight, temperature or with your voice. {Environment.NewLine}Say, for example 'My weight was 92 kg today at 9 o'clock.";

        public SpeechInputViewModel()
        {
            Debug.WriteLine("SpeechInput Constrcutor");
            StartRecognitionCommand = new AsyncRelayCommand(StartRecognitionAsync);
            ProcessTextCommand = new AsyncRelayCommand<string>(ProcessCommandAsync);
            _recognizer = new SpeechRecognizer();
            BloodPressureList = new ObservableCollection<MeasuredItem>();
            GlucoseList = new ObservableCollection<MeasuredItem>();
            PulseList = new ObservableCollection<MeasuredItem>();
            TemperatureList = new ObservableCollection<MeasuredItem>();
            WeightList = new ObservableCollection<MeasuredItem>();
            WireUpRecognizerEvents();
            Messages = new ObservableCollection<MessageDetailViewModel>();
            _evaluator = new TextEvaluation();
            _dataStore = new HealthDataStore();
        }

        #region Public methods and properties
        private string _appState;
        public string AppState
        {
            get => _appState;
            private set
            {
                SetProperty(ref _appState, value);
            }
        }

        public ObservableCollection<MeasuredItem> BloodPressureList { get; private set; }
        public string DirectTextInput
        {
            get => _directTextInput;
            private set
            {
                SetProperty(ref _directTextInput, value);
            }
        }

        public ObservableCollection<MeasuredItem> GlucoseList { get; private set; }
        public InputItemViewModel InputItem
        {
            get => _inputItem;
            set
            {
                SetProperty(ref _inputItem, value);
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

        private bool _isListening;
        public bool IsListening
        {
            get => _isListening;
            private set
            {
                SetProperty(ref _isListening, value);
            }
        }

        public ObservableCollection<MessageDetailViewModel> Messages { get; }

        private async Task ProcessCommandAsync(string TextInput)
        {
            Debug.WriteLine($"Text Inut Input as Paramter {TextInput} and property {DirectTextInput} ");
            if (String.IsNullOrEmpty(TextInput))
            {
                AddClientMessage("Please enter some text for processing.");
            }
            else
            {
                AddClientMessage(TextInput);
                AnalyzeText(TextInput, true);
                DirectTextInput = "";
            }
        }

        public IAsyncRelayCommand<string> ProcessTextCommand { get; }
        public ObservableCollection<MeasuredItem> PulseList { get; private set; }

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

        private bool _showInput = true;
        public bool ShowInput
        {
            get => _showInput;
            private set
            {
                SetProperty(ref _showInput, value);
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

        private async Task StartRecognitionAsync()
        {
            if (IsListening)
            {
                Debug.WriteLine("Stop listening command");
                _recognizer.StopListening();
                IsListening = false;
            }
            else
            {
                Debug.WriteLine("Start listening command");
                await _recognizer.StarListening();
                if (ActiveCommand == SpeechCommand.Pause)
                {
                    ActiveCommand = SpeechCommand.Undefined;
                }
                IsListening = true;
            }
        }

        public IAsyncRelayCommand StartRecognitionCommand { get; }
        public ObservableCollection<MeasuredItem> TemperatureList { get; set; }
        public ObservableCollection<MeasuredItem> WeightList { get; private set; }
        public async void OnAppearing()
        {
            Debug.WriteLine("Viewodel OnAppearing");
            ActiveCommand = SpeechCommand.Undefined;
            IsBusy = true;
            IsListening = false;
            AppState = "Click Mic to start listening.";
            Messages.Clear();
            Messages.Add(new MessageDetailViewModel() { Sender = MessageSender.Server, Message = $"{WelcomeMessage}" });
            var bpList = await _dataStore.GetItemsAsync(Measurement.BloodPressure);
            foreach (var item in bpList)
            {
                BloodPressureList.Add(item);
            }
            var glucoseList = await _dataStore.GetItemsAsync(Measurement.Weight);
            foreach (var item in glucoseList)
            {
                GlucoseList.Add(item);
            }
            var pulseList = await _dataStore.GetItemsAsync(Measurement.Pulse);
            foreach (var item in pulseList)
            {
                PulseList.Add(item);
            }
            var tempList = await _dataStore.GetItemsAsync(Measurement.Temperature);
            foreach (var item in tempList)
            {
                TemperatureList.Add(item);
            }
            var weightList = await _dataStore.GetItemsAsync(Measurement.Weight);
            foreach (var item in weightList)
            {
                WeightList.Add(item);
            }
            IsBusy = false;
        }

        #endregion


        #region Process Input Text 

        /// <summary>
        /// Reset all the flags and input items whenever a new measurement input starts
        /// </summary>
        private void ResetInputItemAndStatusFlags()
        {
            _dataInInputMode = false;
            _askForConfirmation = false;
            _askForTime = false;
            _askForDate = false;
            _askForValue = false;
            ActiveCommand = SpeechCommand.Undefined;
            _askForConfirmation = false;
            _inputItem = new InputItemViewModel();
            LogState();
        }


        private void AskForConfirmation()
        {
            Debug.WriteLine("Ask for confirmation to store");
            _askForConfirmation = true;
            // Measurement date is shown with time only if it is today, otherwise with the date
            string dateOutput;
            if (_inputItem.MeasurementDateTime.Date == DateTime.Now.Date)
            {
                dateOutput = $"measured today at {_inputItem.MeasurementDateTime:t}";
            }
            else
            {
                dateOutput = $"measured at {_inputItem.MeasurementDateTime:g}";
            }
            if (_inputItem.MeasurementType == Measurement.BloodPressure)
            {
                string msg = $"Say Yes, if you want to save your {GetMeasurementDisplayText(_inputItem.MeasurementType)} measurement with Systolic {_inputItem.SysValue} and Diastlic {_inputItem.DiaValue} {dateOutput}?";
                AddSeverMessage(msg);
            }
            else
            {
                string msg = $"Say Yes, if you want to save your {GetMeasurementDisplayText(_inputItem.MeasurementType)} measurement with {_inputItem.MeasuredValue} {_inputItem.Unit} {dateOutput}?";
                AddSeverMessage(msg);
            }

        }
        private void ReactOnConfirmationQuery(string InputText)
        {
            Debug.WriteLine("ask for confirmation");
            if (_evaluator.IsConfirmationText(InputText))
            {
                LogState();
                Debug.WriteLine("Store the item");
                _dataStore.AddItemAsync(_inputItem.Item);
                switch (_inputItem.MeasurementType)
                {
                    case Measurement.BloodPressure:
                        BloodPressureList.Add(_inputItem.Item);
                        break;

                    case Measurement.Glucose:
                        GlucoseList.Add(_inputItem.Item);
                        break;

                    case Measurement.Pulse:
                        PulseList.Add(_inputItem.Item);
                        break;

                    case Measurement.Temperature:
                        TemperatureList.Add(_inputItem.Item);
                        break;

                    case Measurement.Weight:
                        WeightList.Add(_inputItem.Item);
                        break;

                    case Measurement.NotSet:
                    default:
                        break;
                }
                AddSeverMessage($"I saved your {GetMeasurementDisplayText(_inputItem.MeasurementType)} measurement. {Environment.NewLine}You can enter a new value or check the data, e.g. 'show weight'.");
                ResetInputItemAndStatusFlags();
                return;
            }
            else
            {
                AddSeverMessage($"Nothing saved. You can start another input");
                ActiveCommand = SpeechCommand.Undefined;
                _askForConfirmation = false;
                _inputItem.MeasurementType = Measurement.NotSet;
                return;
            }

        }
        private void SetFlagsForCommand(SpeechCommand Command)
        {
            switch (ActiveCommand)
            {
                case SpeechCommand.Show:
                    Debug.WriteLine("Show list command and ask for info");
                    AddSeverMessage("Which measurement do you want to see? Blood Pressure, Weight, Temeprature, Pulse or Glucose?");
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    _dataInInputMode = false;
                    break;

                case SpeechCommand.ShowBloodPressure:
                    ShowInput = false;
                    ShowBloodPressureList = true;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    _dataInInputMode = false;
                    Debug.WriteLine("Show blood pressure list command");
                    break;

                case SpeechCommand.ShowGlucose:
                    ShowInput = false;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = true;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    _dataInInputMode = false;
                    Debug.WriteLine("Show glcuseo list command");
                    break;

                case SpeechCommand.ShowPulse:
                    Debug.WriteLine("Show list pulse command");
                    ShowInput = false;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = true;
                    ShowTemperatureList = false;
                    _dataInInputMode = false;
                    ShowWeightList = false;
                    break;

                case SpeechCommand.ShowTemperature:
                    ShowInput = false;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = true;
                    ShowWeightList = false;
                    _dataInInputMode = false;
                    Debug.WriteLine("Show list command");
                    break;

                case SpeechCommand.ShowWeight:
                    ShowInput = false;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = true;
                    _dataInInputMode = false;
                    break;

                case SpeechCommand.Input:
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    _dataInInputMode = true;
                    _dataInInputMode = false;
                    break;

                case SpeechCommand.InputBloodPressure:
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    Debug.WriteLine("Blood presure");
                    _dataInInputMode = true;
                    _inputItem.MeasurementType = Measurement.BloodPressure;
                    break;

                case SpeechCommand.InputGlucose:
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    Debug.WriteLine("Glucse input");
                    _dataInInputMode = true;
                    _inputItem.MeasurementType = Measurement.Glucose;
                    break;

                case SpeechCommand.InputPulse:
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    Debug.WriteLine("Pulse");
                    _dataInInputMode = true;
                    _inputItem.MeasurementType = Measurement.Pulse;
                    break;

                case SpeechCommand.InputTemperature:
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    Debug.WriteLine("Temperature input");
                    _dataInInputMode = true;
                    _inputItem.MeasurementType = Measurement.Temperature;
                    break;

                case SpeechCommand.InputWeight:
                    ShowInput = true;
                    ShowBloodPressureList = false;
                    ShowGlucoseList = false;
                    ShowPulseList = false;
                    ShowTemperatureList = false;
                    ShowWeightList = false;
                    Debug.WriteLine("Weight input");
                    _dataInInputMode = true;
                    _inputItem.MeasurementType = Measurement.Weight;
                    break;

                case SpeechCommand.ClearInput:
                    ResetInputItemAndStatusFlags();
                    return;
                default:
                    ShowInput = true;
                    break;
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="InputText">The recognized Text. Translation from the speech recognizer or from user input</param>
        /// <param name="forceAnalysis"><Ste to true will evaluate the text even if speech recognition is paused/param>
        private void AnalyzeText(string InputText, bool forceAnalysis = false)
        {
            var command = _evaluator.GetCommandInText(InputText);
            if (command == SpeechCommand.Pause)
            {
                ActiveCommand = SpeechCommand.Pause;
                IsListening = false;
                return;
            }
            if (command == SpeechCommand.Continue)
            {
                Debug.WriteLine("Restart processing.");
                IsListening = true;
                ActiveCommand = SpeechCommand.Undefined;
                return;
            }

            if (!IsListening && !forceAnalysis)
            {
                return;
            }
            if (_askForConfirmation)
            {
                ReactOnConfirmationQuery(InputText);
                return;
            }

            // if the command is not yet given or not completly defined we evaluate the string to prepare the UI and the data extraction
            if ((ActiveCommand == SpeechCommand.Undefined) || (ActiveCommand == SpeechCommand.Input) || (ActiveCommand == SpeechCommand.Show) || ((ActiveCommand != command) && (_dataInInputMode == false)))
            {
                Debug.WriteLine($"Command set from {ActiveCommand} to {command}");
                ActiveCommand = command;
                // Check if chart or list should be shown
                SetFlagsForCommand(ActiveCommand);
            }
            // we are in input mode so we have to analyze the input for data
            if (_dataInInputMode)
            {
                Debug.WriteLine($"Data Input is {_dataInInputMode} with Comand {ActiveCommand}. Other flags Ask4Time: {_askForTime} Ask4Date: {_askForDate} Ask4Value: {_askForValue}");
                DateTime? retDate = null;
                DateTime? retTime = null;
                Double? firstValue = null;
                Double? secondValue = null;
                if (_askForDate)
                {
                    retDate = _evaluator.GetDateValue(InputText);
                    retTime = _evaluator.GetTime(InputText);
                }
                else
                {
                    if (_askForTime)
                    {
                        retDate = _evaluator.GetDateValue(InputText);
                        retTime = _evaluator.GetTime(InputText);
                    }
                    else
                    {
                        if (_askForValue)
                        {
                            firstValue = _evaluator.GetFirstValue(InputText);
                            secondValue = _evaluator.GetSecondValue(InputText);
                        }
                        else
                        {
                            firstValue = _evaluator.GetFirstValue(InputText);
                            secondValue = _evaluator.GetSecondValue(InputText);
                            retDate = _evaluator.GetDateValue(InputText);
                            retTime = _evaluator.GetTime(InputText);
                        }
                    }
                }
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
                        _inputItem.DiaValue = secondValue;
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
            if ((_inputItem.MeasurementType == Measurement.NotSet))
            {
                Debug.WriteLine("Ask for input type");
                AddSeverMessage($"Please tell me what you have measured, e.g. blood pressure, weight or temperature?");
                _askForValue = false;
                _askForDate = false;
                _askForTime = false;
                return;
            }
            if (!_inputItem.HasValue)
            {
                Debug.WriteLine("Ask for values");

                AddSeverMessage($"Please tell me which values you have measured for {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                _askForValue = true;
                _askForDate = false;
                _askForTime = false;
                return;
            }
            if (!_inputItem.DateIsSet)
            {
                Debug.WriteLine("Ask for date time");

                AddSeverMessage($"When have you measured your {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                _askForDate = true;
                _askForValue = false;
                _askForTime = false;
                return;
            }
            if (!_inputItem.TimeIsSet)
            {
                Debug.WriteLine("Ask for time");

                AddSeverMessage($"At which time did you measure {GetMeasurementDisplayText(_inputItem.MeasurementType)}?");
                _askForTime = true;
                _askForValue = false;
                _askForDate = false;
                return;
            }
            if (_inputItem.IsComplete)
            {
                AskForConfirmation();
                return;
            }
            LogState();
        }

        private string GetMeasurementDisplayText(Measurement Tye)
        {
            switch (_inputItem.MeasurementType)
            {
                case Measurement.BloodPressure:
                    return "Blood Pressure";

                case Measurement.Glucose:
                    return "Glucose";

                case Measurement.Pulse:
                    return "Heart Rate";

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
        #endregion
 
        #region Recognizer event handling

        private void WireUpRecognizerEvents()
        {
            _recognizer.RecognizerStartedListening += _recognizer_RecognizerStartedListening;
            _recognizer.RecognitionResult += _recognizer_RecognitionResult;
            _recognizer.RecognizerException += _recognizer_RecognizerException;
            _recognizer.RecognizerStoppedListening += _recognizer_RecognizerStoppedListening;
        }

        private void _recognizer_RecognitionResult(object sender, string e)
        {
            App.Current.Dispatcher.Dispatch(() =>
            {

                Debug.WriteLine($"_recognizer_RecognitionResult {e}");
                AddClientMessage(e);
                AnalyzeText(e);
            });
        }

        private void _recognizer_RecognizerException(object sender, RecognizerError e)
        {
            App.Current.Dispatcher.Dispatch(() =>
            {
                AppState = $"A server exception occured {e}. restart the app.";
                Debug.WriteLine($"_recognizer_RecognizerException {e}");
            });
        }

        private void _recognizer_RecognizerStartedListening(object sender, EventArgs e)
        {
            App.Current.Dispatcher.Dispatch(() =>
            {
                AppState = "Speech recognizer is listening.";
                Debug.WriteLine("RecognizerStartListenin");
            });
        }

        private void _recognizer_RecognizerStoppedListening(object sender, EventArgs e)
        {

            App.Current.Dispatcher.Dispatch(() =>
            {
                IsListening = false;
                AppState = "Recognizer is not listening.";
                AddSeverMessage("Recording automatically stopped. Click on Mic to restart");
            });
        }
        #endregion
        #region misc private methods
        private void AddClientMessage(string Message)
        {
            Messages.Add(new MessageDetailViewModel() { Sender = MessageSender.User, Message = $"{Message}" });
        }

        private void AddSeverMessage(string Message)
        {
            Messages.Add(new MessageDetailViewModel() { Sender = MessageSender.Server, Message = $"{Message}" });
        }
        private void LogState()
        {
            Debug.WriteLine("State");
            Debug.WriteLine($"  Active Command: {ActiveCommand}");
            Debug.WriteLine($"  Item Type: {_inputItem.MeasurementType}");
            Debug.WriteLine($"  Item DateTime: {_inputItem.MeasurementDateTime}");
            Debug.WriteLine($"  Values: Item Dia: {_inputItem.DiaValue} Item Sys: {_inputItem.SysValue} Item Value: {_inputItem.MeasuredValue}");
            Debug.WriteLine($"  Item Flags: HasValue: {_inputItem.HasValue} Input Date is Set: {_inputItem.TimeIsSet} Input Item complete: {_inputItem.IsComplete}");
            Debug.WriteLine($"Processing Flags");
            Debug.WriteLine($"Data Input is {_dataInInputMode}. Other flags Ask4Time: {_askForTime} Ask4Date: {_askForDate} Ask4Value: {_askForValue}");
        }
        #endregion

    }
}