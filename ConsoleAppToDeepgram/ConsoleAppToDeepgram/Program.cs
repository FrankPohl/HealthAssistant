using NAudio.CoreAudioApi;
using NAudio.Wave;
using Deepgram;
using Deepgram.Transcription;

// Get the enumeration of the audio in devices
var deviceEnum = new MMDeviceEnumerator();
var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

var device = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
Console.WriteLine($"Using input device: {device.FriendlyName}");

// just in case some other program has muted it
device.AudioEndpointVolume.Mute = false;

WaveFileWriter _waveWriter;
IWaveIn _waveIn;

// Setup the auio capture of NAudio
_waveIn = new WasapiCapture(device);
Console.WriteLine($"Waveformat from device");
Console.WriteLine($"   Samplerate: {_waveIn.WaveFormat.SampleRate}");
Console.WriteLine($"   Encoding: {_waveIn.WaveFormat.Encoding}");
Console.WriteLine($"   Bits per sample: {_waveIn.WaveFormat.BitsPerSample}");
Console.WriteLine($"   Channels: {_waveIn.WaveFormat.Channels}");

var waveOutFormat = new WaveFormat(_waveIn.WaveFormat.SampleRate / 3, 2);

_waveWriter = new WaveFileWriter($@"C:\Temp\TestConverted.wav", waveOutFormat);

const string secret = "6ad54b90768a3c6f88af80f633682fae31b87201";
var credentials = new Credentials(secret);

var deepgramClient = new DeepgramClient(credentials);
var options = new LiveTranscriptionOptions()
{
    Punctuate = true,
    Diarize = true,
    Encoding = Deepgram.Common.AudioEncoding.Linear16,
    Language = "en-US",
    Utterances = true,
    SampleRate = 44100
};
var _deepgramLive = deepgramClient.CreateLiveTranscriptionClient();

_waveIn.DataAvailable += OnDataAvailableConvert;
_waveIn.RecordingStopped += OnRecordingStopped;

_deepgramLive.ConnectionClosed += DeepgramLive_ConnectionClosed;
_deepgramLive.TranscriptReceived += DeepgramLive_TranscriptReceived;
_deepgramLive.ConnectionOpened += DeepgramLive_ConnectionOpened;
_deepgramLive.ConnectionError += DeepgramLive_ConnectionError;

_waveIn.StartRecording();

await _deepgramLive.StartConnectionAsync(options);

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

void OnDataAvailableConvert(object sender, WaveInEventArgs e)
{
    var convertedBuffer = Convert32BitTo16Bit(e.Buffer, e.BytesRecorded);

    _deepgramLive.SendData(convertedBuffer);

    _waveWriter.Write(convertedBuffer, 0, convertedBuffer.Length);

    int secondsRecorded = (int)(_waveWriter.Length / _waveWriter.WaveFormat.AverageBytesPerSecond);
    if (secondsRecorded >= 30)
    {
        Console.WriteLine("Stop automatically after 30 seconds ");
        _waveIn.StopRecording();
    }
}

void OnRecordingStopped(object sender, StoppedEventArgs e)
{
    _waveWriter.Close();
    _waveWriter.Dispose();

    var wv = new WaveFileReader($@"C:\Temp\TestConverted.wav");
    Console.WriteLine($"Waveformat from file");
    Console.WriteLine($"   Samplerate: {wv.WaveFormat.SampleRate}");
    Console.WriteLine($"   Encoding: {wv.WaveFormat.Encoding}");
    Console.WriteLine($"   Bits per sample: {wv.WaveFormat.BitsPerSample}");
    Console.WriteLine($"   Channels: {wv.WaveFormat.Channels}");
    wv.Close();
    wv.Dispose();

}


void DeepgramLive_ConnectionError(object sender, ConnectionErrorEventArgs e)
{
    Console.WriteLine($"Deepgram Error: {e.Exception.Message}");
}

void DeepgramLive_ConnectionOpened(object sender, ConnectionOpenEventArgs e)
{
    Console.WriteLine("Deepgram Connection opened");
}

void DeepgramLive_TranscriptReceived(object sender, TranscriptReceivedEventArgs e)
{
    Console.WriteLine("Transcript received");
    {
        if (e.Transcript.IsFinal &&
            e.Transcript.Channel.Alternatives.First().Transcript.Length > 0)
        {
            var transcript = e.Transcript;
            Console.WriteLine($"Deepgram Recognition: {transcript.Channel.Alternatives.First().Transcript}");
        }
    }
}

void DeepgramLive_ConnectionClosed(object sender, ConnectionClosedEventArgs e)
{
    Console.WriteLine("Deepgram Connection closed");
}