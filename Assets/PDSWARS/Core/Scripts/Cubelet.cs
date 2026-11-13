using UnityEngine;

namespace PDSWARS.Core {

    public class Cubelet : MonoBehaviour {

        public bool Active {
            get;
            private set;
        }

        public static implicit operator bool(Cubelet cubelet) {
            return cubelet.Active;
        }

    }

}