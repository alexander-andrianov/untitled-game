using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cinemachine;
using Cysharp.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Playables;
using System.Collections;

namespace Content.Scripts.GameCore.Scenes.Game.Controllers
{
    public class PlayerController : NetworkBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float jumpForce = 5f;
        [SerializeField] private float gravity = -9.81f;
        
        [Header("Look Settings")]
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float maxLookAngle = 80f;

        [Header("References")]
        [SerializeField] private PlayableDirector ghostDirector;
        [SerializeField] private Transform mainModel;
        [SerializeField] private Light playerLight;
        [SerializeField] private Renderer modelRenderer;
        [SerializeField] private CinemachineVirtualCamera playerCamera;

        public Light PlayerLight => playerLight;

        private PlayerInput playerInput;
        private PlayableDirector activeDirector;
        private Transform localTransform;
        private Vector3 currentMovementInput;
        private bool isInitialized;
        private CharacterController characterController;
        private NetworkTransform networkTransform;
        private NetworkVariable<float> verticalRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
        private Vector3 moveDirection = Vector3.zero;
        private float currentSpeed;
        private bool isGrounded;
        private NetworkVariable<bool> isDead = new NetworkVariable<bool>();
        private static Dictionary<ulong, PlayerController> allPlayers = new Dictionary<ulong, PlayerController>();
        private static List<PlayerController> alivePlayers = new List<PlayerController>();
        private static int currentSpectatorIndex = -1;

        public override void OnNetworkSpawn()
        {
            Initialize();
            
            // Добавляем игрока в словарь всех игроков и список живых
            allPlayers[NetworkObjectId] = this;
            alivePlayers.Add(this);
            Debug.Log($"Added player {NetworkObjectId} to players lists. Total players: {allPlayers.Count}, Alive: {alivePlayers.Count}");

            // Подписываемся на изменение isDead
            isDead.OnValueChanged += OnDeadStateChanged;
        }

