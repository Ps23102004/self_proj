using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KartGame.Networking;

namespace KartGame.UI
{
    [DisallowMultipleComponent]
    public class LobbyUI : MonoBehaviour
    {
        [Header("Networking")]
        public LobbyManager lobbyManager;

        [Header("Controls")]
        public InputField addressInput;
        public Button hostButton;
        public Button joinButton;
        public Button readyButton;
        public Button startRaceButton;
        public Button quitButton;

        [Header("Player List")]
        public RectTransform playerListRoot;
        public GameObject playerListEntryPrefab;

        private readonly List<GameObject> spawnedEntries = new List<GameObject>();
        private bool isReady;

        private void Start()
        {
            if (lobbyManager == null)
            {
                lobbyManager = LobbyManager.Instance;
            }

            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (readyButton != null) readyButton.onClick.AddListener(OnReadyClicked);
            if (startRaceButton != null) startRaceButton.onClick.AddListener(OnStartRaceClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (addressInput != null && string.IsNullOrEmpty(addressInput.text))
            {
                addressInput.text = "127.0.0.1";
            }

            if (lobbyManager != null)
            {
                lobbyManager.OnLobbyUpdated += RefreshList;
            }

            RefreshList();
        }

        private void OnDestroy()
        {
            if (lobbyManager != null)
            {
                lobbyManager.OnLobbyUpdated -= RefreshList;
            }
        }

        private void OnHostClicked()
        {
            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.SetTransportAddress(GetAddress());
            lobbyManager.StartHost();
        }

        private void OnJoinClicked()
        {
            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.SetTransportAddress(GetAddress());
            lobbyManager.StartClient();
        }

        private void OnReadyClicked()
        {
            if (lobbyManager == null)
            {
                return;
            }

            isReady = !isReady;
            lobbyManager.SetLocalReady(isReady);
            UpdateReadyButtonLabel();
        }

        private void OnStartRaceClicked()
        {
            if (lobbyManager == null)
            {
                return;
            }

            lobbyManager.TryStartRace();
        }

        private void OnQuitClicked()
        {
            if (lobbyManager != null)
            {
                lobbyManager.StopSession();
            }

            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene");
        }

        private string GetAddress()
        {
            return addressInput != null ? addressInput.text : "127.0.0.1";
        }

        private void Update()
        {
            if (lobbyManager == null)
            {
                return;
            }

            bool isHost = lobbyManager.IsServer && lobbyManager.IsClient;
            if (startRaceButton != null)
            {
                startRaceButton.gameObject.SetActive(isHost);
                startRaceButton.interactable = lobbyManager.CanStartRace();
            }
        }

        private void RefreshList()
        {
            foreach (var entry in spawnedEntries)
            {
                Destroy(entry);
            }
            spawnedEntries.Clear();

            if (lobbyManager == null || playerListRoot == null || playerListEntryPrefab == null || !lobbyManager.IsSpawned)
            {
                return;
            }

            var players = lobbyManager.Players;
            for (int i = 0; i < players.Count; i++)
            {
                var entry = Instantiate(playerListEntryPrefab, playerListRoot);
                var text = entry.GetComponent<Text>();
                if (text != null)
                {
                    string ready = players[i].Ready ? "Ready" : "Not Ready";
                    text.text = $"{players[i].Name} - {ready}";
                }
                spawnedEntries.Add(entry);
            }
        }

        private void UpdateReadyButtonLabel()
        {
            if (readyButton == null)
            {
                return;
            }

            var text = readyButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = isReady ? "Unready" : "Ready";
            }
        }
    }
}
