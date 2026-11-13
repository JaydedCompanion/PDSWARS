using System;
using UnityEngine;

namespace PDSWARS.Core {

    public class Cubelet : MonoBehaviour {

        [Serializable]
        public class Data {
            public Cubelet correspondingCubelet;
            public bool Active {
                get;
                private set;
            }
            public static implicit operator bool(Cubelet.Data cubelet) {
                return cubelet.Active;
            }
        }
        
        public Data data;

    }

}