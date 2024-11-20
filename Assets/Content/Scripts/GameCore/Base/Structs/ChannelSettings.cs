using Unity.Services.Vivox;

namespace Content.Scripts.Gamecore.Base.Structs
{
    public struct ChannelSettings
    {
        public string Name;
        // public ChannelType Type;
        public bool ConnectAudio;
        public bool ConnectText;
        public bool SwitchToThisChannel;
    }
}
