using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using KartGame.Track;

namespace KartGame.Gameplay
{
    [RequireComponent(typeof(Rigidbody))]
    [DisallowMultipleComponent]
    public class KartController : NetworkBehaviour
    {
        [Header("Settings")]
        public KartSettings settings;

        [Header("Components")]
        public Rigidbody rb;

        public NetworkVariable<int> LapsCompleted = new NetworkVariable<int>(0);
        public NetworkVariable<int> LastCheckpoint = new NetworkVariable<int>(0);
        public NetworkVariable<float> LastLapStartTime = new NetworkVariable<float>(0f);
        public NetworkVariable<bool> Finished = new NetworkVariable<bool>(false);
        public NetworkVariable<float> FinishTime = new NetworkVariable<float>(0f);

        private RaceManager raceManager;
        private RaceSettings raceSettings;

        private float serverThrottle;
        private float serverSteer;
        private bool serverDrift;
        private bool serverBrake;

        private float driftCharge;
        private bool wasDrifting;
        private bool respawning;

        private InputAction moveAction;
        private InputAction driftAction;
        private InputAction brakeAction;

        public void Configure(RaceManager manager, KartSettings kartSettings, RaceSettings race)
        {
            raceManager = manager;
            settings = kartSettings;
            raceSettings = race;
        }

        public override void OnNetworkSpawn()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.isKinematic = !IsServer;

            if (IsOwner)
            {
                SetupInput();
            }
        }

        private void OnDestroy()
        {
            if (moveAction != null)
            {
                moveAction.Disable();
                driftAction.Disable();
                brakeAction.Disable();
            }
        }

        private void SetupInput()
        {
            moveAction = new InputAction("Move", InputActionType.Value);
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");
            moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");
            moveAction.AddBinding("<Gamepad>/leftStick");
            moveAction.AddBinding("<Gamepad>/dpad");

            driftAction = new InputAction("Drift", InputActionType.Button);
            driftAction.AddBinding("<Keyboard>/space");
            driftAction.AddBinding("<Gamepad>/rightShoulder");

            brakeAction = new InputAction("Brake", InputActionType.Button);
            brakeAction.AddBinding("<Keyboard>/leftShift");
            brakeAction.AddBinding("<Gamepad>/leftTrigger");

            moveAction.Enable();
            driftAction.Enable();
            brakeAction.Enable();
        }

        private void FixedUpdate()
        {
            if (!IsSpawned)
            {
                return;
            }

            if (IsOwner)
            {
                Vector2 move = moveAction != null ? moveAction.ReadValue<Vector2>() : Vector2.zero;
                float throttle = Mathf.Clamp(move.y, -1f, 1f);
                float steer = Mathf.Clamp(move.x, -1f, 1f);
                bool drift = driftAction != null && driftAction.IsPressed();
                bool brake = brakeAction != null && brakeAction.IsPressed();

                SendInputServerRpc(throttle, steer, drift, brake);
            }

            if (IsServer)
            {
                Simulate(serverThrottle, serverSteer, serverDrift, serverBrake);
                CheckOutOfBounds();
            }
        }

        [ServerRpc(RequireOwnership = false)]
        private void SendInputServerRpc(float throttle, float steer, bool drift, bool brake)
        {
            serverThrottle = Mathf.Clamp(throttle, -1f, 1f);
            serverSteer = Mathf.Clamp(steer, -1f, 1f);
            serverDrift = drift;
            serverBrake = brake;
        }

        private void Simulate(float throttle, float steer, bool drift, bool brake)
        {
            if (settings == null || rb == null)
            {
                return;
            }

            if (raceManager != null && !raceManager.CanDrive)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                return;
            }

