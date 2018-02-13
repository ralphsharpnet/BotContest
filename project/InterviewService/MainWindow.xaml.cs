using SpeechToTextWPF;

namespace Microsoft.CognitiveServices.SpeechRecognition
{
    using SpeechToTextWPF.Core;
    using System;
    using System.ComponentModel;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.IO.IsolatedStorage;
    using System.Runtime.CompilerServices;
    using System.Windows;
    using System.Windows.Interop;
    using System.Windows.Threading;

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// USBCamera instance;
        /// </summary>
        private USBCamera pcc;

        /// <summary>
        /// The microphone client
        /// </summary>
        private MicrophoneRecognitionClient micClient;

        /// <summary>
        /// Get the questions to hear 
        /// </summary>
        private QuestionsHandler autoQuiz;

        /// <summary>
        /// Interview object where to store the questions and answers
        /// </summary>
        private Interview interview;

        /// <summary>
        /// Pair structure to set question/answer
        /// </summary>
        private Quiz pair;

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            this.Initialize();
            autoQuiz = new QuestionsHandler();
        }

        /// <summary>
        /// Gets the LUIS endpoint URL.
        /// </summary>
        /// <value>
        /// The LUIS endpoint URL.
        /// </value>
        private string LuisEndpointUrl
        {
            get { return ConfigurationManager.AppSettings["LuisEndpointUrl"]; }
        }

        /// <summary>
        /// Gets the current speech recognition mode.
        /// </summary>
        /// <value>
        /// The speech recognition mode.
        /// </value>
        private SpeechRecognitionMode Mode
        {
            get
            {
                return SpeechRecognitionMode.LongDictation;
            }
        }

        /// <summary>
        /// Gets the default locale.
        /// </summary>
        /// <value>
        /// The default locale.
        /// </value>
        private string DefaultLocale
        {
            get { return "en-US"; }
        }

        /// <summary>
        /// Gets the Cognitive Service Authentication Uri.
        /// </summary>
        /// <value>
        /// The Cognitive Service Authentication Uri.  Empty if the global default is to be used.
        /// </value>
        private string AuthenticationUri
        {
            get
            {
                return ConfigurationManager.AppSettings["AuthenticationUri"];
            }
        }

        /// <summary>
        /// Raises the System.Windows.Window.Closed event.
        /// </summary>
        /// <param name="e">An System.EventArgs that contains the event data.</param>
        protected override void OnClosed(EventArgs e)
        {
            if (null != this.micClient)
            {
                this.micClient.Dispose();
            }

            base.OnClosed(e);
        }

        /// <summary>
        /// Initializes a fresh audio session.
        /// </summary>
        private void Initialize()
        {
            this.SubscriptionKey = "5449424db0dd4f0aa9941ad77193673b";
        }

        private void saveJsonInterview()
        {
            interview.endTime = DateTime.Now.ToString("yyyy, MM, dd, hh, mm, ss");

            string json = new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(interview);

            File.WriteAllText(Environment.CurrentDirectory + @"\interview_" + System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".json", json);

        }

        /// <summary>
        /// Handles the Click event of the _startButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            this._startButton.IsEnabled = false;
            this._stopButton.IsEnabled = true;

            this.LogRecognitionStart();

            if (this.micClient == null)
            {
                this.CreateMicrophoneRecoClient();
            }

            pair = new Quiz();
            interview = new Interview();

            interview.startTime = DateTime.Now.ToString("yyyy, MM, dd, hh, mm, ss");
            pair.question = autoQuiz.Next(ref this.micClient, true);

            if (useCamera.IsChecked ?? false)
            {
                pcc.capture(
                    Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
                    + "\\wpf_" 
                    + System.DateTime.Now.ToString("yyyyMMddHHmmss") 
                    + ".avi"
                );
            }
        }

        /// <summary>
        /// Handles the Click event of the _startButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (useCamera.IsChecked ?? false)
            {
                Application.Current.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));
                pcc.StopCapture();
            }

            this._startButton.IsEnabled = true;
            this._stopButton.IsEnabled = false;

            if (this.micClient == null)
            {
                this.saveJsonInterview();

                this.micClient.EndMicAndRecognition();
                this.micClient.Dispose();
                this.micClient = null;
            }

            autoQuiz.clean();
        }

        /// <summary>
        /// Logs the recognition start.
        /// </summary>
        private void LogRecognitionStart()
        {
            this.WriteLine("\n--- Start speech recognition using microfone with " + this.Mode + " mode in " + this.DefaultLocale + " language ----\n\n");
        }

        /// <summary>
        /// Creates a new microphone reco client without LUIS intent support.
        /// </summary>
        private void CreateMicrophoneRecoClient()
        {
            this.micClient = SpeechRecognitionServiceFactory.CreateMicrophoneClient(
                this.Mode,
                this.DefaultLocale,
                this.SubscriptionKey);
            this.micClient.AuthenticationUri = this.AuthenticationUri;

            // Event handlers for speech recognition results
            this.micClient.OnMicrophoneStatus += this.OnMicrophoneStatus;
            this.micClient.OnPartialResponseReceived += this.OnPartialResponseReceivedHandler;
            this.micClient.OnResponseReceived += this.OnMicDictationResponseReceivedHandler;
            this.micClient.OnConversationError += this.OnConversationErrorHandler;
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnDataShortPhraseResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            Dispatcher.Invoke((Action)(() =>
            {
                this.WriteLine("--- OnDataShortPhraseResponseReceivedHandler ---");

                // we got the final result, so it we can end the mic reco.  No need to do this
                // for dataReco, since we already called endAudio() on it as soon as we were done
                // sending all the data.
                this.WriteResponseResult(e);

                _startButton.IsEnabled = true;
                _stopButton.IsEnabled = false;
            }));
        }

        /// <summary>
        /// Writes the response result.
        /// </summary>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void WriteResponseResult(SpeechResponseEventArgs e)
        {
            if (e.PhraseResponse.Results.Length == 0)
            {
                this.WriteLine("No phrase response is available.");
            }
            else
            {
                
                this.WriteLine("********* Final n-BEST Results *********");

                for (int i = 0; i < e.PhraseResponse.Results.Length; i++)
                {
                    this.WriteLine(
                        "[{0}] Confidence={1}, Text=\"{2}\"", 
                        i, 
                        e.PhraseResponse.Results[i].Confidence,
                        e.PhraseResponse.Results[i].DisplayText);

                    pair.answer = e.PhraseResponse.Results[i].DisplayText;
                }

                this.WriteLine(pair.question + " --> " + pair.answer);
                interview.quiz.Add(pair);
                pair = new Quiz();
                
                this.WriteLine("********* SHOULD record here (confidence High)*********");

                pair.question = autoQuiz.Next(ref this.micClient);

                this.WriteLine("************ Get along ************");
            }
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnMicDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.WriteLine("--- OnMicDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation)
                {
                    this.WriteLine("************ END of answering ************");
                }

                if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
                {
                    this.WriteLine("************ answering TIMEOUT ************");
                }

                Dispatcher.Invoke(
                    (Action)(() => 
                    {
                        // We got the final result, so it we can end the mic reco.  No need to do this
                        // for dataReco, since we already called endAudio() on it as soon as we were done
                        // sending all the data.

                        if (autoQuiz.isOver())
                        {
                            this.saveJsonInterview();

                            this.micClient.EndMicAndRecognition();
                            this._startButton.IsEnabled = true;
                            this._stopButton.IsEnabled = false;
                            autoQuiz.clean();

                            if (useCamera.IsChecked ?? false)
                            {
                                pcc.StopCapture();
                            }
                        }
                    }));                
            }

            this.WriteResponseResult(e);
        }

        /// <summary>
        /// Called when a final response is received;
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnDataDictationResponseReceivedHandler(object sender, SpeechResponseEventArgs e)
        {
            this.WriteLine("--- OnDataDictationResponseReceivedHandler ---");
            if (e.PhraseResponse.RecognitionStatus == RecognitionStatus.EndOfDictation ||
                e.PhraseResponse.RecognitionStatus == RecognitionStatus.DictationEndSilenceTimeout)
            {
                Dispatcher.Invoke(
                    (Action)(() => 
                    {
                        _startButton.IsEnabled = true;
                        _stopButton.IsEnabled = false;

                        // we got the final result, so it we can end the mic reco.  No need to do this
                        // for dataReco, since we already called endAudio() on it as soon as we were done
                        // sending all the data.
                    }));
            }

            this.WriteResponseResult(e);
        }

        /// <summary>
        /// Called when a final response is received and its intent is parsed
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechIntentEventArgs"/> instance containing the event data.</param>
        private void OnIntentHandler(object sender, SpeechIntentEventArgs e)
        {
            this.WriteLine("--- Intent received by OnIntentHandler() ---");
            this.WriteLine("{0}", e.Payload);
            this.WriteLine();
        }

        /// <summary>
        /// Called when a partial response is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="PartialSpeechResponseEventArgs"/> instance containing the event data.</param>
        private void OnPartialResponseReceivedHandler(object sender, PartialSpeechResponseEventArgs e)
        {
            this.WriteLine("--- Partial result received by OnPartialResponseReceivedHandler() ---");
            this.WriteLine("{0}", e.PartialResult);
            this.WriteLine();
        }

        /// <summary>
        /// Called when an error is received.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SpeechErrorEventArgs"/> instance containing the event data.</param>
        private void OnConversationErrorHandler(object sender, SpeechErrorEventArgs e)
        {
           Dispatcher.Invoke(() =>
           {
               _startButton.IsEnabled = true;
               _stopButton.IsEnabled = false;
           });

            this.WriteLine("--- Error received by OnConversationErrorHandler() ---");
            this.WriteLine("Error code: {0}", e.SpeechErrorCode.ToString());
            this.WriteLine("Error text: {0}", e.SpeechErrorText);
            this.WriteLine();
        }

        /// <summary>
        /// Called when the microphone status has changed.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="MicrophoneEventArgs"/> instance containing the event data.</param>
        private void OnMicrophoneStatus(object sender, MicrophoneEventArgs e)
        {
            Dispatcher.Invoke(() =>
            {
                WriteLine("--- Microphone status change received by OnMicrophoneStatus() ---");
                WriteLine("********* Microphone status: {0} *********", e.Recording);
                if (e.Recording)
                {
                    WriteLine("Please start speaking.");
                }

                WriteLine();
            });
        }

        //TODO: Move logs to another class.......

        /// <summary>
        /// Writes the line.
        /// </summary>
        private void WriteLine()
        {
            this.WriteLine(string.Empty);
        }

        /// <summary>
        /// Writes the line.
        /// </summary>
        /// <param name="format">The format.</param>
        /// <param name="args">The arguments.</param>
        private void WriteLine(string format, params object[] args)
        {
            var formattedStr = string.Format(format, args);
            Trace.WriteLine(formattedStr);
            Dispatcher.Invoke(() =>
            {
                _logText.Text += (formattedStr + "\n");
                _logText.ScrollToEnd();
            });
        }

        /// <summary>
        /// Contains the bearer key to certificate the service API.
        /// </summary>
        public string SubscriptionKey { get; set; }

        /// <summary>
        /// Loaded event for the Window
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">events</param>
        private void _mainWindow_loaded(object sender, RoutedEventArgs e)
        {
            HwndSource controlHwnd = HwndSource.FromVisual(cameraPanel) as HwndSource;

            pcc = new USBCamera(
                controlHwnd.Handle,
                (int)(cameraPanel.Margin.Left + 22),
                (int)(cameraPanel.Margin.Top + 42),
                (int)cameraPanel.ActualWidth,
                (int)cameraPanel.ActualHeight
            );

            if (useCamera.IsChecked ?? false)
            {
                pcc.Start();
            }
        }

        /// <summary>
        /// Event to check when the main window is been closed
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">events</param>
        private void _mainWindow_Closed(object sender, EventArgs e)
        {
            if (useCamera.IsChecked ?? false)
            {
                pcc.Stop();
            }
        }

        /// <summary>
        /// To check if the user wants the camera  ON or OFF
        /// </summary>
        /// <param name="sender">the sender</param>
        /// <param name="e">events</param>
        private void useCamera_Checked(object sender, RoutedEventArgs e)
        {
            if (useCamera.IsChecked ?? false)
            {
                pcc.Start();
            }
        }

    }
}
