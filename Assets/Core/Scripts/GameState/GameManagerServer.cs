using System;
using AtProtoUnity;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using IrisUtils;
using PDSWARS.Protocol.UI;
using PDSWARS.Universe;
using PDSWARS.Universe.Cubes;
using PDSWARS.Utils;
using UnityEngine;

namespace PDSWARS.GameState {

    public class GameManagerServer : NetworkSingleton<GameManagerServer> {

        private static readonly AtProtoClient Client = new();
        
        // ReSharper disable once InconsistentNaming
        public readonly SyncDictionary<string, Cube> pdses = new();
        public readonly SyncDictionary<string, PlayerView> Players = new();

        public PlayerView playerPrefab;

        /// <summary>
        /// Called by the client instance when the connection starts (i.e. they connect to the server)
        /// </summary>
        public override void OnStartServer() {
            ServerManager.OnRemoteConnectionState += PlayerJoined;
        }

        /// <summary>
        /// Sends RPC to the server to join the game if a client starts a connection.
        /// </summary>
        /// <param name="playerClient">Client connection.</param>
        /// <param name="state">Connection state.</param>
        private void PlayerJoined(NetworkConnection playerClient, RemoteConnectionStateArgs state) {
            if (state.ConnectionState != RemoteConnectionState.Started)
                return;
            // Instantiate a new player prefab...
            var playerView = Instantiate(playerPrefab);
            // ...then spawn it (instantiate in other clients) while granting ownership to playerClient...
            InstanceFinder.ServerManager.Spawn(playerView, playerClient);
        }
        
        public async Awaitable PlayerJoin(PlayerView playerView, string playerHandle, NetworkConnection playerClient) {
            Debug.Log("Player Joined RCP (should be running on server!)");
            // ...and add it to the list of Players
            Players.Add(playerHandle, playerView);
            // Get the AtProtoIdentity corresponding to playerHandle, since we cannot directly pass the AtProtoIdentity over RPC.
            var playerIdentity = (await Client.ResolveHandleAsync(playerHandle)).Value;
            // Fetch the player's Bluesky display name by looking up the app.bsky.actor.profile record, getting the self class and getting the displayName JSON value.
            var displayName = (await Client.GetRecordAsync(playerIdentity, "app.bsky.actor.profile", "self")).Value.Value["displayName"]?.ToString();
            // Fall back to handle if no Bluesky display name was found.
            if (string.IsNullOrEmpty(displayName))
                displayName = playerIdentity.Handle;
            // Set the PlayerView's display name SyncVar with the value we got from the AtProto records
            playerView.AtpBskyDisplayName.Value = displayName;
            if (!pdses.ContainsKey(playerIdentity.PdsEndpoint)) {
                Debug.Log($"Player {playerIdentity.Handle}'s PDS ({playerIdentity.PdsEndpoint}) has not joined the game.");
                var newCube = Cube.New(playerIdentity.PdsEndpoint, Vector3.up * (pdses.Count - 0.5f) * 10);
                pdses.Add(playerIdentity.PdsEndpoint, newCube);
                pdses.Dirty(playerIdentity.PdsEndpoint);
                Debug.Log(newCube);
            }
        }

    }

}