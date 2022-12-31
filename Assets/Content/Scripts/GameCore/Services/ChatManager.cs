using System;
using System.ComponentModel;
using Unity.Services.Vivox;
using UnityEngine;
using VivoxUnity;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance;

    private ILoginSession localLoginSession;
    private IChannelSession localChannelSession;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnApplicationQuit()
    {
        if (VivoxService.Instance.Client.Initialized)
        {
            VivoxService.Instance.Client.Uninitialize();
        }
    }

    #region LoginMethods

    public void Login(string displayName = null)
    {
        var account = new Account(displayName);

        localLoginSession = VivoxService.Instance.Client.GetLoginSession(account); 

        localLoginSession.BeginLogin(localLoginSession.GetLoginToken(), SubscriptionMode.Accept, null, null, null, ar =>
        {
            try
            {
                localLoginSession.EndLogin(ar);
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not login: {e.Message}");
            }
        });
    }

    #endregion

    #region JoinChannelMethods
    
    public void JoinEchoChannel()
    {
        if (localLoginSession.State == LoginState.LoggedIn)
        {
            JoinNonPositionalChannel("EchoTestChannel", ChannelType.Echo, true, true);
        }
    }

    public void JoinNonPositionalChannel(string channelName)
    {
        if (localLoginSession.State == LoginState.LoggedIn)
        {
            Debug.Log("CHANNEL NAME: " + channelName);
            JoinNonPositionalChannel(channelName, ChannelType.NonPositional, true, true);
        }
    }

    public void JoinPositionalChannel(string channelName, bool includeVoice, bool includeText, bool switchToThisChannel, ChannelType channelType,
        int maxHearingDistance, int minHearingDistance, float voiceFadeOutOverDistance, AudioFadeModel audioFadeModel)
    {
        var channel3DProperties = new Channel3DProperties(maxHearingDistance, minHearingDistance, voiceFadeOutOverDistance, audioFadeModel);
        Join3DChannel(channelName, includeVoice, includeText, switchToThisChannel, channelType, channel3DProperties);
    }

    private void JoinNonPositionalChannel(string channelName, ChannelType channelType, bool connectAudio, bool connectText, bool transmissionSwitch = true, Channel3DProperties properties = null)
    {
        if (localLoginSession.State == LoginState.LoggedIn)
        {
            var channel = new Channel(channelName, channelType, properties);
            var channelSession = localLoginSession.GetChannelSession(channel);

            channelSession.BeginConnect(connectAudio, connectText, transmissionSwitch, channelSession.GetConnectToken(), ar => 
            {
                try
                {
                    channelSession.EndConnect(ar);
                }
                catch(Exception e)
                {
                    Debug.LogError($"Could not connect to channel: {e.Message}");
                }
            });
        } else 
        {
            Debug.LogError("Can't join a channel when not logged in.");
        }
    } 
    
    private void Join3DChannel(string channelName, bool isAudio, bool isText, bool switchTransmission,
        ChannelType channelType, Channel3DProperties channel3DProperties)
    {
        var channel = new Channel(channelName, channelType, channel3DProperties);
        localChannelSession = localLoginSession.GetChannelSession(channel);
        BindChannelCallBackListener(true, localChannelSession);
        BindUserCallBacks(true, localChannelSession);

        if (isAudio)
        {
            localChannelSession.PropertyChanged += OnAudioStateChanged;
        }

        if (isText)
        {
            localChannelSession.PropertyChanged += OnTextStateChanged;
        }

        localChannelSession.BeginConnect(isAudio, isText, switchTransmission, localChannelSession.GetConnectToken(), ar =>
        {
            try
            {
                localChannelSession.EndConnect(ar);
            }
            catch (Exception e)
            {
                BindChannelCallBackListener(false, localChannelSession);
                BindUserCallBacks(false, localChannelSession);

                if (isAudio)
                {
                    localChannelSession.PropertyChanged -= OnAudioStateChanged;
                }

                if (isText)
                {
                    localChannelSession.PropertyChanged -= OnTextStateChanged;
                }

                Debug.Log(e);
            }
        });
    }

    #endregion

    #region MuteMethods

    public void MuteMyself()
    {
        localLoginSession.SetTransmissionMode(TransmissionMode.None);
    }

    #endregion

    #region BindCallBacks
    
    private void BindChannelCallBackListener(bool bind, IChannelSession channelSession)
    {
        if (bind)
        {
            channelSession.PropertyChanged += OnChannelStatusChanged;
        }
        else
        {
            channelSession.PropertyChanged -= OnChannelStatusChanged;

        }
    }
    
    private void BindUserCallBacks(bool bind, IChannelSession channelSession)
    {
        if (bind)
        {
            channelSession.Participants.AfterKeyAdded += OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved += OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated += OnParticipantUpdated;

        }
        else
        {
            channelSession.Participants.AfterKeyAdded -= OnParticipantAdded;
            channelSession.Participants.BeforeKeyRemoved -= OnParticipantRemoved;
            channelSession.Participants.AfterValueUpdated -= OnParticipantUpdated;
        }
    }
    
    #endregion

    #region UserCallbacks

    private void OnParticipantAdded(object sender, KeyEventArg<string> participantArg)
    {
        var source = (IReadOnlyDictionary<string, IParticipant>)sender;

        var user = source[participantArg.Key];

        Debug.Log($"{user.Account.Name} has join the channel");
    }
    
    private void OnParticipantRemoved(object sender, KeyEventArg<string> participantArg)
    {
        var source = (IReadOnlyDictionary<string, IParticipant>)sender;

        var user = source[participantArg.Key];

        Debug.Log($"{user.Account.Name} has left the channel");
    }
    
    private void OnParticipantUpdated(object sender, ValueEventArg<string, IParticipant> participantArg)
    {
        var source = (IReadOnlyDictionary<string, IParticipant>)sender;

        var user = source[participantArg.Key];
        
        Debug.Log($"{user.Account.Name} has been updated");
    }
    
    #endregion
    
    #region StatesChanges

    private void OnChannelStatusChanged(object sender, PropertyChangedEventArgs channelArgs)
    {
        var source = (IChannelSession)sender;

        if (channelArgs.PropertyName == "ChannelState")
        {
            switch (source.ChannelState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Channel Connecting...");
                    break;
                case ConnectionState.Connected:
                    Debug.Log($"{source.Channel.Name} Channel Connected");

                    break;
                case ConnectionState.Disconnecting:
                    Debug.Log($"{source.Channel.Name} Channel Disconnecting...");

                    break;
                case ConnectionState.Disconnected:
                    Debug.Log($"{source.Channel.Name} Channel Disconnected");
                    BindChannelCallBackListener(false, localChannelSession);
                    BindUserCallBacks(false, localChannelSession);

                    break;
            }
        }
    }
    
    private void OnAudioStateChanged(object sender, PropertyChangedEventArgs audioArgs)
    {
        var source = (IChannelSession)sender;
        
        if (audioArgs.PropertyName == "AudioState")
        {
            switch (source.AudioState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Audio Channel Connecting...");
                    break;
                case ConnectionState.Connected:
                    Debug.Log("Audio Channel Connected");
                    break;
                case ConnectionState.Disconnecting:
                    Debug.Log("Audio Channel Disconnecting...");
                    break;
                case ConnectionState.Disconnected:
                    Debug.Log("Audio Channel Disconnected");
                    localChannelSession.PropertyChanged -= OnAudioStateChanged;
                    break;
            }
        }
    }

    private void OnTextStateChanged(object sender, PropertyChangedEventArgs textArgs)
    {
        var source = (IChannelSession)sender;
        
        if (textArgs.PropertyName == "TextState")
        {
            switch (source.TextState)
            {
                case ConnectionState.Connecting:
                    Debug.Log("Text Channel Connecting...");
                    break;
                case ConnectionState.Connected:
                    Debug.Log("Text Channel Connected");

                    break;
                case ConnectionState.Disconnecting:
                    Debug.Log("Text Channel Disconnecting...");

                    break;
                case ConnectionState.Disconnected:
                    Debug.Log("Text Channel Disconnected");
                    localChannelSession.PropertyChanged -= OnTextStateChanged;
                    break;
            }
        }
    }
    
    #endregion
}