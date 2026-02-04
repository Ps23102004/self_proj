using Unity.Netcode;
using UnityEngine;

namespace KartGame.Networking
{
    [DisallowMultipleComponent]
    public class NetworkBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            var networkManager = GetComponent<NetworkManager>();
            if (networkManager == null)
            {
                Debug.LogError("NetworkBootstrap requires a NetworkManager on the same GameObject.");
                return;
            }

            if (NetworkManager.Singleton != null && NetworkManager.Singleton != networkManager)
            {
                Destroy(gameObject);
                return;
            }

            DontDestroyOnLoad(gameObject);
        }
    }
}