            if (Finished.Value)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                return;
            }

            float accelInput = throttle;
            if (brake)
            {
                accelInput = Mathf.Min(accelInput, 0f);
            }

            float accelForce = accelInput >= 0f ? settings.acceleration : settings.brakeAcceleration;
            rb.AddForce(transform.forward * accelInput * accelForce, ForceMode.Acceleration);
            rb.AddForce(Vector3.down * settings.extraGravity, ForceMode.Acceleration);

            Vector3 localVelocity = transform.InverseTransformDirection(rb.velocity);
            float forwardSpeed = localVelocity.z;
            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / settings.maxSpeed);

            bool wantsDrift = drift && Mathf.Abs(steer) > 0.2f && Mathf.Abs(forwardSpeed) > settings.minDriftSpeed;
            if (wantsDrift)
            {
                driftCharge = Mathf.Min(settings.maxDriftCharge, driftCharge + settings.driftChargeRate * Time.fixedDeltaTime);
                wasDrifting = true;
            }
            else
            {
                if (wasDrifting && driftCharge >= settings.minDriftForBoost)
                {
                    rb.AddForce(transform.forward * settings.driftBoostForce, ForceMode.VelocityChange);
                }

                driftCharge = 0f;
                wasDrifting = false;
            }

            float steerStrength = settings.steerStrength * Mathf.Lerp(0.3f, 1f, speedFactor);
            if (wantsDrift)
            {
                steerStrength *= settings.driftSteerBonus;
            }

            if (Mathf.Abs(forwardSpeed) > 0.1f)
            {
                Quaternion rotation = rb.rotation * Quaternion.Euler(0f, steer * steerStrength * Time.fixedDeltaTime, 0f);
                rb.MoveRotation(rotation);
            }

            float lateralFriction = wantsDrift ? settings.driftLateralFriction : settings.lateralFriction;
            localVelocity.x *= lateralFriction;
            localVelocity.z = Mathf.Clamp(localVelocity.z, -settings.maxReverseSpeed, settings.maxSpeed);
            rb.velocity = transform.TransformDirection(localVelocity);
        }

        private void CheckOutOfBounds()
        {
            if (raceSettings == null || respawning)
            {
                return;
            }

            bool flipped = Vector3.Dot(transform.up, Vector3.up) < 0.1f;
            bool outOfBounds = transform.position.y < raceSettings.outOfBoundsY;

            if (flipped || outOfBounds)
            {
                StartCoroutine(RespawnCoroutine());
            }
        }

        private IEnumerator RespawnCoroutine()
        {
            respawning = true;
            yield return new WaitForSeconds(raceSettings.respawnDelay);

            int nearestIndex = FindNearestCheckpoint();
            if (raceManager != null && raceManager.Checkpoints.Count > 0)
            {
                var checkpoint = raceManager.Checkpoints[nearestIndex];
                var next = raceManager.Checkpoints[(nearestIndex + 1) % raceManager.Checkpoints.Count];
                Vector3 forward = (next.transform.position - checkpoint.transform.position).normalized;

                rb.position = checkpoint.transform.position + Vector3.up * 0.5f;
                rb.rotation = Quaternion.LookRotation(forward, Vector3.up);
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
                LastCheckpoint.Value = nearestIndex;
            }

            respawning = false;
        }

        private int FindNearestCheckpoint()
        {
            if (raceManager == null || raceManager.Checkpoints.Count == 0)
            {
                return 0;
            }

            int nearest = 0;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < raceManager.Checkpoints.Count; i++)
            {
                float dist = Vector3.Distance(transform.position, raceManager.Checkpoints[i].transform.position);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    nearest = i;
                }
            }

            return nearest;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsServer || raceManager == null || Finished.Value)
            {
                return;
            }

            var checkpoint = other.GetComponent<Checkpoint>();
            if (checkpoint == null)
            {
                return;
            }

            int total = raceManager.Checkpoints.Count;
            int expected = (LastCheckpoint.Value + 1) % total;

            if (checkpoint.Index != expected)
            {
                return;
            }

            int previous = LastCheckpoint.Value;
            LastCheckpoint.Value = checkpoint.Index;

            if (checkpoint.Index == 0 && previous == total - 1)
            {
                LapsCompleted.Value++;
                LastLapStartTime.Value = (float)NetworkManager.NetworkTime.Time;

                if (raceManager != null && LapsCompleted.Value >= raceManager.GetTotalLaps())
                {
                    Finished.Value = true;
                    FinishTime.Value = (float)NetworkManager.NetworkTime.Time - raceManager.RaceStartTime.Value;
                    raceManager.ServerReportFinish(this, FinishTime.Value);
                }
            }
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (!IsServer)
            {
                return;
            }

            float strength = Mathf.Clamp01(collision.relativeVelocity.magnitude / 12f) * 0.25f;
            if (strength <= 0.01f)
            {
                return;
            }

            var sendParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { OwnerClientId }
                }
            };

            CollisionShakeClientRpc(strength, sendParams);
        }

        [ClientRpc]
        private void CollisionShakeClientRpc(float strength, ClientRpcParams rpcParams = default)
        {
            if (IsOwner)
            {
                CameraFollow.Instance?.AddShake(strength, 0.2f);
            }
        }

        public void ServerBeginRace(float startTime)
        {
            if (!IsServer)
            {
                return;
            }

            LapsCompleted.Value = 0;
            LastCheckpoint.Value = 0;
            LastLapStartTime.Value = startTime;
            Finished.Value = false;
            FinishTime.Value = 0f;
        }
    }
}
