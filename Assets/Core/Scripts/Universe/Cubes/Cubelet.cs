using System;
using UnityEngine;

namespace PDSWARS.Core.Universe.Cubes {

    public class Cubelet : MonoBehaviour {

        [Serializable]
        public class Data {
            public Cubelet Instance {
                get{
                    if (_instance != null) return _instance;
                    Debug.LogWarning("Cubelet instance does not exist! Instantiating GameObject...");
                    _instance = Instantiate();
                    return _instance;
                }
            }
            private Cubelet _instance;

            [field:SerializeField]
            public Vector3Int Position { get; private set; }
            [field:SerializeField]
            public Cube Parent { get; private set; }
            
            public Data(Vector3Int pos, Cube parent) {
                Position = pos;
                Parent = parent;
            }

            /// <summary>
            /// Instantiates a cubelet prefab to represent this cubelet data in the scene.
            /// </summary>
            /// <returns>The Cubelet (MonoBehaviour) instance that has been added to the scene.</returns>
            public Cubelet Instantiate() {
                if (!_instance) {
                    _instance = UnityEngine.Object.Instantiate(Prefab, Parent.transform);
                    _instance.data = this;
                    _instance.transform.localPosition = Position * _instance.size;
                    return _instance;
                };
                Debug.LogWarning("Cubelet instance already existed! Returning existing instance instead of creating a new one...");
                return _instance;
            }
        }

        public static Cubelet Prefab {
            get {
                if (!_cubelet) {
                    _cubelet = Resources.Load<Cubelet>("Prefabs/Cubelet");
                }
                return _cubelet;
            }
        }
        private static Cubelet _cubelet;

        public Vector3Int Position => data.Position;
        public Data data;
        public int size = 1;

    }

}