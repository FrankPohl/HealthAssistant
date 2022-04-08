using System.Globalization;

namespace HealthAssistant.Services
{
    public enum RecognizerError
    {
        SoundRecognizedButNoText,
        NoNetwork,
        AudioLow
    }

    public interface ISpeechRecognizer
    {
        /// <summary>
        /// Starts the listener which translates audio to text
        /// Check for Micorphone access before calling this
        /// </summary>
        /// <param name="RecognitionCulture">Optional culture that should be used for the recognizer</param>
        /// <param name="EndSilenceInMs">Optional silence in audio thta signals the end of the input. After this period of silence the RecognitionResult event is thrown</param>
        /// <param name="RestartAfterAutoStop">If set to true this will restart infinetly. This is necessary because some Online recognizers limit the connection to save bandwidth</param>
        Task StarListening(CultureInfo RecognitionCulture = null, int EndSilenceInMs = 1500, bool RestartAfterAutoStop = false);

        /// <summary>
        /// Stop the listener
        /// </summary>
        void StopListening();

        /// <summary>
        /// The listener is ready to work
        /// At least once after Initialization in StartListening is finished
        /// </summary>
        event EventHandler RecognizerStartedListening;

        ///// <summary>
        ///// The listener has stopped. Nothing will be recognized any longer.
        ///// </summary>
        event EventHandler RecognizerStoppedListening;

        ///// <summary>
        ///// Audio input is porocessed.
        ///// </summary>
        event EventHandler RecognizerIsProcessing;

        ///// <summary>
        ///// After text is recognized
        ///// </summary>
        event EventHandler<String> RecognitionResult;

        ///// <summary>
        ///// In case an error occured this event is thrown.
        /////
        ///// </summary>
        event EventHandler<RecognizerError> RecognizerException;

        ///// <summary>
        ///// Ask for permission to sue the microphone
        ///// </summary>
        ///// <returns></returns>
        //Task CheckAndRequestMicPermission();

        ///// <summary>
        ///// Does the device contain a microphone.
        ///// If there is no microphone it is not necessary to ask for permission to use it
        ///// </summary>
        //bool DeviceHasMicrophone { get; }

        ///// <summary>
        ///// Set culture that is used by the recognizer
        ///// If paramneter RecognitionCulture is not given in the StartListening call it is the default culture
        ///// </summary>
        //CultureInfo RecognitionCulture { get; }
    }
}