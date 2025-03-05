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
        [SerializeField] private Renderer modelRenderer;
        [SerializeField] private CinemachineVirtualCamera virtualCameraPrefab;

        public Light PlayerLight => playerLight;

        private PlayerInput playerInput;
        private PlayableDirector activeDirector;
        private Transform localTransform;
        private Vector3 currentMovementInput;
        private bool isInitialized;
        private CinemachineVirtualCamera playerCamera;

        public override void OnNetworkSpawn()
        {
            Initialize();
        }

        private async void Start()
        {
            await Task.Delay(TimeSpan.FromSeconds(15f));
            HandleKill();
        }

        private void OnDisable()
        {
            if (IsOwner && playerInput != null)
            {
                playerInput.Disable();
            }

            if (playerCamera != null)
            {
                Destroy(playerCamera.gameObject);
            }
        }

        private void Update()
        {
            if (!isInitialized) return;

            if (IsOwner)
            {
                MovePlayer();
                RotatePlayer();
            }
        }

        private void Initialize()
        {
            localTransform = GetComponent<Transform>();

            if (IsOwner)
            {
                if (virtualCameraPrefab == null)
                {
                    Debug.LogError($"Virtual camera prefab is not assigned for player {NetworkManager.Singleton.LocalClientId}");
                    return;
                }

                // Создаем отдельную камеру для этого игрока
                playerCamera = Instantiate(virtualCameraPrefab);
                playerCamera.Follow = transform;
                playerCamera.Priority = 10; // Устанавливаем высокий приоритет для камеры игрока
                Debug.Log($"Created camera for player {NetworkManager.Singleton.LocalClientId}");

                playerInput = new PlayerInput();
                playerInput.Enable();
            }

            isInitialized = true;
        }

        private void MovePlayer()
        {
            if (!IsOwner) return;

            var currentPosition = localTransform.position;
            currentMovementInput = playerInput.Player.Move.ReadValue<Vector3>().normalized;
            currentMovementInput.y = 0;

            currentPosition += currentMovementInput * (MovementSpeedValue * Time.deltaTime);
            transform.position = currentPosition;
        }

        private void RotatePlayer()
        {
            if (!IsOwner) return;

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

        public void SetColor(Color color)
        {
            if (modelRenderer != null)
            {
                modelRenderer.material.color = color;
            }
        }
    }
}