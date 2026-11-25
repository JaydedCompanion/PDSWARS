using FishNet;
using FishNet.Managing;
using UnityEngine;

namespace PDSWARS.Networking {

    public class JoinGame : MonoBehaviour {

        public NetworkManager NetworkManager {
            get {
                if (_networkManager == null)
                    return _networkManager = FindAnyObjectByType<NetworkManager>();
                return _networkManager;
            }
        }
        private NetworkManager _networkManager;

        public bool autostartServerIfEditor = true;


        private void Start() {
            #if UNITY_EDITOR
            // If running as editor, act as the server by default (will save time on debugging)
            if (!autostartServerIfEditor)
                return;
            ushort.TryParse(PlayerPrefs.GetString("ServerPort", "7770"), out var port);
            InstanceFinder.ServerManager.StartConnection(port); 
            #endif
        }

        public void Connect() {
            Join();
        }

        public void Join() {
            ushort.TryParse(PlayerPrefs.GetString("ServerPort", "7770"), out var port);
            NetworkManager?.ClientManager.StartConnection(PlayerPrefs.GetString("ServerAddress", "localhost"), port);
        }

    }

}