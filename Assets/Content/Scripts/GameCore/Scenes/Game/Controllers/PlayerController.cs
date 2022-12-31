using Cinemachine;
using Unity.Netcode;
using UnityEngine;

namespace Content.Scripts.GameCore.Scenes.Game.Controllers
{
    public class PlayerController : NetworkBehaviour
    {
        private const float SpeedValue = 350f;
        private const float RotationDumpingValue = 10f;
        
        [SerializeField]
        private Light playerLight;
        
        public Light PlayerLight => playerLight;
        
        private PlayerInput playerInput;
        private Rigidbody localRigidbody;

        public override void OnNetworkSpawn() {
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        private void Start()
        {
            Initialize();
        }
        
        private void OnDisable()
        {
            if (IsOwner)
            {
                playerInput.Disable();
            }
        }

        private void FixedUpdate()
        {
            MovePlayer();
            RotatePlayer();
        }

        private void Initialize()
        {
            var virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            virtualCamera.Follow = transform;

            localRigidbody = GetComponent<Rigidbody>();
            
            playerInput = new PlayerInput();
            playerInput.Enable();
        }
        
        private void MovePlayer()
        {  
            var movementInput = playerInput.Player.Move.ReadValue<Vector3>().normalized;
            movementInput.y = 0;

            localRigidbody.velocity = movementInput * (SpeedValue * Time.fixedDeltaTime);
        }
        
        private void RotatePlayer()
        {  
            var rotateDirection = localRigidbody.velocity.normalized;
            
            if (rotateDirection != Vector3.zero) {
                transform.forward = Vector3.Lerp(transform.forward, rotateDirection, RotationDumpingValue);
            }
        }
    }
}
