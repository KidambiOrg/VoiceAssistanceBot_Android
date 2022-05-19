using Android.Media;
using Microsoft.Bot.Schema;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.CognitiveServices.Speech.Dialog;
using NAudio.Wave;
using Newtonsoft.Json;
using Plugin.SimpleAudioPlayer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VoiceBotMobile.Xamarin.Services;
using Xamarin.Forms;

namespace VoiceBotMobile.Xamarin
{
    public partial class MainPage : ContentPage
    {
        private string kwsModelDir;
        private string kwsModelFile = "<Keword Recognition file>";

        private DialogServiceConnector connector = null;
        private Queue<WavQueueEntry> playbackStreams = new Queue<WavQueueEntry>();
        private WaveOutEvent player = new WaveOutEvent();
        private WakeWordConfiguration activeWakeWordConfig = null;
        private CustomSpeechConfiguration customSpeechConfig = null;
        private ListenState listening = ListenState.NotListening;
        // public ObservableCollection<MessageDisplay> Messages { get; private set; } = new ObservableCollection<MessageDisplay>();

        public ObservableCollection<ActivityDisplay> Activities { get; private set; } = new ObservableCollection<ActivityDisplay>();

        public MainPage()
        {
            InitializeComponent();

            this.player.PlaybackStopped += this.Player_PlaybackStopped;
        }

        public ListenState ListeningState
        {
            get
            {
                return this.listening;
            }

            private set
            {
                this.listening = value;
                this.OnPropertyChanged(nameof(this.ListeningState));
            }
        }

        private void StartListening()
        {
            if (this.ListeningState == ListenState.NotListening)
            {
                if (this.connector == null)
                {
                    this.InitSpeechConnector();
                }

                try
                {
                    this.ListeningState = ListenState.Initiated;

                    this.connector.ListenOnceAsync();
                    System.Diagnostics.Debug.WriteLine("Started ListenOnceAsync");
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception ex)
                {
                }
#pragma warning restore CA1031 // Do not catch general exception types
            }
        }

        //public EventHandler<SpeechRecognitionCanceledEventArgs> Connector_Canceled { get; private set; }

