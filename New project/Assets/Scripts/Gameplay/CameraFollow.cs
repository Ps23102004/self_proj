using UnityEngine;

namespace KartGame.Gameplay
{
    [DisallowMultipleComponent]
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [Header("Follow")]
        public Transform target;
        public float distance = 10f;
        public float height = 6f;
        public float pitch = 40f;
        public float followSpeed = 6f;
        public float rotationSpeed = 6f;

        private float shakeTime;
        private float shakeStrength;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                FindTarget();
                return;
            }

            Vector3 forward = target.forward;
            Vector3 desiredPosition = target.position - forward * distance + Vector3.up * height;
            transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);

            Quaternion desiredRotation = Quaternion.Euler(pitch, target.eulerAngles.y, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, rotationSpeed * Time.deltaTime);

            ApplyShake();
        }

        private void FindTarget()
        {
            var karts = FindObjectsOfType<KartController>();
            foreach (var kart in karts)
            {
                if (kart.IsOwner)
                {
                    target = kart.transform;
                    break;
                }
            }
        }

        public void AddShake(float strength, float duration)
        {
            shakeStrength = Mathf.Max(shakeStrength, strength);
            shakeTime = Mathf.Max(shakeTime, duration);
        }

        private void ApplyShake()
        {
            if (shakeTime <= 0f)
            {
                return;
            }

            shakeTime -= Time.deltaTime;
            float shake = shakeStrength * (shakeTime / Mathf.Max(0.01f, shakeTime + 0.01f));
            Vector3 offset = Random.insideUnitSphere * shake;
            transform.position += new Vector3(offset.x, offset.y, offset.z);
        }
    }
}
