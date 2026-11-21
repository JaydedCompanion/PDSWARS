using System.Collections.Generic;
using AtProtoUnity;
using IrisUtils;
using PDSWARS.Core.Universe;
using PDSWARS.Protocol;
using UnityEngine;

namespace PDSWARS.GameState {

    public class GameManager : Singleton<GameManager> {

        private static readonly AtProtoClient Client = new AtProtoClient();

        // ReSharper disable once InconsistentNaming
        public Dictionary<string, Planet> pdses = new();
        public Dictionary<AtProtoIdentity, Player> Players = new();

        private void Start() {
            // Here's where I'd handle instantiation of my other players... If I had [a multiplayer system in place]!
        }

        public async Awaitable PlayerJoin(AtProtoIdentity player) {
            if (!pdses.ContainsKey(player.PdsEndpoint)) {
                var record = await Client.GetRecordAsync(player, "app.bsky.actor.profile", "self");
                var displayName = record.Value.Value["displayName"].ToString();
                Debug.Log($"Player {displayName}'s PDS ({player.PdsEndpoint}) has not joined the game.");
                Planet newPlanet = Instantiate(Resources.Load<Planet>("Prefabs/PDS"));
                pdses.Add(player.PdsEndpoint, newPlanet);
            }
        }

    }

}