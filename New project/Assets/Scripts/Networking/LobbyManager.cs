using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KartGame.Networking
{
    [DisallowMultipleComponent]
    public class LobbyManager : NetworkBehaviour
    {
        public static LobbyManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int maxPlayers = 4;
        [SerializeField] private string raceSceneName = "RaceScene";

        public event Action OnLobbyUpdated;

        private NetworkList<LobbyPlayerState> players;

        public IReadOnlyList<LobbyPlayerState> Players => players;
        public int MaxPlayers => maxPlayers;
        public string RaceSceneName => raceSceneName;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            players = new NetworkList<LobbyPlayerState>();
        }

        public override void OnNetworkSpawn()
        {
            players.OnListChanged += HandleLobbyListChanged;

            if (IsServer)
            {
                NetworkManager.OnClientConnectedCallback += HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

                AddPlayer(NetworkManager.LocalClientId);
            }
        }

        public override void OnNetworkDespawn()
        {
            if (players != null)
            {
                players.OnListChanged -= HandleLobbyListChanged;
            }

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
                NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
            }
        }

        private void HandleLobbyListChanged(NetworkListEvent<LobbyPlayerState> changeEvent)
        {
            OnLobbyUpdated?.Invoke();
        }

        private void HandleClientConnected(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            AddPlayer(clientId);
        }

        private void HandleClientDisconnected(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            RemovePlayer(clientId);
        }

        private void AddPlayer(ulong clientId)
        {
            if (players.Count >= maxPlayers)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].ClientId == clientId)
                {
                    return;
                }
            }

            var state = new LobbyPlayerState
            {
                ClientId = clientId,
                Ready = false,
                Name = new FixedString32Bytes($"Player {players.Count + 1}")
            };

            players.Add(state);
        }

        private void RemovePlayer(ulong clientId)
        {
            for (int i = players.Count - 1; i >= 0; i--)
            {
                if (players[i].ClientId == clientId)
                {
                    players.RemoveAt(i);
                    break;
                }
            }
        }

        public void SetTransportAddress(string address)
        {
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            if (transport != null)
            {
                transport.ConnectionData.Address = address;
            }
        }

        public void StartHost()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                return;
            }

            NetworkManager.Singleton.StartHost();
        }

        public void StartClient()
        {
            if (NetworkManager.Singleton.IsListening)
            {
                return;
            }

            NetworkManager.Singleton.StartClient();
        }

        public void StopSession()
        {
            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                NetworkManager.Singleton.Shutdown();
            }
        }

        public void SetLocalReady(bool ready)
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsServer)
            {
                UpdateReadyState(NetworkManager.LocalClientId, ready);
            }
            else
            {
                SetReadyServerRpc(ready);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
        {
            UpdateReadyState(rpcParams.Receive.SenderClientId, ready);
        }

        private void UpdateReadyState(ulong clientId, bool ready)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i].ClientId == clientId)
                {
                    var state = players[i];
                    state.Ready = ready;
                    players[i] = state;
                    break;
                }
            }
        }

        public bool CanStartRace()
        {
            if (!IsServer || players.Count == 0)
            {
                return false;
            }

            for (int i = 0; i < players.Count; i++)
            {
                if (!players[i].Ready)
                {
                    return false;
                }
            }

            return true;
        }

        public void TryStartRace()
        {
            if (!IsServer || !CanStartRace())
            {
                return;
            }

            NetworkManager.SceneManager.LoadScene(raceSceneName, LoadSceneMode.Single);
        }

        public void ResetReadyStates()
        {
            if (!IsServer)
            {
                return;
            }

            for (int i = 0; i < players.Count; i++)
            {
                var state = players[i];
                state.Ready = false;
                players[i] = state;
            }
        }
    }
}