        private void InitSpeechConnector()
        {
            try
            {
                DialogServiceConfig config = null;

                config = BotFrameworkConfig.FromSubscription(Constants.CognitiveServicesApiKey, Constants.CognitiveServicesRegion);

                if (this.connector != null)
                {
                    // First, unregister all events
                    this.connector.ActivityReceived -= this.Connector_ActivityReceived;
                    this.connector.Recognizing -= this.Connector_Recognizing;
                    this.connector.Recognized -= this.Connector_Recognized;
                    this.connector.Canceled -= this.Connector_Canceled;
                    this.connector.SessionStarted -= this.Connector_SessionStarted;
                    this.connector.SessionStopped -= this.Connector_SessionStopped;

                    // Then dispose the object
                    this.connector.Dispose();
                    this.connector = null;
                }

                this.connector = new DialogServiceConnector(config, AudioConfig.FromDefaultMicrophoneInput());

                this.connector.ActivityReceived += this.Connector_ActivityReceived;
                this.connector.Recognizing += this.Connector_Recognizing;
                this.connector.Recognized += this.Connector_Recognized;
                this.connector.Canceled += this.Connector_Canceled;
                this.connector.SessionStarted += this.Connector_SessionStarted;
                this.connector.SessionStopped += this.Connector_SessionStopped;

                this.connector.ConnectAsync();

                //this.activeWakeWordConfig = new WakeWordConfiguration(kwsModelFile);
                //this.connector.StartKeywordRecognitionAsync(this.activeWakeWordConfig.WakeWordModel);

                kwsModelDir = DependencyService.Get<IAssetService>().GetAssetPath(kwsModelFile);
                var model = KeywordRecognitionModel.FromFile(kwsModelDir);
                this.connector.StartKeywordRecognitionAsync(model);

                //// Creates an instance of a speech config with specified subscription key and
                //// service region. Replace with your own subscription key and service region (e.g., "westus").
                //var config = SpeechConfig.FromSubscription("24838d27e2b749b9aaf27bd2c91ea506", "westus2");

                //kwsModelDir = DependencyService.Get<IAssetService>().GetAssetPath(kwsModelFile);
                //var model = KeywordRecognitionModel.FromFile(kwsModelDir);

                //// The phrase your keyword recognition model triggers on.
                //var keyword = "Hey Emma";

                //var stopRecognition = new TaskCompletionSource<int>();
                //var resultStr = "";

                //using (var recognizer = new SpeechRecognizer(config))
                //{
                //    // Subscribes to events.
                //    recognizer.Recognized += (s, e) =>
                //    {
                //        if (e.Result.Reason == ResultReason.RecognizedKeyword)
                //        {
                //            resultStr = $"RECOGNIZED KEYWORD: '{e.Result.Text}'";
                //        }
                //        else if (e.Result.Reason == ResultReason.RecognizedSpeech)
                //        {
                //            resultStr = $"RECOGNIZED: '{e.Result.Text}'";
                //        }
                //        else if (e.Result.Reason == ResultReason.NoMatch)
                //        {
                //            resultStr = "NOMATCH: Speech could not be recognized.";
                //        }
                //        Debug.WriteLine(resultStr);
                //        UpdateUI(resultStr);
                //    };

                // recognizer.Canceled += (s, e) => { var cancellation =
                // CancellationDetails.FromResult(e.Result); resultStr = $"CANCELED:
                // Reason={cancellation.Reason} ErrorDetails={cancellation.ErrorDetails}"; if
                // (cancellation.Reason == CancellationReason.Error) { UpdateUI(resultStr); }
                // Debug.WriteLine(resultStr); stopRecognition.TrySetResult(0); };

                // recognizer.SessionStarted += (s, e) => { Debug.WriteLine("\nSession started
                // event."); };

                // recognizer.SessionStopped += (s, e) => { Debug.WriteLine("\nSession stopped
                // event."); Debug.WriteLine("\nStop recognition.");
                // stopRecognition.TrySetResult(0); };

                // Debug.WriteLine($"Say something starting with the keyword '{keyword}' followed by
                // whatever you want...");

                // // Starts continuous recognition using the keyword model. Use //
                // StopKeywordRecognitionAsync() to stop recognition. await recognizer.StartKeywordRecognitionAsync(model).ConfigureAwait(false);

                // // Waits for a single successful keyword-triggered speech recognition (or //
                // error). Use Task.WaitAny to keep the task rooted. Task.WaitAny(new[] {
                // stopRecognition.Task });

                //    await recognizer.StopKeywordRecognitionAsync().ConfigureAwait(false);
                //}
            }
            catch (Exception ex)
            {
                UpdateUI("Exception: " + ex.ToString());
            }
        }

        private async void OnRecognitionButtonWithKeywordClicked(object sender, EventArgs args)
        {
            this.ListeningState = ListenState.NotListening;
            InitSpeechConnector();
            //StartListening();
        }

        private void Connector_SessionStopped(object sender, SessionEventArgs e)
        {
            var message = "Stopped listening";

            Debug.WriteLine($"SessionStopped event, id = {e.SessionId}");

            message = "Stopped actively listening - waiting for wake word";

            this.UpdateUI(message);
            this.ListeningState = ListenState.NotListening;
        }

        private void Connector_SessionStarted(object sender, SessionEventArgs e)
        {
            Debug.WriteLine($"SessionStarted event, id = {e.SessionId}");
            this.UpdateUI("Listening ...");
            //this.player.Stop();
            this.ListeningState = ListenState.Listening;
        }

        private void Connector_Canceled(object sender, SpeechRecognitionCanceledEventArgs e)
        {
            if (e.Reason == CancellationReason.Error
                 && e.ErrorCode == CancellationErrorCode.ConnectionFailure
                 && e.ErrorDetails.Contains("1000"))
            {
                // Connection was closed by the remote host. Error code: 1000. Error details:
                // Exceeded maximum websocket connection idle duration (>300000ms = 5 minutes). A
                // graceful timeout after a connection is idle manifests as an error but isn't an
                // exceptional condition -- we don't want it show up as a big red bubble!
                this.UpdateUI("Active connection timed out but ready to reconnect on demand.");
            }
            else
            {
                var statusMessage = $"Error ({e.ErrorCode}) : {e.ErrorDetails}";
                this.UpdateUI(statusMessage);

                this.ListeningState = ListenState.NotListening;
            }
        }

