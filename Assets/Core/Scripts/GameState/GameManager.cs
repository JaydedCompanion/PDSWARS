using System;
using System.Collections.Generic;
using AtProtoUnity;
using PDSWARS.Core.Protocol;
using PDSWARS.Core.Universe;
using UnityEngine;

namespace PDSWARS.Core.GameState
{

    public class GameManager : MonoBehaviour {
        
        // ReSharper disable once InconsistentNaming
        public Dictionary<string, Planet> pdses = new();
        public Dictionary<AtProtoIdentity, Player> Players = new ();

        private void Start() {
            // Here's where I'd handle instantiation of my other players... If I had [a multiplayer system in place]!
        }

        public void PlayerJoin(AtProtoIdentity player) {
            if (!pdses.ContainsKey(player.PdsEndpoint)) {
                Debug.Log($"Player {player.Handle}'s PDS ({player.PdsEndpoint} has not joined the game.");
                Planet newPlanet = Instantiate(Resources.Load<Planet>("Prefabs/PDS"));
                pdses.Add(player.PdsEndpoint, newPlanet);
            }
        }

    }

}
