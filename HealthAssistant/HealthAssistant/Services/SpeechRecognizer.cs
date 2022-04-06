using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;
using System.Globalization;
using Deepgram;
using Deepgram.Transcription;


namespace HealthAssistant.Services;

public class SpeechRecognizer:ISpeechRecognizer
{
    DeepgramClient _deepgramClient;
    ILiveTranscriptionClient _deepgramLive;
    IWaveIn _waveIn;


    byte[] Convert32BitTo16Bit(byte[] input, int numberOfBytes)
    {
        byte[] output = new byte[numberOfBytes / 2];
        byte[] resampledOutput = new byte[numberOfBytes / 6];
        WaveBuffer sourceWaveBuffer = new WaveBuffer(input);
        WaveBuffer outputWaveBuffer = new WaveBuffer(output);
        WaveBuffer resampledWaveBuffer = new WaveBuffer(resampledOutput);
        int resampledValueCounter = 0;
        int outputCounter = 0;
        var resampleSourceSamples = new short[3];
        for (int n = 0; n < numberOfBytes / 4; n += 1)
        {
            float sample32 = sourceWaveBuffer.FloatBuffer[n];
            if (sample32 > 1.0f)
                sample32 = 1.0f;
            if (sample32 < -1.0f)
                sample32 = -1.0f;
            outputWaveBuffer.ShortBuffer[n] = (short)(sample32 * 32767);
            resampleSourceSamples[resampledValueCounter] = (short)(sample32 * 32767);
            resampledValueCounter++;
            if (resampledValueCounter == 3)
            {
                resampledValueCounter = 0;
                short resample = (short)((resampleSourceSamples[0] + resampleSourceSamples[1] + resampleSourceSamples[2]) / 3);
                resampledWaveBuffer.ShortBuffer[outputCounter] = resample;
                outputCounter++;
            }
        }
        return resampledOutput;
    }

    double _recordingLength = 0;

    void OnDataAvailableConvert(object sender, WaveInEventArgs e)
    {
        //TODO: Check for empty buffer to prevent sending ewmpty buffers

        var convertedBuffer = Convert32BitTo16Bit(e.Buffer, e.BytesRecorded);
        _deepgramLive.SendData(convertedBuffer);
        _recordingLength += e.BytesRecorded / _bytesPerSecond;
        if (_recordingLength >= 60)
        {
            Debug.WriteLine("Stop automatically after 60 seconds ");
            _waveIn.StopRecording();
        }
    }

    void OnRecordingStopped(object sender, StoppedEventArgs e)
    {
        _recordingLength = 0;
        _deepgramLive.FinishAsync();
        Debug.WriteLine($"   Recordinfg stopped");
    }


    void DeepgramLive_ConnectionError(object sender, ConnectionErrorEventArgs e)
    {
        Debug.WriteLine($"Deepgram Error: {e.Exception.Message}");
    }

    void DeepgramLive_ConnectionOpened(object sender, ConnectionOpenEventArgs e)
    {
        Debug.WriteLine("Deepgram Connection opened");
        RecognizerStartedListening?.Invoke(this, EventArgs.Empty);
    }

    void DeepgramLive_TranscriptReceived(object sender, TranscriptReceivedEventArgs e)
    {
        Debug.WriteLine("Transcript received");
        RecognizerIsProcessing?.Invoke(this, EventArgs.Empty);
            if (e.Transcript.IsFinal &&
                e.Transcript.Channel.Alternatives.First().Transcript.Length > 0)
            {
                string recognizedValue = e.Transcript.Channel.Alternatives.First().Transcript;

                Debug.WriteLine($"Deepgram Recognition: {recognizedValue}");
                RecognitionResult?.Invoke(this, recognizedValue);
            }
    }


    void DeepgramLive_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
    {
        RecognizerStoppedListening?.Invoke(this, EventArgs.Empty);
        Debug.WriteLine("Deepgram Connection closed");
    }

    public event EventHandler RecognizerStartedListening;
    public event EventHandler RecognizerStoppedListening;
    public event EventHandler RecognizerIsProcessing;
    public event EventHandler<string> RecognitionResult;
    public event EventHandler<RecognizerError> RecognizerException;

    private double _bytesPerSecond;  // used to do a rough calculation of the recording legnth

    public async Task StarListening(CultureInfo RecognitionCulture = null, int EndSilenceInMs = 1500, bool RestartAfterAutoStop = false)
    {
        _waveIn?.Dispose();
        _deepgramLive?.Dispose();
        _deepgramClient = null;
        Debug.WriteLine($"Startlistneing");
        var deviceEnum = new MMDeviceEnumerator();

        var device = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        Debug.WriteLine($"Using input device: {device.FriendlyName}");

        // just in case some other program has muted it
        device.AudioEndpointVolume.Mute = false;
        // Setup the audio capture of NAudio
        _waveIn = new WasapiCapture(device);
        Debug.WriteLine($"Waveformat from device");
        Debug.WriteLine($"   Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"   Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"   Bits per sample: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"   Channels: {_waveIn.WaveFormat.Channels}");

        _bytesPerSecond = _waveIn.WaveFormat.SampleRate * _waveIn.WaveFormat.BitsPerSample * _waveIn.WaveFormat.Channels / 8;
        Debug.WriteLine($"Bytes per second {_bytesPerSecond}");
        var waveOutFormat = new WaveFormat(_waveIn.WaveFormat.SampleRate / 3, 2);

        const string secret = "6ad54b90768a3c6f88af80f633682fae31b87201";
        var credentials = new Credentials(secret);

        _deepgramClient = new DeepgramClient(credentials);
        var options = new LiveTranscriptionOptions()
        {
            Punctuate = false,
            Diarize = false,
            Numerals = true,
            Encoding = Deepgram.Common.AudioEncoding.Linear16,
            Language = "en-US",
            //Utterances = true,
            InterimResults = true,
            SampleRate = 44100
        };
        _deepgramLive = _deepgramClient.CreateLiveTranscriptionClient();

        _waveIn.DataAvailable += OnDataAvailableConvert;
        _waveIn.RecordingStopped += OnRecordingStopped;

        _deepgramLive.ConnectionClosed += DeepgramLive_ConnectionClosed;
        _deepgramLive.TranscriptReceived += DeepgramLive_TranscriptReceived;
        _deepgramLive.ConnectionOpened += DeepgramLive_ConnectionOpened;
        _deepgramLive.ConnectionError += DeepgramLive_ConnectionError;

        _waveIn.StartRecording();

        await _deepgramLive.StartConnectionAsync(options);

    }

    public void StopListening()
    {
        _waveIn.StopRecording();
        _deepgramLive.FinishAsync();
        Debug.WriteLine($"Stop listening");

    }
}