        private void Connector_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            this.UpdateUI(string.Empty);
            if (!string.IsNullOrWhiteSpace(e.Result.Text) && e.Result.Reason == ResultReason.RecognizedSpeech)
            {
            }
        }

        private void Connector_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            this.UpdateUI(e.Result.Text);
        }

        private void Connector_ActivityReceived(object sender, ActivityReceivedEventArgs e)
        {
            var json = e.Activity;
            var activity = JsonConvert.DeserializeObject<Activity>(json);

            if (e.HasAudio && activity.Speak != null)
            {
                UpdateUI(activity.Speak);
                var audio = e.Audio;

                long readSize = -1;
                int sampleRate = 16000;

                var playbufferSize = AudioTrack.GetMinBufferSize(sampleRate, Android.Media.ChannelOut.Mono, Android.Media.Encoding.Pcm16bit);
                AudioAttributes attrs = new AudioAttributes.Builder().
                  SetContentType(Android.Media.AudioContentType.Speech).
                  SetUsage(AudioUsageKind.Media).Build();

                AudioFormat fmt = new AudioFormat.Builder().
                    SetChannelMask(ChannelOut.Mono).
                    SetEncoding(Android.Media.Encoding.Pcm16bit).
                    SetSampleRate(sampleRate).Build();

                var audioTrack = new AudioTrack(attrs, fmt, playbufferSize, AudioTrackMode.Stream, 0);
                byte[] buffer = new byte[playbufferSize];

                audioTrack.Play();

                while (readSize != 0)
                {
                    readSize = audio.Read(buffer);
                    audioTrack.Write(buffer, 0, (int)readSize);
                }

                audioTrack.Stop();
                audioTrack.Release();

                //var stream = new ProducerConsumerStream();

                //Task.Run(() =>
                //{
                //    var buffer = new byte[800];
                //    uint bytesRead = 0;
                //    while ((bytesRead = audio.Read(buffer)) > 0)
                //    {
                //        stream.Write(buffer, 0, (int)bytesRead);
                //    }
                //}).Wait();

                //var channelData = activity.GetChannelData<SpeechChannelData>();
                //var id = channelData?.ConversationalAiData?.RequestInfo?.InteractionId;
                //if (!string.IsNullOrEmpty(id))
                //{
                //    System.Diagnostics.Debug.WriteLine($"Expecting TTS stream {id}");
                //}

                //var wavStream = new RawSourceWaveStream(stream, new WaveFormat(16000, 16, 1));
                //this.playbackStreams.Enqueue(new WavQueueEntry(id, false, stream, wavStream));

                //if (this.player.PlaybackState != PlaybackState.Playing)
                //{
                //    Task.Run(() => this.PlayFromAudioQueue());
                //}
            }
        }

        private bool PlayFromAudioQueue()
        {
            WavQueueEntry entry = null;
            lock (this.playbackStreams)
            {
                if (this.playbackStreams.Count > 0)
                {
                    entry = this.playbackStreams.Peek();
                }
            }

            if (entry != null)
            {
                System.Diagnostics.Debug.WriteLine($"START playing {entry.Id}");
                try
                {
                    this.player.Init(entry.Reader);
                }
                catch (Exception ex)
                {
                }

                this.player.Play();

                return true;
            }

            return false;
        }

        private void Player_PlaybackStopped(object sender, StoppedEventArgs e)
        {
            lock (this.playbackStreams)
            {
                if (this.playbackStreams.Count == 0)
                {
                    return;
                }

                var entry = this.playbackStreams.Dequeue();
                entry.Stream.Close();
            }

            if (!this.PlayFromAudioQueue())
            {
                if (this.Activities.LastOrDefault(x => x.Activity.Type == ActivityTypes.Message)
                    ?.Activity?.AsMessageActivity()?.InputHint == InputHints.ExpectingInput)
                {
                    this.StartListening();
                }
            }
        }

        private async void OnEnableMicrophoneButtonClicked(object sender, EventArgs e)
        {
            bool micAccessGranted = await DependencyService.Get<IMicrophoneService>().GetPermissionAsync();
            if (!micAccessGranted)
            {
                UpdateUI("Please give access to microphone");
            }
        }

        private void UpdateUI(String message)
        {
            Device.BeginInvokeOnMainThread(() =>
            {
                RecognitionText.Text = message;
            });
        }
    }
}