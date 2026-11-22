using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;

namespace PDSWARS.Networking {

    public class JoinGame : MonoBehaviour {

        public Toggle host;

        public NetworkManager NetworkManager {
            get {
                if (_networkManager == null)
                    return _networkManager = FindAnyObjectByType<NetworkManager>();
                return _networkManager;
            }
        }
        private NetworkManager _networkManager;

        public void Connect() {
            if (host.isOn)
                Server();
            Join();
        }

        public void Server() {
            NetworkManager?.ServerManager.StartConnection();
        }
        public void Join() {
            ushort.TryParse(PlayerPrefs.GetString("ServerPort", "7770"), out var port);
            NetworkManager?.ClientManager.StartConnection(PlayerPrefs.GetString("ServerAddress", "localhost"), port);
        }

    }

}