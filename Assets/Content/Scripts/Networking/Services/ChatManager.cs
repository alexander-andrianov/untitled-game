using System;
using Unity.Services.Vivox;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class ChatManager : MonoBehaviour
{
    public static ChatManager Instance { get; private set; }

    private IVivoxService vivoxService;
    private string currentChannelName;
    private bool isTransmitting;
    private float nextPosUpdate;
    private bool isInitialized;

    public bool IsTransmitting => isTransmitting;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            nextPosUpdate = Time.time;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        try
        {
            LeaveChannel().Forget();
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave channel: {e.Message}");
        }
    }

    public async UniTask InitializeAsync()
    {
        if (isInitialized) return;

        try
        {
            if (vivoxService == null)
            {
                vivoxService = VivoxService.Instance;
                if (vivoxService == null)
                {
                    Debug.LogError("Failed to get VivoxService instance");
                    return;
                }
            }

            await vivoxService.InitializeAsync();
            isInitialized = true;
            Debug.Log("Vivox initialized successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to initialize Vivox: {e.Message}");
            isInitialized = false;
        }
    }

    public async UniTask JoinPositionalChannel(string channelName, float maxDistance = 10f, float minDistance = 5f)
    {
        if (!isInitialized || vivoxService == null)
        {
            Debug.LogError("Vivox is not initialized");
            return;
        }

        try
        {
            // Сначала покидаем текущий канал, если он есть
            if (!string.IsNullOrEmpty(currentChannelName))
            {
                await LeaveChannel();
            }

            currentChannelName = channelName;
            var channel3DProperties = new Channel3DProperties(
                (int)maxDistance, // audibleDistance
                (int)minDistance, // conversationalDistance
                1.0f, // audioFadeIntensityByDistance
                AudioFadeModel.InverseByDistance // audioFadeModel
            );

            await vivoxService.JoinPositionalChannelAsync(channelName, ChatCapability.TextAndAudio, channel3DProperties);
            Debug.Log($"Joined channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join channel: {e.Message}");
            currentChannelName = null;
        }
    }

    public async UniTask JoinNonPositionalChannel(string channelName)
    {
        if (!isInitialized || vivoxService == null)
        {
            Debug.LogError("Vivox is not initialized");
            return;
        }

        try
        {
            currentChannelName = channelName;
            await vivoxService.JoinGroupChannelAsync(channelName, ChatCapability.TextAndAudio);
            Debug.Log($"Joined channel: {channelName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to join channel: {e.Message}");
        }
    }

    public async UniTask LeaveChannel()
    {
        if (!isInitialized || vivoxService == null || string.IsNullOrEmpty(currentChannelName))
        {
            return;
        }

        try
        {
            await vivoxService.LeaveChannelAsync(currentChannelName);
            currentChannelName = null;
            Debug.Log("Left channel");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to leave channel: {e.Message}");
        }
    }

    public async void MuteMyself()
    {
        if (!isInitialized || vivoxService == null)
        {
            Debug.LogError("Vivox is not initialized");
            return;
        }

        try
        {
            await vivoxService.SetChannelTransmissionModeAsync(TransmissionMode.None, currentChannelName);
            isTransmitting = false;
            Debug.Log("Microphone muted");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to mute microphone: {e.Message}");
        }
    }

    public async void UnmuteMyself()
    {
        if (!isInitialized || vivoxService == null)
        {
            Debug.LogError("Vivox is not initialized");
            return;
        }

        try
        {
            await vivoxService.SetChannelTransmissionModeAsync(TransmissionMode.All, currentChannelName);
            isTransmitting = true;
            Debug.Log("Microphone unmuted");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to unmute microphone: {e.Message}");
        }
    }

    public void UpdatePosition(Vector3 position)
    {
        if (!isInitialized || vivoxService == null || string.IsNullOrEmpty(currentChannelName))
        {
            return;
        }

        try
        {
            if (Time.time > nextPosUpdate)
            {
                vivoxService.Set3DPosition(gameObject, currentChannelName);
                nextPosUpdate += 0.3f; // Обновляем позицию каждые 0.3 секунды
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to update position: {e.Message}");
        }
    }
}