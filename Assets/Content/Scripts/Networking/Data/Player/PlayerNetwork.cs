using Content.Scripts.Networking.Data.Player;
using Unity.Netcode;
using UnityEngine;

namespace Content.Scripts.Networking.Data
{
    public class PlayerNetwork : NetworkBehaviour
    {
        private readonly NetworkVariable<PlayerNetworkData> networkData = new(writePerm: NetworkVariableWritePermission.Owner);

        [SerializeField] private float interpolationTime = 0.08f;

        private Vector3 velocity;
        
        private float rotationVelocity;
        
        private void Update()
        {
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