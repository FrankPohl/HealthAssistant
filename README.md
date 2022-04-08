# Content
This repository contains a MAUI app that was developed in a Hackathon which was started to develop solutions which benefit from Deepgram's speech recognition service.
The user can put in health data like heart rate, temperature or weight in a kind of chat. The utterances of the user are translated into text from the Deepgram service, and the program extracts the relevant info from this text and stores it. In case some info is missing the app asks the user to put in that data.


## General Info
The app is a prototype and was developed in a short timeframe. Therefore you should not expect ready to roll-out code with outstanding code quality.

Nevertheless the app works and can be used to test the analysis approach and the behavior of the Deepgram API.
If you want to see it in action check out this [video on YouTube](https://youtu.be/3R08QHkPRLo)

## Technical details
### General
This is a MAUI app and at the tiem of this writing you need Visual Studio 2022 Preview to compile it. I must use MAUI because the Deepgram SDK is only available for .NET Core and a MAUI is used to implement a UI for this framework on different platforms.

### 3rd party controls
To connect the audio from the microphone I'm unsing NAudio. The input is directly send to the Deepgram live transcription API. The Deepgram .NET SDK is used for the direct communication with the API.
There is a key to access the Deepgram service inlcuded but it will no no longer work. You need a key from Deepgram to access the Deepgram API. To get this key you have to create a deepgram account. Don#t worry, a free option is available. Check out [Deepgram website](www.deepgram.com)

In addition you need the Deepgram .NET SDK package and the NAudio package.
They should be installed automatically as soon as you build the app.
If necessary  the packages with the package manager.
install-package Deepgram
install-package NAudio

NAudio is necessary to link the audio input stream with the deepgram service. This is very OS specific and therefore the app will not work on Android or iOS. 
If you want to use it on one of these platforms you have to implement SpeechRecognizer.cs for the target platform and put the implementations in the respective platform folders. 

### Analyze the input
The string that is returned from the Deepgram API is not analyzed by some fancy AI model. I do not have enough training data for such an approach. So I tried to extract the intent and the data with searching for keywords and Regular Expressions. The code for this is in TextEvaluation.cs. This apporach is also much easier to implement in different languages.

## License
The source code in this repository is published under the Apache-2 license license which you can find [here](LICENSE).