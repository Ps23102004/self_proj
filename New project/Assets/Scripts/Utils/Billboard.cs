using UnityEngine;

namespace KartGame.Utils
{
    [DisallowMultipleComponent]
    public class Billboard : MonoBehaviour
    {
        public bool lockY = true;
        private Camera targetCamera;

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                targetCamera = Camera.main;
                if (targetCamera == null)
                {
                    return;
                }
            }

            Vector3 direction = targetCamera.transform.position - transform.position;
            if (lockY)
            {
                direction.y = 0f;
            }

            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(-direction.normalized, Vector3.up);
            }
        }
    }
}
