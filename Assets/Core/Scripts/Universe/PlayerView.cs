using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using PDSWARS.Protocol;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PDSWARS.Universe {

    public class PlayerView : NetworkBehaviour {

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

        public override void OnStartClient() {
            // When the client connects, make their PlayerView's camera the active camera
            if (IsOwner) {
                if (Camera.main != null && Camera.main != povCam) Destroy(Camera.main.gameObject);
                povCam.enabled = true;
                UpdateName(Player.AtpIdentity.Handle, Player.BskyDisplayName);
            }

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
        
        [ServerRpc]
        private void UpdateName(string handle, string displayName) {
            Debug.Log($"Updating handle {handle} and displayName {displayName}.");
            AtpHandle.Value = handle;
            AtpBskyDisplayName.Value = displayName;
            handleDisplay.text = displayName;
        }

    }

}