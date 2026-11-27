using System;
using System.Collections;
using AtProtoUnity;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using PDSWARS.GameState;
using PDSWARS.Protocol;
using PDSWARS.Protocol.UI;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PDSWARS.Universe {

    public class PlayerView : NetworkBehaviour {

        public static AtProtoAuthenticatedClient AtpAuthClient;
        public static AtProtoIdentity AtpIdentity;

        public readonly SyncVar<string> SyncedAtpHandle = new();
        public readonly SyncVar<string> SyncedAtpBskyDisplayName = new();

        public TextMeshPro handleDisplay;
        public Camera povCam;
        public string panActionName = "Pan";
        public Vector2 panSensitivity = new(1, -1);
        public float scrollSensitivity = 0.1f;
        public Transform focus;

        public Vector3 FocusPosition {
            get {
                if (focus)
                    return _focusPosition = focus.position;
                return _focusPosition;
            }
        }
        private Vector3 _focusPosition;

        public InputAction PanAction {
            get {
                if (_panAction != null) return _panAction;
                return _panAction = InputSystem.actions[panActionName];
            }
        }
        private InputAction _panAction;
        
        private bool Panning => PanAction.inProgress;

        public override void OnStartNetwork() {
                // ==== On-join setup for all PlayerViews ==== //
                // (Equivalent to Start() for all instances of PlayerView)
            //Subscribing to OnChange here occurs after AtpBskDisplayName has already been updated to match server data, so we must call it manually the first time.
            SyncedAtpBskyDisplayName.OnChange += UpdateDisplayName;
            UpdateDisplayName();
            
                // ==== Setup for this client's PlayerView ==== //
                // (Equivalent to Start() for the single instance of PlayerView belonging to this client)
            if (Owner.IsLocalClient) {
                // When the client connects, make their PlayerView's camera the active camera (and add an audio listener, since just adding one is easier than removing all the duplicate ones)
                if (Camera.main != null && Camera.main != povCam) {
                    Destroy(Camera.main.gameObject.GetComponent<AudioListener>());
                    Destroy(Camera.main.gameObject);
                }
                povCam.gameObject.AddComponent(typeof(AudioListener));
                povCam.enabled = true;
                _ = Initialize();
            }
        }

        public override void OnStopClient() {
            SyncedAtpBskyDisplayName.OnChange -= UpdateDisplayName;
        }

        private void UpdateDisplayName(string prev, string next, bool asServer) => UpdateDisplayName();

        public void UpdateDisplayName() {
            Debug.Log($"handle changed: {SyncedAtpBskyDisplayName.Value}");
            handleDisplay.text = SyncedAtpBskyDisplayName.Value;
        }

        public void Update() {
            // Only execute for current client's PlayerView (the others will be controlled by network)
            if (!IsOwner)
                return;
            float scrollDelta = Mouse.current.scroll.y.value;
            // Applies the following formula to the scroll delta so that it has inverse exponential scaling (approaches but is never 1):
            // (1-(2^(-|x|)))*sgn(x)
            // Desmos-friendly:
            // (1-(2^{-\left|x\right|}))\ \cdot\operatorname{sgn}(x)
            float exponentialScrollDelta = (1 - Mathf.Pow(2, -(Mathf.Abs(scrollDelta) * scrollSensitivity))) * Mathf.Sign(scrollDelta);
            // Make our position relative to the focus
            transform.position -= FocusPosition;
            // Zoom in/out according to scroll
            transform.position *= 1 + exponentialScrollDelta;
            // Move our position relative to world origin
            transform.position += FocusPosition;
            if (!Panning) return;
            transform.RotateAround(FocusPosition, Vector3.up, Pointer.current.delta.x.value * panSensitivity.x);
            transform.RotateAround(FocusPosition, transform.right, Pointer.current.delta.y.value * panSensitivity.y);
            
        }
        
        private async Awaitable Initialize() {
            // Fetch the player's Bluesky display name by looking up the app.bsky.actor.profile record, getting the self class and getting the displayName JSON value.
            var bskyDisplayName = (await AtpAuthClient.GetRecordAsync(AtpIdentity, "app.bsky.actor.profile", "self")).Value.Value["displayName"]?.ToString();
            // Fall back to handle if no Bluesky display name was found.
            if (string.IsNullOrEmpty(bskyDisplayName)) {
                Debug.Log($"{AtpIdentity.Handle} doesn't have a Bluesky profile (app.bsky.actor.profile record). Defaulting to ATProto handle as display name.");
                bskyDisplayName = AtpIdentity.Handle;
            } else
                Debug.Log($"Handle: \"{AtpIdentity.Handle}\" | Display name: \"{bskyDisplayName}\"");
            // Manually update the display name once it's been acquired
            UpdateDisplayName();
            // Continue with initialization on server side via RPC
            InitializeRPC(AtpIdentity.Handle, bskyDisplayName, ClientManager.Connection);
        }

        /// <summary>
        /// RPC to, on this instance on the server, call GameManagerServer.PlayerJoin. Calling an RPC on the client can only be done on objects it owns (i.e. this instance of PlayerView), hence why this RPC just points to a function in a different class. Since ServerRPCs cannot be asynchronous, this just calls the actual function—PlayerJoin—which is async.
        /// </summary>
        /// <param name="handle">Joining player's AtProto handle.</param>
        /// <param name="displayName">The Bluesky display name (or handle, if no handle exists) of the joining player.</param>
        /// <param name="playerClient">The NetworkConnection of the joining player's client.</param>
        [ServerRpc]
        private void InitializeRPC(string handle, string displayName,  NetworkConnection playerClient) {
            // Set SyncVars
            SyncedAtpHandle.Value = handle;
            SyncedAtpBskyDisplayName.Value = displayName;
            // Register player with GameManagerServer
            _ = GameManagerServer.Instance.PlayerJoin(this);
        }

    }

}