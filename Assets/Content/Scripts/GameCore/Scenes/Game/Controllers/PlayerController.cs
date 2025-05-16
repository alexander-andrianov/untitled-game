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

namespace Content.Scripts.GameCore.Scenes.Game.Controllers
{
    public class PlayerController : NetworkBehaviour
    {
        private static Dictionary<ulong, PlayerController> allPlayers = new Dictionary<ulong, PlayerController>();
        private static List<PlayerController> alivePlayers = new List<PlayerController>();
        
        private static int currentSpectatorIndex = -1;
        
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
        private CharacterController characterController;
        private NetworkTransform networkTransform;
        private NetworkVariable<float> verticalRotation = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Owner);
        private Vector3 moveDirection = Vector3.zero;
        private NetworkVariable<bool> isDead = new NetworkVariable<bool>();
        
        private float currentSpeed;
        private bool isGrounded;
        private bool isInitialized;

        public override void OnNetworkSpawn()
        {
            Initialize();
            
            allPlayers[NetworkObjectId] = this;
            alivePlayers.Add(this);
            Debug.Log($"Added player {NetworkObjectId} to players lists. Total players: {allPlayers.Count}, Alive: {alivePlayers.Count}");

            isDead.OnValueChanged += OnDeadStateChanged;
        }

        private void OnDeadStateChanged(bool previousValue, bool newValue)
        {
            if (newValue)
            {
                if (alivePlayers.Contains(this))
                {
                    alivePlayers.Remove(this);
                    Debug.Log($"Player {NetworkObjectId} died. Remaining alive: {alivePlayers.Count}");
                    
                    if (IsOwner)
                    {
                        playerInput.Disable();
                        characterController.enabled = false;
                        
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
            
            if (IsSpawned)
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

            playerCamera.Priority = 0;
            playerCamera.enabled = false;

            if (IsOwner)
            {
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
            
            currentMovementInput = playerInput.Player.Move.ReadValue<Vector2>();
            
            var moveX = currentMovementInput.x;
            var moveZ = currentMovementInput.y;
            var move = transform.right * moveX + transform.forward * moveZ;
            
            characterController.Move(move * currentSpeed * Time.deltaTime);
            
            if (playerInput.Player.Jump.WasPressedThisFrame() && isGrounded)
            {
                moveDirection.y = Mathf.Sqrt(jumpForce * -2f * gravity);
            }
            
            moveDirection.y += gravity * Time.deltaTime;
            characterController.Move(moveDirection * Time.deltaTime);
            
            if (networkTransform != null)
            {
                networkTransform.InLocalSpace = false;
                networkTransform.Interpolate = true;
            }
        }

        private void HandleLook()
        {
            if (playerCamera == null) return;
            
            var lookInput = playerInput.Player.Look.ReadValue<Vector2>();
            var mouseX = lookInput.x * mouseSensitivity;
            var mouseY = lookInput.y * mouseSensitivity;
            
            if (IsOwner)
            {
                verticalRotation.Value -= mouseY;
                verticalRotation.Value = Mathf.Clamp(verticalRotation.Value, -maxLookAngle, maxLookAngle);
            }
            
            playerCamera.transform.localRotation = Quaternion.Euler(verticalRotation.Value, 0f, 0f);
            transform.Rotate(Vector3.up * mouseX);
        }

        private async UniTask HandleKill()
        {
            if (isDead.Value) return;

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