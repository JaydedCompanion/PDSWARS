using System;
using System.Collections.Generic;
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
        public static readonly Dictionary<string, AtProtoIdentity> Identities = new();
        
        // ReSharper disable once InconsistentNaming
        public readonly SyncDictionary<string, Cube> SyncedPdses = new();
        public readonly SyncDictionary<string, PlayerView> SyncedPlayers = new();

        public PlayerView playerPrefab;

        /// <summary>
        /// Called by the client instance when the connection starts (i.e. they connect to the server)
        /// </summary>
        public override void OnStartServer() {
            ServerManager.OnRemoteConnectionState += PlayerJoined;
        }

        /// <summary>
        /// Subscribes to the client's OnLoadedStartScenes event to know when to spawn their PlayerView.
        /// </summary>
        /// <param name="playerClient">Client connection.</param>
        /// <param name="state">Connection state.</param>
        private void PlayerJoined(NetworkConnection playerClient, RemoteConnectionStateArgs state) {
            if (state.ConnectionState != RemoteConnectionState.Started)
                return;
            playerClient.OnLoadedStartScenes += PlayerLoadedScene;
        }
        /// <summary>
        /// Sends RPC to the server to join the game if a client starts a connection.
        /// </summary>
        /// <param name="playerClient">Client connection.</param>
        /// <param name="asServer">Whether this event is being called on a server instance (I think? Documentation is unclear).</param>
        private void PlayerLoadedScene(NetworkConnection playerClient, bool asServer) {
            // Instantiate a new player prefab...
            var playerView = Instantiate(playerPrefab);
            // ...then spawn it (instantiate in other clients) while granting ownership to playerClient...
            InstanceFinder.ServerManager.Spawn(playerView, playerClient);
            // Once we've initialized the player's instance, we can unsubscribe from the event.
            // I think this only gets called once anyway, but doesn't hurt.
            playerClient.OnLoadedStartScenes -= PlayerLoadedScene;
        }

        public async Awaitable PlayerJoin(PlayerView playerView) {
            var handle = playerView.SyncedAtpHandle.Value;
            Debug.Log($"Player {playerView.SyncedAtpBskyDisplayName.Value} ({handle}) joining...");
            // ...and add it to the list of Players
            SyncedPlayers.Add(handle, playerView);
            // Get player's identity and register it in the dictionary
            Identities.Add(handle, (await Client.ResolveHandleAsync(handle)).Value);
            // TODO: Implement "Player has already joined" check
            // TODO: Initialize cube for this player's PDS if it hasn't joined
            string endpoint;
            if (!SyncedPdses.ContainsKey(endpoint = Identities[handle].PdsEndpoint)) {
                Debug.Log($"Player's PDS endpoint ({endpoint}) was not found in the PDSs collection.");
                SyncedPdses.Add(endpoint,
                    Cube.New(endpoint, Vector3.up * (SyncedPdses.Count - 0.5f) * 10));
            }
        }

    }

}