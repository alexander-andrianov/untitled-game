using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
// using VivoxUnity;

namespace Content.Scripts.Gamecore.Services
{
    public class VivoxVoiceManager : MonoBehaviour
    {
        #region Enums

        /// <summary>
        /// Defines properties that can change.  Used by the functions that subscribe to the OnAfterTYPEValueUpdated functions.
        /// </summary>
        public enum ChangedProperty
        {
            None,
            Speaking,
            Typing,
            Muted
        }

        public enum ChatCapability
        {
            TextOnly,
            AudioOnly,
            TextAndAudio
        };

        #endregion

        #region Delegates/Events

        // public delegate void ParticipantValueChangedHandler(string username, ChannelId channel, bool value);

        // public event ParticipantValueChangedHandler OnSpeechDetectedEvent;


        // public delegate void ParticipantStatusChangedHandler(string username, ChannelId channel,
            // IParticipant participant);

        // public event ParticipantStatusChangedHandler OnParticipantAddedEvent;
        // public event ParticipantStatusChangedHandler OnParticipantRemovedEvent;

        // public delegate void ChannelTextMessageChangedHandler(string sender, IChannelTextMessage channelTextMessage);

        // public event ChannelTextMessageChangedHandler OnTextMessageLogReceivedEvent;

        public delegate void LoginStatusChangedHandler();

        public event LoginStatusChangedHandler OnUserLoggedInEvent;
        public event LoginStatusChangedHandler OnUserLoggedOutEvent;

        #endregion

        #region Member Variables

        private Uri _serverUri
        {
            get => new Uri(_server);

            set { _server = value.ToString(); }
        }

        [SerializeField] private string _server = "https://GETFROMPORTAL.www.vivox.com/api2";
        [SerializeField] private string _domain = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
        [SerializeField] private string _tokenIssuer = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
        [SerializeField] private string _tokenKey = "GET VALUE FROM VIVOX DEVELOPER PORTAL";
        private TimeSpan _tokenExpiration = TimeSpan.FromSeconds(90);

        // private Client _client = new Client();
        // private AccountId _accountId;

        // Check to see if we're about to be destroyed.
        private static object m_Lock = new object();
        private static VivoxVoiceManager m_Instance;

        /// <summary>
        /// Access singleton instance through this propriety.
        /// </summary>
        public static VivoxVoiceManager Instance
        {
            get
            {
                lock (m_Lock)
                {
                    if (m_Instance == null)
                    {
                        // Search for existing instance.
                        m_Instance = (VivoxVoiceManager)FindObjectOfType(typeof(VivoxVoiceManager));

                        // Create new instance if one doesn't already exist.
                        if (m_Instance == null)
                        {
                            // Need to create a new GameObject to attach the singleton to.
                            var singletonObject = new GameObject();
                            m_Instance = singletonObject.AddComponent<VivoxVoiceManager>();
                            singletonObject.name = typeof(VivoxVoiceManager).ToString() + " (Singleton)";
                        }
                    }

                    // Make instance persistent even if its already in the scene
                    DontDestroyOnLoad(m_Instance.gameObject);
                    return m_Instance;
                }
            }
        }


        // public LoginState LoginState { get; private set; }
        // public ILoginSession LoginSession;

        // public VivoxUnity.IReadOnlyDictionary<ChannelId, IChannelSession> ActiveChannels =>
            // LoginSession?.ChannelSessions;

        // public IAudioDevices AudioInputDevices => _client.AudioInputDevices;
        // public IAudioDevices AudioOutputDevices => _client.AudioOutputDevices;

        #endregion

        #region Properties

        /// <summary>
        /// Retrieves the first instance of a session that is transmitting. 
        /// </summary>
        // public IChannelSession TransmittingSession
        // {
        //     get
        //     {
        //         if (_client == null)
        //             throw new NullReferenceException("client");
        //         return _client.GetLoginSession(_accountId).ChannelSessions.FirstOrDefault(x => x.IsTransmitting);
        //     }
        //     set
        //     {
        //         if (value != null)
        //         {
        //             _client.GetLoginSession(_accountId).SetTransmissionMode(TransmissionMode.Single, value.Channel);
        //         }
        //     }
        // }

