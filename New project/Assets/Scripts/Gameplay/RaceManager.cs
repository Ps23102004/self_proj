using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using KartGame.Track;

namespace KartGame.Gameplay
{
    [DisallowMultipleComponent]
    public class RaceManager : NetworkBehaviour
    {
        public static RaceManager Instance { get; private set; }

        [Header("Prefabs")]
        public KartController kartPrefab;

        [Header("Settings")]
        public KartSettings kartSettings;
        public RaceSettings raceSettings;

        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        public NetworkVariable<RaceState> State { get; private set; }
        public NetworkVariable<float> CountdownEndTime { get; private set; }
        public NetworkVariable<float> RaceStartTime { get; private set; }

        public event Action OnRaceStateChanged;

        private NetworkList<RaceResult> results;
        private readonly List<Checkpoint> checkpoints = new List<Checkpoint>();
        private readonly Dictionary<ulong, KartController> karts = new Dictionary<ulong, KartController>();
        private bool spawned;

        public IReadOnlyList<Checkpoint> Checkpoints => checkpoints;
        public IReadOnlyDictionary<ulong, KartController> Karts => karts;
        public NetworkList<RaceResult> Results => results;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            State = new NetworkVariable<RaceState>(RaceState.Waiting);
            CountdownEndTime = new NetworkVariable<float>(0f);
            RaceStartTime = new NetworkVariable<float>(0f);
            results = new NetworkList<RaceResult>();
        }

        public override void OnNetworkSpawn()
        {
            State.OnValueChanged += HandleStateChanged;

            if (IsServer)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted += HandleLoadEventCompleted;
            }
        }

        public override void OnNetworkDespawn()
        {
            State.OnValueChanged -= HandleStateChanged;

            if (IsServer && NetworkManager != null)
            {
                NetworkManager.SceneManager.OnLoadEventCompleted -= HandleLoadEventCompleted;
            }
        }

        private void HandleStateChanged(RaceState previous, RaceState next)
        {
            OnRaceStateChanged?.Invoke();
        }

        private void HandleLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
        {
            if (!IsServer)
            {
                return;
            }

            if (sceneName != gameObject.scene.name)
            {
                return;
            }

            if (!spawned)
            {
                SpawnAllKarts();
                StartCountdown();
                spawned = true;
            }
        }

        public void ApplySettings(KartSettings kart, RaceSettings race)
        {
            if (kart != null)
            {
                kartSettings = kart;
            }

            if (race != null)
            {
                raceSettings = race;
            }
        }

        public void SetSpawnPoints(Transform[] points)
        {
            spawnPoints = points;
        }

        public void SetCheckpoints(List<Checkpoint> list)
        {
            checkpoints.Clear();
            if (list != null)
            {
                checkpoints.AddRange(list);
            }
        }

        public bool CanDrive => State.Value == RaceState.Racing;

        private void SpawnAllKarts()
        {
            if (kartPrefab == null)
            {
                Debug.LogError("RaceManager: Kart prefab not set.");
                return;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("RaceManager: No spawn points configured.");
                return;
            }

            karts.Clear();
            results.Clear();

            int index = 0;
            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                Transform spawn = spawnPoints[index % spawnPoints.Length];
                var kart = Instantiate(kartPrefab, spawn.position, spawn.rotation);
                kart.Configure(this, kartSettings, raceSettings);

                var networkObject = kart.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.SpawnWithOwnership(clientId, true);
                }

                karts[clientId] = kart;
                index++;
            }
        }

        private void StartCountdown()
        {
            if (!IsServer || raceSettings == null)
            {
                return;
            }

            State.Value = RaceState.Countdown;
            CountdownEndTime.Value = (float)NetworkManager.NetworkTime.Time + raceSettings.countdownSeconds;
        }

        private void Update()
        {
            if (!IsServer || raceSettings == null)
            {
                return;
            }

            if (State.Value == RaceState.Countdown)
            {
                if (NetworkManager.NetworkTime.Time >= CountdownEndTime.Value)
                {
                    State.Value = RaceState.Racing;
                    RaceStartTime.Value = (float)NetworkManager.NetworkTime.Time;

                    foreach (var kart in karts.Values)
                    {
                        kart.ServerBeginRace(RaceStartTime.Value);
                    }
                }
            }
        }

        public void ServerReportFinish(KartController kart, float finishTime)
        {
            if (!IsServer)
            {
                return;
            }

            var result = new RaceResult { ClientId = kart.OwnerClientId, Time = finishTime };
            results.Add(result);

            if (results.Count >= karts.Count)
            {
                State.Value = RaceState.Finished;
            }
        }

        public int GetTotalLaps()
        {
            return raceSettings != null ? raceSettings.totalLaps : 3;
        }
    }
}