        private void OnDeadStateChanged(bool previousValue, bool newValue)
        {
            if (newValue) // Если игрок умер
            {
                if (alivePlayers.Contains(this))
                {
                    alivePlayers.Remove(this);
                    Debug.Log($"Player {NetworkObjectId} died. Remaining alive: {alivePlayers.Count}");
                    
                    // Отключаем управление для мертвого игрока
                    if (IsOwner)
                    {
                        playerInput.Disable();
                        characterController.enabled = false;
                        
                        // Если есть живые игроки, начинаем наблюдение за первым из них
                        if (alivePlayers.Count > 0)
                        {
                            currentSpectatorIndex = 0;
                            UpdateSpectatorCamera();
                        }
                    }
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            isDead.OnValueChanged -= OnDeadStateChanged;
            
            if (IsSpawned) // Проверяем, что объект все еще заспавнен
            {
                allPlayers.Remove(NetworkObjectId);
                alivePlayers.Remove(this);
                Debug.Log($"Removed player {NetworkObjectId} from players lists. Total players: {allPlayers.Count}, Alive: {alivePlayers.Count}");
                
                if (IsOwner)
                {
                    UpdateSpectatorCamera();
                }
            }
            
            base.OnNetworkDespawn();
        }

        private async void Start()
        {
            if (IsHost && IsOwner)
            {
                await Task.Delay(TimeSpan.FromSeconds(15f));

                if (!isDead.Value)
                {
                    HandleKill();
                }
            }
        }

        private void OnDisable()
        {
            if (IsOwner && playerInput != null)
            {
                playerInput.Disable();
            }
        }

        private void Update()
        {
            if (!isInitialized || !IsOwner || playerInput == null) return;
            
            if (!isDead.Value)
            {
                HandleMovement();
                HandleLook();
            }
            else
            {
                HandleSpectatorInput();
            }
        }

        private void Initialize()
        {
            localTransform = GetComponent<Transform>();
            characterController = GetComponent<CharacterController>();
            networkTransform = GetComponent<NetworkTransform>();
            currentSpeed = moveSpeed;

            if (playerCamera == null)
            {
                Debug.LogError($"Player camera is not assigned for player {NetworkManager.Singleton.LocalClientId}");
                return;
            }

            // Отключаем камеру для всех игроков по умолчанию
            playerCamera.Priority = 0;
            playerCamera.enabled = false;

            if (IsOwner)
            {
                // Включаем камеру только для владельца
                playerCamera.Priority = 10;
                playerCamera.enabled = true;
                Debug.Log($"Initialized camera for player {NetworkManager.Singleton.LocalClientId}");

                playerInput = new PlayerInput();
                playerInput.Enable();
                
                Cursor.lockState = CursorLockMode.Locked;
            }

            isInitialized = true;
        }

        private void HandleSpectatorInput()
        {
            var currentAlivePlayers = alivePlayers.Where(p => p != null && !p.isDead.Value).ToList();
            if (!isDead.Value || currentAlivePlayers.Count <= 1) return;

            if (Input.GetKeyDown(KeyCode.E))
            {
                currentSpectatorIndex = (currentSpectatorIndex + 1) % currentAlivePlayers.Count;
                UpdateSpectatorCamera();
            }
            else if (Input.GetKeyDown(KeyCode.Q))
            {
                currentSpectatorIndex = (currentSpectatorIndex - 1 + currentAlivePlayers.Count) % currentAlivePlayers.Count;
                UpdateSpectatorCamera();
            }
        }

        private void UpdateSpectatorCamera()
        {
            var currentAlivePlayers = alivePlayers.Where(p => p != null && !p.isDead.Value).ToList();
            if (currentAlivePlayers.Count == 0) return;

            Debug.Log($"Updating spectator camera. Alive players: {currentAlivePlayers.Count}, Current index: {currentSpectatorIndex}");

            // Отключаем все камеры только для мертвого игрока
            if (isDead.Value)
            {
                foreach (var player in allPlayers.Values)
                {
                    if (player?.playerCamera != null)
                    {
                        player.playerCamera.Priority = 0;
                        player.playerCamera.enabled = false;
                        Debug.Log($"Disabled camera for player {player.NetworkObjectId}");
                    }
                }

                // Если остался только один игрок, включаем его камеру
                if (currentAlivePlayers.Count == 1)
                {
                    var alivePlayer = currentAlivePlayers[0];
                    if (alivePlayer?.playerCamera != null)
                    {
                        alivePlayer.playerCamera.Priority = 10;
                        alivePlayer.playerCamera.enabled = true;
                        Debug.Log($"Enabled camera for single alive player {alivePlayer.NetworkObjectId}");
                    }
                    currentSpectatorIndex = 0;
                    return;
                }

                // Включаем камеру текущего наблюдаемого игрока
                if (currentSpectatorIndex >= 0 && currentSpectatorIndex < currentAlivePlayers.Count)
                {
                    var targetPlayer = currentAlivePlayers[currentSpectatorIndex];
                    if (targetPlayer?.playerCamera != null)
                    {
                        targetPlayer.playerCamera.Priority = 10;
                        targetPlayer.playerCamera.enabled = true;
                        Debug.Log($"Enabled camera for spectator target player {targetPlayer.NetworkObjectId}");
                    }
                }
            }
        }

        private void HandleMovement()
        {
            if (characterController == null || !characterController.enabled) return;
            
            isGrounded = characterController.isGrounded;
            
            if (isGrounded && moveDirection.y < 0)
            {
                moveDirection.y = -2f;
            }
            
            currentSpeed = playerInput.Player.Sprint.IsPressed() ? sprintSpeed : moveSpeed;
            
            // Get input
            currentMovementInput = playerInput.Player.Move.ReadValue<Vector2>();
            float moveX = currentMovementInput.x;
            float moveZ = currentMovementInput.y;
            
            // Calculate movement direction
            Vector3 move = transform.right * moveX + transform.forward * moveZ;
            
            // Apply movement
            characterController.Move(move * currentSpeed * Time.deltaTime);
            
            // Jump
            if (playerInput.Player.Jump.WasPressedThisFrame() && isGrounded)
            {
                moveDirection.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            
            // Apply gravity
            moveDirection.y += gravity * Time.deltaTime;
            characterController.Move(moveDirection * Time.deltaTime);
            
            // Update network transform
            if (networkTransform != null)
            {
                networkTransform.InLocalSpace = false;
                networkTransform.Interpolate = true;
            }
        }

        private void HandleLook()
        {
            if (playerCamera == null) return;
            
            // Get mouse input
            Vector2 lookInput = playerInput.Player.Look.ReadValue<Vector2>();
            float mouseX = lookInput.x * mouseSensitivity;
            float mouseY = lookInput.y * mouseSensitivity;
            
            // Rotate camera up/down
            if (IsOwner) // Только владелец может изменять verticalRotation
            {
                verticalRotation.Value -= mouseY;
                verticalRotation.Value = Mathf.Clamp(verticalRotation.Value, -maxLookAngle, maxLookAngle);
            }
            
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation.Value, 0f, 0f);
            
            // Rotate player left/right
            transform.Rotate(Vector3.up * mouseX);
        }

        private async void HandleKill()
        {
            if (isDead.Value) return;

            // Сначала отключаем все компоненты управления
            if (IsOwner)
            {
                playerInput.Disable();
                characterController.enabled = false;
            }
            
            if (mainModel != null)
            {
                mainModel.gameObject.SetActive(false);
            }
            if (ghostDirector != null)
            {
                await RunPlayableDirectorAsync(ghostDirector);
            }

            // Только после всех визуальных эффектов меняем состояние смерти
            isDead.Value = true;
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