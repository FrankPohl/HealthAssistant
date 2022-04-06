# Health Assistant
A MAUI App to enter health data with speech input. The speech input is transformed into processable text with the DeepGram services.

## General Info
Using DeepGram as the speech recognition in a MAUI Windows app.

## Technical details
To connect the audio from the microphone I'm unsing NAudio. The input is deirctly sen dto the DeepGram service. This is doen with the Deepgram .NET SDK.

### Why Deepgram
Very fast and low latency
Easy to switch recognition languages


### And why not
Bandwith (I have to take care of not sending silence data packages to DeepGram on my own 
