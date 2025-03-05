namespace Content.Scripts.Networking.Data
{
    public struct Constants
    {
        public const int MaxPlayers = 4;
        public const string JoinKey = "j";
        
        // Lobby constants
        public const int MaxLobbies = 15;
        public const int HeartbeatInterval = 15;
        public const int LobbyRefreshRate = 20;
        
        // Chat constants
        public const float MinDistance = 1.0f;
        public const float MaxDistance = 10.0f;
        public const float AudioFadeIntensity = 1.0f;
    }
}