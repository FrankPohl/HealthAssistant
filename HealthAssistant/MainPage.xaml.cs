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
        Debug.WriteLine($"OnStopAudioClicked");

        if (_waveIn != null)
        {
            _waveIn.StopRecording();
        }
        try
        {
            if (_deepgramLive != null)
            {
                Debug.WriteLine($"DeepGram State {_deepgramLive.State()}");
                await _deepgramLive.FinishAsync();
                await _deepgramLive.StopConnectionAsync();
            }
        }
        catch (Exception)
        {

            Debug.WriteLine("_deepgramLive.FinishAsync crashed");
        }
    }

    private async void OnReOpenDeepgramConnectionClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnStartRecognitionAndSaveClicked");
        
        await _deepgramLive.StartConnectionAsync(_options);
    }


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
        Debug.WriteLine($"Waveformat: Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"            Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"            Bits: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"            Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"            Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        Debug.WriteLine($"            Bytes per Second: {_waveIn.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");

        _waveWriter = new WaveFileWriter($@"C:\Temp\TestUnconverted-{_waveIn.WaveFormat.SampleRate}-{_waveIn.WaveFormat.Encoding}-{_waveIn.WaveFormat.Channels}.wav", _waveIn.WaveFormat);
        _waveIn.StartRecording();
    }

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
        Debug.WriteLine($"Waveformat: Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"            Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"            Bits: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"            Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"            Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        Debug.WriteLine($"            Bytes per Second: {_waveIn.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");

        var waveOutFormat = new WaveFormat(sampleRate: 16000, channels: 1);

        _waveWriter = new WaveFileWriter($@"C:\Temp\TestUnconverted-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav", waveOutFormat);
        _waveIn.StartRecording();

    }

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
        Debug.WriteLine($"Waveformat: Samplerate: {_waveIn.WaveFormat.SampleRate}");
        Debug.WriteLine($"            Encoding: {_waveIn.WaveFormat.Encoding}");
        Debug.WriteLine($"            Bits: {_waveIn.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"            Channels: {_waveIn.WaveFormat.Channels}");
        Debug.WriteLine($"            Blockalign: {_waveIn.WaveFormat.BlockAlign}");
        Debug.WriteLine($"            Bytes per Second: {_waveIn.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");
        //var waveOutFormat = new WaveFormat(sampleRate: _waveIn.WaveFormat.SampleRate, channels: _waveIn.WaveFormat.Channels);
        // WaveFloatTo16Provider prov = new WaveFloatTo16Provider()

        var waveOutFormat = new WaveFormat(sampleRate: 16000, channels: 1);

        _waveWriter = new WaveFileWriter($@"C:\Temp\TestUnconverted-{waveOutFormat.SampleRate}-{waveOutFormat.Encoding}-{waveOutFormat.Channels}.wav", waveOutFormat);
        _waveIn.StartRecording();
    }

    private async void OnTranslateFileClicked(object sender, EventArgs e)
    {
        Debug.WriteLine($"OnTranslateFileClicked");

        var picker = await FilePicker.PickAsync();
        if (picker == null)
        {
            return;
        }
        var waveReader = new WaveFileReader(picker.FullPath);
        Debug.WriteLine($"Waveformat: Samplerate: {waveReader.WaveFormat.SampleRate}");
        Debug.WriteLine($"            Encoding: {waveReader.WaveFormat.Encoding}");
        Debug.WriteLine($"            Bits: {waveReader.WaveFormat.BitsPerSample}");
        Debug.WriteLine($"            Channels: {waveReader.WaveFormat.Channels}");
        Debug.WriteLine($"            Blockalign: {waveReader.WaveFormat.BlockAlign}");
        Debug.WriteLine($"            Bytes per Second: {waveReader.WaveFormat.AsStandardWaveFormat().AverageBytesPerSecond}");
        waveReader.Close();

        Debug.WriteLine($"Translate file {picker.FullPath} with deepgram");
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
            Encoding = Deepgram.Common.AudioEncoding.MuLaw,
            Channels = 1,
            SampleRate = 16000
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
    int iBufferCounter = 0;

    private void OnDataAvailable(object sender, WaveInEventArgs e)
    {

        Debug.WriteLine($"Storage package: {iBufferCounter} with length {e.Buffer.Length}");
        _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

        int secondsRecorded = (int)(_waveWriter.Length / _waveWriter.WaveFormat.AverageBytesPerSecond);
        if (secondsRecorded >= 60)
        {
            Debug.WriteLine("Stop automatically after 60 seconds ");
            _waveIn.StopRecording();
        }
    }
    private void OnDataAvailablecSend(object sender, WaveInEventArgs e)
        {
            iBufferCounter += 1;
        Debug.WriteLine($"Sending Data Package no: {iBufferCounter} with length {e.Buffer.Length}");
        //  Hav to transcode the data
        // Write to file to show that audio in worked
        // write tp deepgram
        byte[] sendBuffer = new byte[e.Buffer.Length];
        _deepgramLive.SendData(sendBuffer);
        Task.Delay(50).Wait();
    }
    private void OnDataAvailablecConvert(object sender, WaveInEventArgs e)
    {
        byte[] convertedbuffer = new byte[e.Buffer.Length];
        iBufferCounter += 1;
        Debug.WriteLine($"Receiving Data Package no: {iBufferCounter} with length {e.Buffer.Length}");

    }
    private void OnRecordginStopped(object sender, StoppedEventArgs e)
    {
        _waveWriter.Close();
        _waveWriter.Dispose();
        Debug.WriteLine("Audio In stopped");
        DispatchStatusMsg($"Audio In stopped");

    }
    #endregion

//    int sourceSamples = e.Buffer.Length / 4;
//    //WaveBuffer sourceWaveBuffer = new WaveBuffer(e.Buffer);
//    //WaveBuffer destWaveBuffer = new WaveBuffer(convertedbuffer);

//    // Code mainly taken from
//    // https://www.codeproject.com/articles/501521/how-to-convert-between-most-audio-formats-in-net
//    WaveBuffer n = new WaveBuffer(e.Buffer);
//    //short[] dest = n.ShortBuffer;


//    float[] floats;

//    //a variable to flag the mod 3-ness of the current sample
//    //we're mapping 48000 --> 16000, so we need to average 3 source
//    //samples to make 1 output sample
//    var arity = -1;
//    floats = n.FloatBuffer;
//        short[] convertedBuffer = new short[n.FloatBuffer.Length / 3];
//    var runningSamples = new short[3];
//    iBufferCounter = 0;
//        int fcount = 0;
//        while (fcount<n.FloatBuffer.Count())
//        {

//            //simple average to collapse 2 channels into 1
//            float mono = (float)((double)floats[fcount] + (double)floats[fcount]) / 2;
//    Debug.WriteLine($"Mon: {mono}");
//            //convert (-1, 1) range int to short
//            short sixteenbit = (short)(mono * 32767);

//    //the input is 48000Hz and the output is 16000Hz, so we need 1/3rd of the data points
//    //so save up 3 running samples and then mix and write to the file
//    arity = (arity + 1) % 3;

//            runningSamples[arity] = sixteenbit;

//            //on the third of 3 running samples
//            if (arity == 2)
//            {
//                //simple average of the 3 and put in the 0th position
//                runningSamples[0] = (short) (((int) runningSamples[0] + (int) runningSamples[1] + (int) runningSamples[2]) / 3);
//    Debug.WriteLine($"Sample in buffer: {runningSamples[0]}");

//                //write the one 16 bit short to the output

//                // writer.WriteData(runningSamples, 0, 1);
//                convertedBuffer[iBufferCounter] = runningSamples[0];
//                iBufferCounter++;
//            }
//fcount += 2;
//        }
//        Debug.WriteLine($" So vile im array {iBufferCounter}");
//_waveWriter.WriteData(convertedbuffer, 0, convertedbuffer.Length);
//        //_deepgramLive.SendData(convertedbuffer);

//        //int destOffset = 0;
//        //for (int sample = 0; sample < sourceSamples; sample++)
//        //{
//        //    // adjust volume
//        //    float sample32 = e.Buffer[sample] * 1.0f;
//        //    // clip
//        //    if (sample32 > 1.0f)
//        //        sample32 = 1.0f;
//        //    if (sample32 < -1.0f)
//        //        sample32 = -1.0f;
//        //    convertedbuffer[destOffset++] = (byte)(sample32 * 32767);
//        //}
//        //Debug.WriteLine($"Converted: {string.Join(", ", convertedbuffer)}");
//        //Debug.WriteLine($"Sending converted data {i} with length {convertedbuffer.Length}");
//        //_waveWriter.Write(convertedbuffer,0, convertedbuffer.Length);
//        //Debug.WriteLine($"Converted: {string.Join(", ", dest)}");
//        //Debug.WriteLine($"Sending converted data {i} with length {dest.Length}");
//        //_waveWriter.Write((dest, 0, dest.Length);

//        //for (int sample = 0; sample < sourceSamples; sample++)
//        //{
//        //    // adjust volume
//        //    // clip
//        //    if (sample32 > 1.0f)
//        //        sample32 = 1.0f;
//        //    if (sample32 < -1.0f)
//        //        sample32 = -1.0f;
//        //    destWaveBuffer[destOffset++] = (short)(sample32 * 32767);
//        //}

//        // Write to file to show that audio in worked
//        // I got this from an NAudio sample 
//        // _waveWriter.Write(e.Buffer, 0, e.BytesRecorded);

//        //int secondsRecorded = (int)(_waveWriter.Length / _waveWriter.WaveFormat.AverageBytesPerSecond);
//        //if (secondsRecorded >= 60)
//        //{
//        //    Debug.WriteLine("Stop automatically after 60 seconds ");
//        //    _waveIn.StopRecording();
//        //}

//        // write tp deepgram
//        //_deepgramLive.SendData(convertedbuffer);
//        //Task.Delay(50).Wait();


}