        #endregion

        private void Awake()
        {
            if (m_Instance != this)
            {
                Debug.LogWarning(
                    "Multiple VivoxVoiceManager detected in the scene. Only one VivoxVoiceManager can exist at a time. The duplicate VivoxVoiceManager will be destroyed.");
                Destroy(this);
                return;
            }
        }

        private void Start()
        {
            if (_serverUri.ToString() == "https://GETFROMPORTAL.www.vivox.com/api2" ||
                _domain == "GET VALUE FROM VIVOX DEVELOPER PORTAL" ||
                _tokenKey == "GET VALUE FROM VIVOX DEVELOPER PORTAL" ||
                _tokenIssuer == "GET VALUE FROM VIVOX DEVELOPER PORTAL")
            {
                Debug.LogError(
                    "The default VivoxVoiceServer values (Server, Domain, TokenIssuer, and TokenKey) must be replaced with application specific issuer and key values from your developer account.");
            }

            // _client.Uninitialize();

            // _client.Initialize();
        }

        private void OnApplicationQuit()
        {
            // Needed to add this to prevent some unsuccessful uninit, we can revisit to do better -carlo
            // Client.Cleanup();
            // if (_client != null)
            // {
                // VivoxLog("Uninitializing client.");
                // _client.Uninitialize();
                // _client = null;
            // }
        }

        public void Login(string displayName = null)
        {
            string uniqueId = Guid.NewGuid().ToString();
            //for proto purposes only, need to get a real token from server eventually
            // _accountId = new AccountId(_tokenIssuer, uniqueId, _domain, displayName);
            // LoginSession = _client.GetLoginSession(_accountId);
            // LoginSession.PropertyChanged += OnLoginSessionPropertyChanged;
            // LoginSession.BeginLogin(_serverUri, LoginSession.GetLoginToken(_tokenKey, _tokenExpiration),
            //     SubscriptionMode.Accept, null, null, null, ar =>
            //     {
            //         try
            //         {
            //             LoginSession.EndLogin(ar);
            //         }
            //         catch (Exception e)
            //         {
            //             // Handle error 
            //             VivoxLogError(nameof(e));
            //             // Unbind if we failed to login.
            //             LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            //             return;
            //         }
            //     });
        }

        public void Logout()
        {
            // if (LoginSession != null && LoginState != LoginState.LoggedOut && LoginState != LoginState.LoggingOut)
            // {
            //     OnUserLoggedOutEvent?.Invoke();
            //     LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
            //     LoginSession.Logout();
            // }
        }

        // public void JoinChannel(string channelName, ChannelType channelType, ChatCapability chatCapability,
        //     bool switchTransmission = true, Channel3DProperties properties = null)
        // {
        //     if (LoginState == LoginState.LoggedIn)
        //     {
        //         ChannelId channelId = new ChannelId(_tokenIssuer, channelName, _domain, channelType, properties);
        //         IChannelSession channelSession = LoginSession.GetChannelSession(channelId);
        //         channelSession.PropertyChanged += OnChannelPropertyChanged;
        //         channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
        //         channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
        //         channelSession.Participants.AfterValueUpdated += OnParticipantValueUpdated;
        //         channelSession.MessageLog.AfterItemAdded += OnMessageLogRecieved;
        //         channelSession.BeginConnect(chatCapability != ChatCapability.TextOnly,
        //             chatCapability != ChatCapability.AudioOnly, switchTransmission,
        //             channelSession.GetConnectToken(_tokenKey, _tokenExpiration), ar =>
        //             {
        //                 try
        //                 {
        //                     channelSession.EndConnect(ar);
        //                 }
        //                 catch (Exception e)
        //                 {
        //                     // Handle error 
        //                     VivoxLogError($"Could not connect to voice channel: {e.Message}");
        //                     return;
        //                 }
        //             });
        //     }
        //     else
        //     {
        //         VivoxLogError("Cannot join a channel when not logged in.");
        //     }
        // }

