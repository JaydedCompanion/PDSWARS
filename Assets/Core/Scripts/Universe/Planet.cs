using AtProtoUnity;
using UnityEditor;
using UnityEngine;

namespace PDSWARS.Core.Universe {

    [RequireComponent(typeof(AtProtoLogin))]
    // ReSharper disable once InconsistentNaming
    public class Planet : MonoBehaviour {

        public string address;

        private AtProtoLogin _login;
        private AtProtoReader _reader;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start() {
        }

        // Update is called once per frame
        void Update() { }

        [MenuItem("ATProto/Test OAuth")]
        public static void TestOAuth() {
            AtProtoLogin login = new GameObject("AtProtoLoginTester", typeof(AtProtoLogin)).GetComponent<AtProtoLogin>();
            login.Handle = "@jayde.callejas.xyz";
            login.LoginMethod = AtProtoLoginMethod.OAuth;
            login.Login();
            Debug.Log(login.State);
        }

    }

}