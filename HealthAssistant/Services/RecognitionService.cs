using Deepgram;
using Deepgram.Transcription;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HealthAssistant.Services
{
    internal class RecognitionService
    {
		public async Task StartListening()
        {
			var credentials = new Credentials("mykey");
			var deepgramClient = new DeepgramClient(credentials);
			using (var deepgramLive = deepgramClient.CreateLiveTranscriptionClient())
			{
				var options = new LiveTranscriptionOptions()
				{
					Punctuate = true,
					Diarize = true,
					Encoding = Deepgram.Common.AudioEncoding.Linear16
				};
				await deepgramLive.StartConnectionAsync(options);
			}
		}

	}
}
