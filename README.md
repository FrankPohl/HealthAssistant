# Health Assistant
Using Deepgram SDk with direct speech input from Audio in a MAUI app.
It was intended to provide an app to put in health data in a natrual language dialog.

# Technical details
I'm using different Nuget packages. Mainly these
 - NAudio to get the audio from the microphone
 - Deepgram to connect to the deepgram service
 
 
# Problems
I do not get any answer from the deepgram SDK. I do not know whether something wsa processed or not.
Reopening the connection after close is alos not working as epxected but raises an error.