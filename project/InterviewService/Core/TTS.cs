using System;
using System.IO;
using System.Media;
using System.Threading;

namespace SpeechToTextWPF
{
    class TTS
    {
        Synthesize cortana;

        string requestUri;

        string accessToken;

        /// <summary>
        /// This method is called once the audio returned from the service.
        /// It will then attempt to play that audio file.
        /// Note that the playback will fail if the output audio format is not pcm encoded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="args">The <see cref="GenericEventArgs{Stream}"/> instance containing the event data.</param>
        private void PlayAudio(object sender, GenericEventArgs<Stream> args)
        {
            Console.WriteLine(args.EventData);

            // For SoundPlayer to be able to play the wav file, it has to be encoded in PCM.
            // Use output audio format AudioOutputFormat.Riff16Khz16BitMonoPcm to do that.
            SoundPlayer player = new SoundPlayer(args.EventData);
            player.PlaySync();
            args.EventData.Dispose();
        }

        /// <summary>
        /// Handler an error when a TTS request failed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="GenericEventArgs{Exception}"/> instance containing the event data.</param>
        private void ErrorHandler(object sender, GenericEventArgs<Exception> e)
        {
            Console.WriteLine("ERROR: Unable to complete the TTS request: [{0}]", e.ToString());
        }

        public void Start()
        {
            Console.WriteLine("Starting Authtentication");

            // Note: The way to get api key:
            // Free: https://www.microsoft.com/cognitive-services/en-us/subscriptions?productId=/products/Bing.Speech.Preview
            // Paid: https://portal.azure.com/#create/Microsoft.CognitiveServices/apitype/Bing.Speech/pricingtier/S0
            Authentication auth = new Authentication("3b045e01b45946c5ad7afee11ec469a7");

            try
            {
                accessToken = auth.GetAccessToken();
                Console.WriteLine("Token: {0}\n", accessToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed authentication.");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
                return;
            }

            Console.WriteLine("Starting request code execution.");

            requestUri = "https://speech.platform.bing.com/synthesize";

            cortana = new Synthesize();

            cortana.OnAudioAvailable += PlayAudio;
            cortana.OnError          += ErrorHandler;

        }

        public void Speak(string text)
        {
            if (text.Length == 0)
            {
                text = "Hello there. How are you doing?";
            }
            // Reuse Synthesize object to minimize latency
            cortana.Speak(
                CancellationToken.None, 
                new Synthesize.InputOptions()
                    {
                        RequestUri = new Uri(requestUri),
                        // Text to be spoken.
                        Text = text,
                        VoiceType = Gender.Female,
                        // Refer to the documentation for complete list of supported locales.
                        Locale = "en-US",
                        // You can also customize the output voice. Refer to the documentation to view the different
                        // voices that the TTS service can output.
                        VoiceName = "Microsoft Server Speech Text to Speech Voice (en-US, ZiraRUS)",
                        // Service can return audio in different output format.
                        OutputFormat = AudioOutputFormat.Riff16Khz16BitMonoPcm,
                        AuthorizationToken = "Bearer " + accessToken,
                    }
            ).Wait();
        }
    }
}
