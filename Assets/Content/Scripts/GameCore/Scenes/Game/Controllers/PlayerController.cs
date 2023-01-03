using System;
using System.Threading.Tasks;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

namespace Content.Scripts.GameCore.Scenes.Game.Controllers
{
    public class PlayerController : NetworkBehaviour
    {
        private const float MovementSpeedValue = 7f;
        private const float RotationSpeedValue = 350f;
        private const float RotationDumpingValue = 10f;

        [SerializeField] private PlayableDirector ghostDirector;

        [SerializeField] private Transform mainModel;

        [SerializeField] private Light playerLight;

        public Light PlayerLight => playerLight;

        private PlayerInput playerInput;
        private Transform localTransform;
        private Vector3 currentMovementInput;

        private PlayableDirector activeDirector;

        public override void OnNetworkSpawn()
        {
            if (!IsOwner)
            {
                enabled = false;
            }
        }

        private async void Start()
        {
            Initialize();

            await Task.Delay(TimeSpan.FromSeconds(15f));
            HandleKill();
        }

        private void OnDisable()
        {
            if (IsOwner)
            {
                playerInput.Disable();
            }
        }

        private void Update()
        {
            MovePlayer();
            RotatePlayer();
        }

        private void Initialize()
        {
            var virtualCamera = FindObjectOfType<CinemachineVirtualCamera>();
            virtualCamera.Follow = transform;

            localTransform = GetComponent<Transform>();

            playerInput = new PlayerInput();
            playerInput.Enable();
        }

        private void MovePlayer()
        {
            var currentPosition = localTransform.position;
            currentMovementInput = playerInput.Player.Move.ReadValue<Vector3>().normalized;
            currentMovementInput.y = 0;

            currentPosition += currentMovementInput * (MovementSpeedValue * Time.deltaTime);
            transform.position = currentPosition;
        }

        private void RotatePlayer()
        {
            var movementDirection = currentMovementInput;

            if (movementDirection == Vector3.zero) return;

            var rotateDirection = Quaternion.LookRotation(movementDirection);
            transform.rotation =
                Quaternion.RotateTowards(transform.rotation, rotateDirection, RotationSpeedValue * Time.deltaTime);
        }

        private async void HandleKill()
        {
            mainModel.gameObject.SetActive(false);
            await RunPlayableDirectorAsync(ghostDirector);
        }

        private UniTask RunPlayableDirectorAsync(PlayableDirector playableDirector)
        {
            playableDirector.Play();
            return UniTask.WaitWhile(() => playableDirector.state == PlayState.Playing);
        }
    }
}