        // public void SendTextMessage(string messageToSend, ChannelId channel, string applicationStanzaNamespace = null,
        //     string applicationStanzaBody = null)
        // {
        //     if (ChannelId.IsNullOrEmpty(channel))
        //     {
        //         throw new ArgumentException("Must provide a valid ChannelId");
        //     }
        //
        //     if (string.IsNullOrEmpty(messageToSend))
        //     {
        //         throw new ArgumentException("Must provide a message to send");
        //     }
        //
        //     var channelSession = LoginSession.GetChannelSession(channel);
        //     channelSession.BeginSendText(null, messageToSend, applicationStanzaNamespace, applicationStanzaBody, ar =>
        //     {
        //         try
        //         {
        //             channelSession.EndSendText(ar);
        //         }
        //         catch (Exception e)
        //         {
        //             VivoxLog($"SendTextMessage failed with exception {e.Message}");
        //         }
        //     });
        // }

        // public void DisconnectAllChannels()
        // {
        //     if (ActiveChannels?.Count > 0)
        //     {
        //         foreach (var channelSession in ActiveChannels)
        //         {
        //             channelSession?.Disconnect();
        //         }
        //     }
        // }

        #region Vivox Callbacks

        // private void OnMessageLogRecieved(object sender, QueueItemAddedEventArgs<IChannelTextMessage> textMessage)
        // {
        //     ValidateArgs(new object[] { sender, textMessage });
        //
        //     IChannelTextMessage channelTextMessage = textMessage.Value;
        //     VivoxLog(channelTextMessage.Message);
        //     OnTextMessageLogReceivedEvent?.Invoke(channelTextMessage.Sender.DisplayName, channelTextMessage);
        // }
        //
        // private void OnLoginSessionPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        // {
        //     if (propertyChangedEventArgs.PropertyName != "State")
        //     {
        //         return;
        //     }
        //
        //     var loginSession = (ILoginSession)sender;
        //     LoginState = loginSession.State;
        //     VivoxLog("Detecting login session change");
        //     switch (LoginState)
        //     {
        //         case LoginState.LoggingIn:
        //         {
        //             VivoxLog("Logging in");
        //             break;
        //         }
        //         case LoginState.LoggedIn:
        //         {
        //             VivoxLog("Connected to voice server and logged in.");
        //             OnUserLoggedInEvent?.Invoke();
        //             break;
        //         }
        //         case LoginState.LoggingOut:
        //         {
        //             VivoxLog("Logging out");
        //             break;
        //         }
        //         case LoginState.LoggedOut:
        //         {
        //             VivoxLog("Logged out");
        //             LoginSession.PropertyChanged -= OnLoginSessionPropertyChanged;
        //             break;
        //         }
        //         default:
        //             break;
        //     }
        // }
        //
        // private void OnParticipantAdded(object sender, KeyEventArg<string> keyEventArg)
        // {
        //     ValidateArgs(new object[] { sender, keyEventArg });
        //
        //     // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        //     var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        //     // Look up the participant via the key.
        //     var participant = source[keyEventArg.Key];
        //     var username = participant.Account.Name;
        //     var channel = participant.ParentChannelSession.Key;
        //     var channelSession = participant.ParentChannelSession;
        //
        //     // Trigger callback
        //     OnParticipantAddedEvent?.Invoke(username, channel, participant);
        // }
        //
        // private void OnParticipantRemoved(object sender, KeyEventArg<string> keyEventArg)
        // {
        //     ValidateArgs(new object[] { sender, keyEventArg });
        //
        //     // INFO: sender is the dictionary that changed and trigger the event.  Need to cast it back to access it.
        //     var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        //     // Look up the participant via the key.
        //     var participant = source[keyEventArg.Key];
        //     var username = participant.Account.Name;
        //     var channel = participant.ParentChannelSession.Key;
        //     var channelSession = participant.ParentChannelSession;
        //
        //     if (participant.IsSelf)
        //     {
        //         VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
        //         // Now that we are disconnected, unsubscribe.
        //         channelSession.PropertyChanged -= OnChannelPropertyChanged;
        //         channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
        //         channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
        //         channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
        //         channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;
        //
        //         // Remove session.
        //         var user = _client.GetLoginSession(_accountId);
        //         user.DeleteChannelSession(channelSession.Channel);
        //     }
        //
        //     // Trigger callback
        //     OnParticipantRemovedEvent?.Invoke(username, channel, participant);
        // }

