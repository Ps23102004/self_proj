using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using KartGame.Gameplay;
using KartGame.Networking;

namespace KartGame.UI
{
    [DisallowMultipleComponent]
    public class ResultsUI : MonoBehaviour
    {
        public RaceManager raceManager;

        [Header("UI")]
        public GameObject panel;
        public RectTransform resultsRoot;
        public GameObject resultEntryPrefab;
        public Button backToLobbyButton;

        private readonly List<GameObject> entries = new List<GameObject>();

        private void Start()
        {
            if (raceManager == null)
            {
                raceManager = RaceManager.Instance;
            }

            if (raceManager != null)
            {
                raceManager.OnRaceStateChanged += Refresh;
            }

            if (backToLobbyButton != null)
            {
                backToLobbyButton.onClick.AddListener(OnBackToLobbyClicked);
            }

            Refresh();
        }

        private void OnDestroy()
        {
            if (raceManager != null)
            {
                raceManager.OnRaceStateChanged -= Refresh;
            }
        }

        private void Refresh()
        {
            bool show = raceManager != null && raceManager.State.Value == RaceState.Finished;
            if (panel != null)
            {
                panel.SetActive(show);
            }

            if (!show)
            {
                ClearEntries();
                return;
            }

            PopulateResults();
        }

        private void PopulateResults()
        {
            ClearEntries();

            if (raceManager == null || resultsRoot == null || resultEntryPrefab == null)
            {
                return;
            }

            var results = new List<RaceResult>(raceManager.Results);
            results.Sort((a, b) => a.Time.CompareTo(b.Time));

            for (int i = 0; i < results.Count; i++)
            {
                var entry = Instantiate(resultEntryPrefab, resultsRoot);
                var text = entry.GetComponent<Text>();
                if (text != null)
                {
                    text.text = $"{i + 1}. Player {results[i].ClientId} - {RaceUtils.FormatTime(results[i].Time)}";
                }
                entries.Add(entry);
            }
        }

        private void ClearEntries()
        {
            foreach (var entry in entries)
            {
                Destroy(entry);
            }
            entries.Clear();
        }

        private void OnBackToLobbyClicked()
        {
            if (raceManager != null && raceManager.IsServer)
            {
                var lobby = LobbyManager.Instance;
                if (lobby != null)
                {
                    lobby.ResetReadyStates();
                }

                raceManager.NetworkManager.SceneManager.LoadScene("LobbyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
            }
        }
    }
}
