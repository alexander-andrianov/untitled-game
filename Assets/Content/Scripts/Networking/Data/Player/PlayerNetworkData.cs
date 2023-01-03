using Unity.Netcode;
using UnityEngine;

namespace Content.Scripts.Networking.Data.Player
{
    struct PlayerNetworkData : INetworkSerializable
    {
        private Vector3 position;
        private short yRotation;

        internal Vector3 Position
        {
            get => position;
            set => position = value;
        }

        internal Vector3 Rotation
        {
            get => new(0f, yRotation, 0f);
            set => yRotation = (short)value.y;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref position);
            serializer.SerializeValue(ref yRotation);
        }
    }
}