        private static void ValidateArgs(object[] objs)
        {
            foreach (var obj in objs)
            {
                if (obj == null)
                    throw new ArgumentNullException(obj.GetType().ToString(), "Specify a non-null/non-empty argument.");
            }
        }

        // private void OnParticipantValueUpdated(object sender, ValueEventArg<string, IParticipant> valueEventArg)
        // {
        //     ValidateArgs(new object[] { sender, valueEventArg });
        //
        //     var source = (VivoxUnity.IReadOnlyDictionary<string, IParticipant>)sender;
        //     // Look up the participant via the key.
        //     var participant = source[valueEventArg.Key];
        //
        //     string username = valueEventArg.Value.Account.Name;
        //     ChannelId channel = valueEventArg.Value.ParentChannelSession.Key;
        //     string property = valueEventArg.PropertyName;
        //
        //     switch (property)
        //     {
        //         case "SpeechDetected":
        //         {
        //             VivoxLog($"OnSpeechDetectedEvent: {username} in {channel}.");
        //             OnSpeechDetectedEvent?.Invoke(username, channel, valueEventArg.Value.SpeechDetected);
        //             break;
        //         }
        //         case "AudioEnergy":
        //         {
        //             break;
        //         }
        //         default:
        //             break;
        //     }
        // }

        // private void OnChannelPropertyChanged(object sender, PropertyChangedEventArgs propertyChangedEventArgs)
        // {
        //     ValidateArgs(new object[] { sender, propertyChangedEventArgs });
        //
        //     //if (_client == null)
        //     //    throw new InvalidClient("Invalid client.");
        //     var channelSession = (IChannelSession)sender;
        //
        //     // IF the channel has removed audio, make sure all the VAD indicators aren't showing speaking.
        //     if (propertyChangedEventArgs.PropertyName == "AudioState" &&
        //         channelSession.AudioState == ConnectionState.Disconnected)
        //     {
        //         VivoxLog($"Audio disconnected from: {channelSession.Key.Name}");
        //
        //         foreach (var participant in channelSession.Participants)
        //         {
        //             OnSpeechDetectedEvent?.Invoke(participant.Account.Name, channelSession.Channel, false);
        //         }
        //     }
        //
        //     // IF the channel has fully disconnected, unsubscribe and remove.
        //     if ((propertyChangedEventArgs.PropertyName == "AudioState" ||
        //          propertyChangedEventArgs.PropertyName == "TextState") &&
        //         channelSession.AudioState == ConnectionState.Disconnected &&
        //         channelSession.TextState == ConnectionState.Disconnected)
        //     {
        //         VivoxLog($"Unsubscribing from: {channelSession.Key.Name}");
        //         // Now that we are disconnected, unsubscribe.
        //         channelSession.PropertyChanged -= OnChannelPropertyChanged;
        //         channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
        //         channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
        //         channelSession.Participants.AfterValueUpdated -= OnParticipantValueUpdated;
        //         channelSession.MessageLog.AfterItemAdded -= OnMessageLogRecieved;
        //
        //         // Remove session.
        //         var user = _client.GetLoginSession(_accountId);
        //         user.DeleteChannelSession(channelSession.Channel);
        //     }
        // }

        #endregion

        private void VivoxLog(string msg)
        {
            Debug.Log("<color=green>VivoxVoice: </color>: " + msg);
        }

        private void VivoxLogError(string msg)
        {
            Debug.LogError("<color=green>VivoxVoice: </color>: " + msg);
        }
    }
}