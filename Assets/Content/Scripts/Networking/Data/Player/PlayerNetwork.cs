using Content.Scripts.Networking.Data.Player;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace Content.Scripts.Networking.Data
{
    public class PlayerNetwork : NetworkBehaviour
    {
        private NetworkVariable<PlayerNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Owner);
        private NetworkTransform networkTransform;

        [SerializeField] private float interpolationTime = 0.08f;

        private Vector3 velocity;
        private float rotationVelocity;
        
        public override void OnNetworkSpawn()
        {
            networkTransform = GetComponent<NetworkTransform>();
            if (networkTransform == null)
            {
                networkTransform = gameObject.AddComponent<NetworkTransform>();
            }
            
            networkTransform.Interpolate = true;
        }
        
        private void Update()
        {
            if (!IsSpawned) return;
            
            var playerTransform = transform;
            
            if (IsOwner)
            {
                networkData.Value = new PlayerNetworkData()
                {
                    Position = Vector3.SmoothDamp(playerTransform.position, networkData.Value.Position, ref velocity, interpolationTime),
                    Rotation = Quaternion.Euler(
                        0f,
                        Mathf.SmoothDampAngle(playerTransform.rotation.eulerAngles.y, networkData.Value.Rotation.y, ref rotationVelocity, interpolationTime),
                        0f).eulerAngles
                };
            }
            else
            {
                transform.position = Vector3.SmoothDamp(playerTransform.position, networkData.Value.Position, ref velocity, interpolationTime);
                transform.rotation = Quaternion.Euler(
                    0f,
                    Mathf.SmoothDampAngle(playerTransform.rotation.eulerAngles.y, networkData.Value.Rotation.y, ref rotationVelocity, interpolationTime),
                    0f);
            }
        }
    }
}