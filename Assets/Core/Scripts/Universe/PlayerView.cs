using System;
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

        public static AtProtoIdentity AtpIdentity;
        public static AtProtoAuthenticatedClient AtpAuthClient;

        public readonly SyncVar<string> AtpHandle = new();
        public readonly SyncVar<string> AtpBskyDisplayName = new();

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
            AtpBskyDisplayName.OnChange += UpdateDisplayName;
            //Subscribing to OnChange here occurs after AtpBskDisplayName has already been updated to match server data, so we must call it manually the first time.
            UpdateDisplayName();
            // When the client connects, make their PlayerView's camera the active camera (and add an audio listener, since just adding one is easier than removing all the duplicate ones)
            if (Owner.IsLocalClient) {
                if (Camera.main != null && Camera.main != povCam) {
                    Destroy(Camera.main.gameObject.GetComponent<AudioListener>());
                    Destroy(Camera.main.gameObject);
                }
                povCam.gameObject.AddComponent(typeof(AudioListener));
                povCam.enabled = true;
                PlayerJoinRpc(this, Login.Instance.AtpLogin.Identity.Handle, ClientManager.Connection);
            }
        }

        public override void OnStopClient() {
            AtpBskyDisplayName.OnChange -= UpdateDisplayName;
        }

        private void UpdateDisplayName(string prev, string next, bool asServer) => UpdateDisplayName();

        public void UpdateDisplayName() {
            handleDisplay.text = AtpBskyDisplayName.Value;
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

        /// <summary>
        /// Remote procedure call for a player's client to tell the server's GameManager instance to join the game and set up their PlayerView. Since ServerRPCs cannot be asynchronous, this just calls the actual function—PlayerJoin—which is async.
        /// </summary>
        /// <param name="playerView">The PlayerView instance belonging to the joining player.</param>
        /// <param name="playerHandle">The ATProto handle of the joining player.</param>
        /// <param name="playerClient">The NetworkConnection of the joining player's client.</param>
        [ServerRpc]
        private void PlayerJoinRpc(PlayerView playerView, string playerHandle, NetworkConnection playerClient) => _ = GameManagerServer.Instance.PlayerJoin(playerView, playerHandle, playerClient);

    }

}