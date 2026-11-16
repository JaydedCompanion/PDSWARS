using AtProtoUnity;
using UnityEngine;
using UnityEngine.UI;

namespace PDSWARS.Core.Protocol.UI {

    [RequireComponent(typeof(AtProtoLogin))]
    public class Login : MonoBehaviour {

        public TMPro.TMP_InputField fieldUsername;
        public Button authenticateButton;
        
        private AtProtoLogin _atpLogin;
        
        public AtProtoLogin AtpLogin {
            get {
                if (_atpLogin == null) {
                    _atpLogin = GetComponent<AtProtoLogin>();
                }
                return _atpLogin;
            }
        }

        /// <summary>
        /// Function used by Button component's OnClick event to initiate ATProto authentication process.
        /// </summary>
        public void Submit() {
            // This is a fire-and-forget of the async method. Since we handle the result of the process within the function, we don't care about waiting for it to complete.
            // Adding "_ = " before calling an async method simply tells the IDE that we don't care about the function's return, but does not actually change the functionality. 
            _ = LogIn();
        }

        private async Awaitable LogIn() {
            // Abort LogIn() if the ATProtoLogin is already authenticating
            if (AtpLogin.State == AtProtoLoginState.Authorizing || AtpLogin.State == AtProtoLoginState.Refreshing) {
                Debug.LogError($"Attempted to initiate LogIn(), but ATProtoLogin.State was {AtpLogin.State}.\nCannot Initiate LogIn() until the current process has completed!");
                return;
            }
            // Disable authenticate button while oauth process is already underway.
            authenticateButton.interactable = false;
            AtpLogin.Handle = fieldUsername.text;
            // Initiate and wait for LoginAsync to complete
            await AtpLogin.LoginAsync();
            switch (AtpLogin.State) {
                case AtProtoLoginState.Authenticated: {
                    // TODO: Proceed to close login window add player to the game
                    // TODO: Change the log to use the display name (which will require looking up the record on the user's repository)
                    Debug.Log($"Log In Successful! Welcome, {AtpLogin.Handle}.");
                    LoginComplete();
                    break;
                }
                case AtProtoLoginState.Failed: {
                    // TODO: Inform the user that authentication failed
                    Debug.Log($"Log In Failed!\n{AtpLogin.LastError}");
                    // Re-enable authenticate button
                    authenticateButton.interactable = true;
                    break;
                }
                default: {
                    Debug.LogError($"ATProtoLogin.State was not \"{AtProtoLoginState.Authenticated}\" nor \"{AtProtoLoginState.Failed}\"?!\n" +
                                   $"State was \"{AtpLogin.State}\".");
                    authenticateButton.interactable = true;
                    break;
                }
            }
        }

        public void LoginComplete() {
            _ = new GameObject(AtpLogin.Handle);
        }

    }

}