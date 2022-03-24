using Deepgram;
using Deepgram.Transcription;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Deepgram.Logger;
using Serilog;
using System.Net.WebSockets;
using NAudio.Wave.SampleProviders;
using NAudio.Utils;
using NAudio.Codecs;

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
    /// <summary>
    /// Stop the recording and dispose all objects
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>

    private async void OnStopAudioClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnStopAudioClicked");

        if (_waveWriter != null)
        {
            _waveWriter.Close();
            _waveWriter.Dispose();
        }
        if (_waveIn != null)
        {
            _waveIn.StopRecording();
            _waveIn.Dispose();
        }
        // we do not stop deepgram but wait for it to finish on its own
        //try
        //{
        //    if (_deepgramLive != null)
        //    {
        //        Debug.WriteLine($"DeepGram State {_deepgramLive.State()}");
        //        await _deepgramLive.FinishAsync();
        //        await _deepgramLive.StopConnectionAsync();
        //        _deepgramLive.Dispose();
        //    }
        //}
        //catch (Exception)
        //{

        //    Debug.WriteLine("_deepgramLive.FinishAsync crashed");
        //}
    }

    private async void OnReOpenDeepgramConnectionClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnStartRecognitionAndSaveClicked");
        
        await _deepgramLive.StartConnectionAsync(_options);
    }
    /// <summary>
    /// Store audio input in a file without any conversion
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>

    private void OnStartRecognitionAndSaveClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnStartRecognitionAndSaveClicked");

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
        iBufferCounter = 0;
        Debug.WriteLine($"Waveformat (input and output the same");
        Debug.WriteLine($"   Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"   Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"   Bits: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"   Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"   Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        Debug.WriteLine($"   Bytes per Second: {_waveIn.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");

        _waveWriter = new WaveFileWriter($@"C:\Temp\TestUnconverted-{_waveIn.WaveFormat.SampleRate}-{_waveIn.WaveFormat.Encoding}-{_waveIn.WaveFormat.Channels}.wav", _waveIn.WaveFormat);
        _waveIn.StartRecording();
    }

    /// <summary>
    /// Record the audio from the Mic and store it in a file.
    /// The input is converted to 16 bit PCM and downsampled to 16000
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>

    private void OnStartAudioWithConversionClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnStartAudioWithConversionClicked");

        // Get the enumeration of the audio in devices
        var deviceEnum = new MMDeviceEnumerator();
        var devices = deviceEnum.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToList();

        var device = deviceEnum.GetDefaultAudioEndpoint(DataFlow.Capture, Role.Communications);
        Debug.WriteLine($"OnStartAudioWithConversionClicked");
        Debug.WriteLine($"Using input device: {device.FriendlyName}");
        DispatchStatusMsg($"Using input device: {device.FriendlyName}");
        // just in case some other program has muted it
        device.AudioEndpointVolume.Mute = false;

        // Setup the auio capture of NAudio
        iBufferCounter = 0;
        _waveIn = new WasapiCapture(device);
        _waveIn.DataAvailable += OnDataAvailablecConvert;
        _waveIn.RecordingStopped += OnRecordginStopped;
        Debug.WriteLine($"Waveformat from device");
        Debug.WriteLine($"   Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"   Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"   Bits per sample: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"   Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"   Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        Debug.WriteLine($"   Bytes per Second: {_waveIn.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");
        // then next waveOutFormat this call should work with ConvertArray4
        var waveOutFormat = new WaveFormat(sampleRate: _waveIn.WaveFormat.SampleRate / 3, channels: 2);
        // then next waveOutFormat works with ConvertArray2 (works with file) 
        // var waveOutFormat = new WaveFormat(sampleRate: _waveIn.WaveFormat.SampleRate, channels: 2);
        // the next call works with ConvertArray3 (works with file) 
        // _waveWriter = new WaveFileWriter($@"C:\Temp\TestConverted-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav", _waveIn.WaveFormat);
        _waveWriter = new WaveFileWriter($@"C:\Temp\TestConverted-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav", waveOutFormat);
        _waveIn.StartRecording();

    }

    /// <summary>
    /// Convert audio from a wav file and store the conversion in a  new file.
    /// This is an example I found on stackoverflow
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>

    private async void OnConvertFileClicked(object sender, EventArgs e)
    {
        var picker = await FilePicker.PickAsync();
        if (picker == null)
        {
            return;
        }
        Debug.WriteLine($"Convert {picker.FullPath}");
        var waveOutFormat = new WaveFormat(sampleRate: 16000, channels: 1);

        string convertedFileName = $@"{Path.GetDirectoryName(picker.FullPath)}\\ConvertedFile-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav";
        Debug.WriteLine($"Convert file {picker.FullPath} to {convertedFileName}");
        using (var reader = new NAudio.Wave.WaveFileReader(picker.FullPath))
        using (var writer = new NAudio.Wave.WaveFileWriter(convertedFileName, waveOutFormat))
        {
            DispatchStatusMsg($"Convert file {picker.FullPath}  to {convertedFileName}");
            Debug.WriteLine($"Reader hat eine Länge von {reader.Length}");
            float[] floats;
            //a variable to flag the mod 3-ness of the current sample
            //we're mapping 48000 --> 16000, so we need to average 3 source
            //samples to make 1 output sample
            var arity = -1;

            int i = 0;
            int j = 0;
            var runningSamples = new short[3];
            while ((floats = reader.ReadNextSampleFrame()) != null)
            {
                //simple average to collapse 2 channels into 1
                float mono = (float)((double)floats[0] + (double)floats[1]) / 2;

                //convert (-1, 1) range int to short
                short sixteenbit = (short)(mono * 32767);

                //the input is 48000Hz and the output is 16000Hz, so we need 1/3rd of the data points
                //so save up 3 running samples and then mix and write to the file
                arity = (arity + 1) % 3;

                runningSamples[arity] = sixteenbit;

                //on the third of 3 running samples
                if (arity == 2)
                {
                    //simple average of the 3 and put in the 0th position
                    runningSamples[0] = (short)(((int)runningSamples[0] + (int)runningSamples[1] + (int)runningSamples[2]) / 3);

                    //write the one 16 bit short to the output
                    writer.WriteData(runningSamples, 0, 1);
                    j++;
                }
                i++;
            }
            Debug.WriteLine($"ReadNextSampleFrame for {i} times and {j} written with {writer.Length} bytes");
        }

    }

    private void OnStartAudioWithDeepgramClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnStartAudioWithDeepgramClicked");

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
        _waveIn.DataAvailable += OnDataAvailablecSend;
        _waveIn.RecordingStopped += OnRecordginStopped;
        iBufferCounter = 0;
        Debug.WriteLine($"Wave format from device");
        Debug.WriteLine($"    Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"    Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"    Bits: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"   Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"   Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        Debug.WriteLine($"   Bytes per Second: {_waveIn.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");
        //var waveOutFormat = new WaveFormat(sampleRate: _waveIn.WaveFormat.SampleRate, channels: _waveIn.WaveFormat.Channels);
        // WaveFloatTo16Provider prov = new WaveFloatTo16Provider()

        var waveOutFormat = new WaveFormat(sampleRate: 16000, channels: 1);

        _waveWriter = new WaveFileWriter($@"C:\Temp\TestUnconverted-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav", waveOutFormat);
        _waveIn.StartRecording();
    }

    /// <summary>
    /// Transalte the content of a file with deepgram
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>

    private async void OnTranslateFileClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnTranslateFileClicked");

        var picker = await FilePicker.PickAsync();
        if (picker == null)
        {
            return;
        }
        Debug.WriteLine($"Translate file {picker.FullPath} with deepgram");
        DispatchStatusMsg($"Translate file {picker.FullPath} with deepgram");
        var waveReader = new WaveFileReader(picker.FullPath);
        Debug.WriteLine($"Input file Wave format");
        Debug.WriteLine($"    Samplerate: {waveReader.WaveFormat.SampleRate}");
        Debug.WriteLine($"    Encoding: {waveReader.WaveFormat.Encoding}");
        Debug.WriteLine($"    Bits: {waveReader.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"    Channels: {waveReader.WaveFormat.Channels}");
        Debug.WriteLine($"    Blockalign: {waveReader.WaveFormat.BlockAlign}");
        Debug.WriteLine($"    Bytes per Second: {waveReader.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");
        waveReader.Close();

        var credentials = new Credentials("9bc2b8137471bdc642b5cb4b7c29c6e960462a2e");
        // credentials.ApiUrl = @"https://api.deepgram.com";
        var deepgramClient = new DeepgramClient(credentials);
        var options = new PrerecordedTranscriptionOptions()
        {
            Utterances = true,
            Punctuate = false,
        };
        var response = await deepgramClient.Transcription.Prerecorded.GetTranscriptionAsync(new UrlSource(picker.FullPath), options);
    }


    /// <summary>
    /// Initialize the deepgram client. Must be called before using deepgram
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>

    private async void OnInitDeepgramClicked(object sender, EventArgs e)
	{
        Debug.WriteLine($"OnInitDeepgramClicked");
        var credentials = new Credentials("9bc2b8137471bdc642b5cb4b7c29c6e960462a2e");
        credentials.ApiUrl = @"https://api.deepgram.com";
        var deepgramClient = new DeepgramClient(credentials);
        _options = new LiveTranscriptionOptions()
        {
            Punctuate = true,
            Diarize = true,
            Encoding = Deepgram.Common.AudioEncoding.Linear16,
            MultiChannel = true,
            Utterances = true,
            Language = "en-US",
            Channels = 2,
            SampleRate = 16000
        };
        _deepgramLive = deepgramClient.CreateLiveTranscriptionClient();
        _deepgramLive.ConnectionClosed += DeepgramLive_ConnectionClosed;
        _deepgramLive.TranscriptReceived += DeepgramLive_TranscriptReceived;
        _deepgramLive.ConnectionOpened += DeepgramLive_ConnectionOpened;
        _deepgramLive.ConnectionError += DeepgramLive_ConnectionError;
        await _deepgramLive.StartConnectionAsync(_options);
    }

    /// <summary>
    /// Convert audio from a wav file and store the conversio in a  new file
    /// </summary>
    /// <param name="sender">Not used</param>
    /// <param name="e">Not used</param>
    private async void OnConvertMyFileClicked(object sender, EventArgs e)
    {
        var picker = await FilePicker.PickAsync();
        if (picker == null)
        {
            return;
        }
        Debug.WriteLine($"Convert {picker.FullPath}");

        byte[] buffer = new byte[384000];
        var reader = new NAudio.Wave.WaveFileReader(picker.FullPath);
        _waveIn = new WasapiCapture();
        _waveIn.WaveFormat = reader.WaveFormat;
        Debug.WriteLine($"Input file Wave format");
        Debug.WriteLine($"    Samplerate: {reader.WaveFormat.SampleRate}");
        Debug.WriteLine($"    Encoding: {reader.WaveFormat.Encoding}");
        Debug.WriteLine($"    Bits: {reader.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"    Channels: {reader.WaveFormat.Channels}");
        Debug.WriteLine($"    Blockalign: {reader.WaveFormat.BlockAlign}");
        Debug.WriteLine($"    Bytes per Second: {reader.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");
        // this waveout works with ConvertArray2
        //var waveOutFormat = new WaveFormat(sampleRate: reader.WaveFormat.SampleRate, channels: 2);
        // this waveout works with ConvertArray4
        var waveOutFormat = new WaveFormat(sampleRate: reader.WaveFormat.SampleRate /3, channels: 2);
        string convertedFileName = $@"{Path.GetDirectoryName(picker.FullPath)}\\MyConversion-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav";
        var writer = new NAudio.Wave.WaveFileWriter(convertedFileName, waveOutFormat);
        Debug.WriteLine($"Outout file Wave format");
        Debug.WriteLine($"    Samplerate: {writer.WaveFormat.SampleRate}");
        Debug.WriteLine($"    Encoding: {writer.WaveFormat.Encoding}");
        Debug.WriteLine($"    Bits: {writer.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"    Channels: {writer.WaveFormat.Channels}");
        Debug.WriteLine($"    Blockalign: {writer.WaveFormat.BlockAlign}");
        Debug.WriteLine($"    Bytes per Second: {writer.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");

        int i = 1;
        int bytes = 1;
        while (bytes > 0)
        {
            bytes = reader.Read(buffer, 0, buffer.Length);
            Debug.WriteLine($"Buffers converted {i}");
            var convertedBuffer = ConvertArray4(buffer, bytes);
            writer.Write(convertedBuffer, 0, convertedBuffer.Length);
            i++;
        }
        reader.Close();
        reader.Dispose();
        writer.Close();
        writer.Dispose();

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
        Debug.WriteLine($"Alternatives First: {e.Transcript.Channel.Alternatives.First().Transcript}");
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

    /// <summary>
    /// Convert an array 
    /// </summary>
    /// <param name="Input"></param>
    /// <param name="NoOfBytes"></param>
    /// <returns></returns>
    private byte[] ConvertArray2(byte[] Input, int NoOfBytes)
    {
        Debug.WriteLine($"Called ConvertArray2 auf PCM");
        byte[] output = new byte[NoOfBytes / 2];
        WaveBuffer sourceWaveBuffer = new WaveBuffer(Input);
        WaveBuffer destWaveBuffer = new WaveBuffer(output);
        for (int n = 0; n < NoOfBytes / 4; n += 1)
        {
            float sample32 = sourceWaveBuffer.FloatBuffer[n];
            if (sample32 > 1.0f)
                sample32 = 1.0f;
            if (sample32 < -1.0f)
                sample32 = -1.0f;
            destWaveBuffer.ShortBuffer[n] = (short)(sample32 * 32767);
        }
        return output;
    }


    private byte[] ConvertArray4(byte[] Input, int NoOfBytes)
    {
        Debug.WriteLine($"Called ConvertArray4 (Resample by 1/3 and to PCM");
        byte[] output = new byte[NoOfBytes / 2];
        byte[] resampledOutput = new byte[NoOfBytes / 6];
        WaveBuffer sourceWaveBuffer = new WaveBuffer(Input);
        WaveBuffer outputWaveBuffer = new WaveBuffer(output);
        WaveBuffer resampledWaveBuffer = new WaveBuffer(resampledOutput);
        int resampledValueCounter = 0;
        int outputCounter = 0;
        var resampleSourceSamples = new short[3];
        for (int n = 0; n < NoOfBytes / 4; n += 1)
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
        Debug.WriteLine($"Created out with {outputCounter} values from {NoOfBytes} input bytes");
        return resampledOutput;
    }

    
    private byte[] ConvertArray3(byte[] Input, int NoOfBytes)
    {
        Debug.WriteLine($"Called ConvertArray3, direct copy");
        byte[] output = new byte[NoOfBytes];

        for (int n = 0; n < NoOfBytes; n += 1)
        {
            output[n] = Input[n];
        }
        return output;
    }

    int iBufferCounter = 0;


    /// <summary>
    /// OnDataAvailable Event handling with conversion to PCNM and resampling from 48000 to 16000
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Audio data in a byte buffer</param>
    private void OnDataAvailablecConvert(object sender, WaveInEventArgs e)
    {
        DispatchStatusMsg($"OnDataAvailable for audio with conversion");
        var convertedBuffer = ConvertArray4(e.Buffer, e.BytesRecorded);
        _waveWriter.Write(convertedBuffer, 0, convertedBuffer.Length);
    }

    /// <summary>
    /// OnDataAvailable Event handling with any conversion
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Audio data in a byte buffer</param>
    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {

        DispatchStatusMsg($"OnDataAvailable for audio with conversion");
        Debug.WriteLine($"OnDataAvailable for audio input without any conversion");
        _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

        int secondsRecorded = (int)(_waveWriter.Length / _waveWriter.WaveFormat.AverageBytesPerSecond);
        if (secondsRecorded >= 60)
        {
            Debug.WriteLine("Stop automatically after 60 seconds ");
            _waveIn.StopRecording();
        }
    }

    /// <summary>
    /// OnDataAvailable Event handling with conversion and sending to deepgram
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e">Audio data in a byte buffer</param>
    private void OnDataAvailablecSend(object sender, WaveInEventArgs e)
    {
        if (_deepgramLive == null)
        {
            DispatchStatusMsg($"Initlaize deepgrma first");
            return;
        }
        iBufferCounter += 1;
        Debug.WriteLine($"Sending Data Package no: {iBufferCounter} with length {e.BytesRecorded}");
        DispatchStatusMsg($"OnDataAvailable for audio with conversion and send to deepgram");
        var convertedBuffer = ConvertArray4(e.Buffer, e.BytesRecorded);
        _waveWriter.Write(convertedBuffer, 0, convertedBuffer.Length);
        byte[] sendBuffer = new byte[e.Buffer.Length];
        _deepgramLive.SendData(sendBuffer);
        Task.Delay(50).Wait();
    }
    private void OnRecordginStopped(object sender, StoppedEventArgs e)
    {
        _waveWriter.Close();
        _waveWriter.Dispose();
        Debug.WriteLine("Audio In stopped");
        DispatchStatusMsg($"Audio In stopped");
    }

    #endregion

    }

