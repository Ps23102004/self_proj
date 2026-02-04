using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using KartGame.Gameplay;

namespace KartGame.UI
{
    [DisallowMultipleComponent]
    public class RaceHUD : MonoBehaviour
    {
        [Header("References")]
        public RaceManager raceManager;

        [Header("UI")]
        public Text lapText;
        public Text positionText;
        public Text speedText;
        public Text lapTimeText;
        public Text totalTimeText;
        public Text countdownText;

        private KartController localKart;
        private Vector3 lastPosition;
        private float lastSpeedSampleTime;
        private float currentSpeed;
        private float goFlashTimer;
        private bool goShown;

        private void Start()
        {
            if (raceManager == null)
            {
                raceManager = RaceManager.Instance;
            }
        }

        private void Update()
        {
            if (raceManager == null)
            {
                return;
            }

            if (localKart == null)
            {
                FindLocalKart();
                return;
            }

            UpdateSpeed();
            UpdateLapInfo();
            UpdateTimeInfo();
            UpdatePlacement();
            UpdateCountdown();
        }

        private void FindLocalKart()
        {
            var karts = FindObjectsOfType<KartController>();
            foreach (var kart in karts)
            {
                if (kart.IsOwner)
                {
                    localKart = kart;
                    lastPosition = kart.transform.position;
                    lastSpeedSampleTime = Time.time;
                    break;
                }
            }
        }

        private void UpdateSpeed()
        {
            float deltaTime = Time.time - lastSpeedSampleTime;
            if (deltaTime > 0.02f)
            {
                float distance = Vector3.Distance(localKart.transform.position, lastPosition);
                currentSpeed = distance / deltaTime;
                lastPosition = localKart.transform.position;
                lastSpeedSampleTime = Time.time;
            }

            if (speedText != null)
            {
                speedText.text = $"{currentSpeed * 3.6f:0} km/h";
            }
        }

        private void UpdateLapInfo()
        {
            if (lapText == null)
            {
                return;
            }

            int totalLaps = raceManager.GetTotalLaps();
            int currentLap = Mathf.Clamp(localKart.LapsCompleted.Value + 1, 1, totalLaps);
            lapText.text = $"Lap {currentLap}/{totalLaps}";
        }

        private void UpdateTimeInfo()
        {
            float raceTime = 0f;
            float lapTime = 0f;

            if (raceManager.State.Value == RaceState.Racing || raceManager.State.Value == RaceState.Finished)
            {
                raceTime = (float)(NetworkManager.Singleton.NetworkTime.Time - raceManager.RaceStartTime.Value);
                lapTime = (float)(NetworkManager.Singleton.NetworkTime.Time - localKart.LastLapStartTime.Value);
            }

            if (localKart.Finished.Value)
            {
                raceTime = localKart.FinishTime.Value;
            }

            if (totalTimeText != null)
            {
                totalTimeText.text = RaceUtils.FormatTime(raceTime);
            }

            if (lapTimeText != null)
            {
                lapTimeText.text = RaceUtils.FormatTime(lapTime);
            }
        }

        private void UpdatePlacement()
        {
            if (positionText == null)
            {
                return;
            }

            var karts = new List<KartController>(FindObjectsOfType<KartController>());
            karts.Sort((a, b) => RaceUtils.CalculateProgress(b, raceManager.Checkpoints).CompareTo(RaceUtils.CalculateProgress(a, raceManager.Checkpoints)));
            int placement = karts.IndexOf(localKart) + 1;
            positionText.text = $"{placement}{GetOrdinal(placement)}";
        }

        private void UpdateCountdown()
        {
            if (countdownText == null)
            {
                return;
            }

            if (raceManager.State.Value == RaceState.Countdown)
            {
                float remaining = raceManager.CountdownEndTime.Value - (float)NetworkManager.Singleton.NetworkTime.Time;
                int count = Mathf.Max(0, Mathf.CeilToInt(remaining));
                countdownText.text = count > 0 ? count.ToString() : "GO!";
                countdownText.gameObject.SetActive(true);
                goShown = false;
            }
            else if (raceManager.State.Value == RaceState.Racing)
            {
                if (!goShown)
                {
                    goFlashTimer = 0.8f;
                    goShown = true;
                }

                if (goFlashTimer > 0f)
                {
                    goFlashTimer -= Time.deltaTime;
                    countdownText.text = "GO!";
                    countdownText.gameObject.SetActive(true);
                }
                else
                {
                    countdownText.gameObject.SetActive(false);
                }
            }
            else
            {
                countdownText.gameObject.SetActive(false);
            }
        }

        private string GetOrdinal(int number)
        {
            if (number % 100 >= 11 && number % 100 <= 13)
            {
                return "th";
            }

            switch (number % 10)
            {
                case 1: return "st";
                case 2: return "nd";
                case 3: return "rd";
                default: return "th";
            }
        }
    }
}
