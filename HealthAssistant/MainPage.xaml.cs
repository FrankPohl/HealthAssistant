using Deepgram;
using Deepgram.Transcription;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Deepgram.Logger;
using Serilog;


namespace HealthAssistant;

public partial class MainPage : ContentPage
{
    WaveFileWriter _waveWriter;
    IWaveIn _waveIn;
    ILiveTranscriptionClient _deepgramLive;
    LiveTranscriptionOptions _options;
    public MainPage()
	{
		InitializeComponent();

        var log = new LoggerConfiguration()
        .WriteTo.Debug(outputTemplate: "{Timestamp:HH:mm} [{Level}]: {Message}\n")
        .CreateLogger();
        Log.Information("Hello, world!");
        var factory = new LoggerFactory();
        factory.AddSerilog(log);
        LogProvider.SetLogFactory(factory);
    }

    #region private helpers

    private void DispatchStatusMsg(string Message)
    {
        Dispatcher.Dispatch(() =>
        {
            this.Status.Text += Message + Environment.NewLine;
        });

    }
    #endregion

    #region UI events
    private async void OnStopAudioClicked(object sender, EventArgs e)
    {
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
        }
        try
        {
            if (_deepgramLive != null)
            {
                Debug.WriteLine($"DeepGram State {_deepgramLive.State}");
                await _deepgramLive.FinishAsync();
            }
        }
        catch (Exception)
        {

            Debug.WriteLine("_deepgramLive.FinishAsync crashed");
        }
    }

    private async void OnReOpenDeepgramConnectionClicked(object sender, EventArgs e)
    {
        await _deepgramLive.StartConnectionAsync(_options);
    }

    private void OnStartAudioClicked(object sender, EventArgs e)
    {
        // Get the enumeration of the audio in devices
        var deviceEnum = new MMDeviceEnumerator();
        var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

        var device = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        Debug.WriteLine($"Using input device: {device.FriendlyName}");
        DispatchStatusMsg($"Using input device: {device.FriendlyName}");
        // just in case some other program has muted it
        device.AudioEndpointVolume.Mute = false;

        // Setup the auio capture of NAudio
        _waveIn = new WasapiCapture(device);
        _waveIn.DataAvailable += OnDataAvailable;
        _waveIn.RecordingStopped += OnRecordginStopped;
        i = 0;
        Debug.WriteLine($"Waveformat: Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"            Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"            Bits: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"            Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"            Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        _waveWriter = new WaveFileWriter(@"C:\Temp\Test.wav", _waveIn.WaveFormat);
        _waveIn.StartRecording();
    }

    private async void OnInitDeepgramClicked(object sender, EventArgs e)
	{
        var credentials = new Credentials("9bc2b8137471bdc642b5cb4b7c29c6e960462a2e");
        credentials.ApiUrl = @"https://api.deepgram.com";
        var deepgramClient = new DeepgramClient(credentials);
        _options = new LiveTranscriptionOptions()
        {
            Punctuate = true,
            Diarize = true,
            Encoding = Deepgram.Common.AudioEncoding.Linear16,
            Channels = 2,
            SampleRate = 48000            
        };
        _deepgramLive = deepgramClient.CreateLiveTranscriptionClient();
        _deepgramLive.ConnectionClosed += DeepgramLive_ConnectionClosed;
        _deepgramLive.TranscriptReceived += DeepgramLive_TranscriptReceived;
        _deepgramLive.ConnectionOpened += DeepgramLive_ConnectionOpened;
        _deepgramLive.ConnectionError += DeepgramLive_ConnectionError;
        await _deepgramLive.StartConnectionAsync(_options);
    }

    #endregion

    #region Deepgram Event Handling
    private void DeepgramLive_ConnectionError(object sender, ConnectionErrorEventArgs e)
    {
        DispatchStatusMsg($"Deepgram Error: {e.Exception.Message}");
        Debug.WriteLine($"Deepgram Error: {e.Exception.Message}");
    }

    private void DeepgramLive_ConnectionOpened(object sender, ConnectionOpenEventArgs e)
    {
        DispatchStatusMsg($"Deepgram Connection opened");
        Debug.WriteLine("Deepgram Connection opened");
    }

    private void DeepgramLive_TranscriptReceived(object sender, TranscriptReceivedEventArgs e)
    {
        Debug.WriteLine("Transcript received");
        {
            if (e.Transcript.IsFinal &&
                e.Transcript.Channel.Alternatives.First().Transcript.Length > 0)
            {
                var transcript = e.Transcript;
                Debug.WriteLine($"Deepgram Recognition: {transcript.Channel.Alternatives.First().Transcript}");
                DispatchStatusMsg($"Deepgram Recognition: {transcript.Channel.Alternatives.First().Transcript}");
            }
        }
    }

    private void DeepgramLive_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
    {
        DispatchStatusMsg($"Deepgram Connection closed");
        Debug.WriteLine("Deepgram Connection closed");
    }

    #endregion

    #region nAudio Event Handling

    int i =0;
    private void  OnDataAvailable(object sender, WaveInEventArgs e)
    {
        // Write to file
        i += 1;
        Debug.WriteLine($"Sending Data Package no: {i} with length {e.Buffer.Length}");
        _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

        int secondsRecorded = (int)(_waveWriter.Length / _waveWriter.WaveFormat.AverageBytesPerSecond);
        if (secondsRecorded >= 60)
        {
            Debug.WriteLine("Stop automatically after 60 seconds ");
            _waveIn.StopRecording();
        }

        // write tp deepgram
        _deepgramLive.SendData(e.Buffer);
        Task.Delay(50).Wait();
    }

	private void OnRecordginStopped(object sender, StoppedEventArgs e)
    {
        _waveWriter.Close();
        Debug.WriteLine("Audio In stopped");
        DispatchStatusMsg($"Audio In stopped");

    }
    #endregion
}

