using System;
using System.Collections.Generic;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Object.Synchronizing.Internal;
using UnityEngine;

namespace PDSWARS.Universe.Cubes {

    public class Cubelet : NetworkBehaviour {
        
        public class DataSync : SyncBase, ICustomSync {
            public object GetSerializedType() {
                return typeof(Data);
            }
        }
        
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

            /// <summary>
            /// Whether this struct's constructor has been called and data has been assigned
            /// </summary>
            public bool Initialized { get; private set; }
            [field:SerializeField]
            public Vector3Int Position { get; private set; }
            [field:SerializeField]
            public Cube Parent { get; private set; }

            /// <summary>
            /// Collection of adjacent cubelets
            /// </summary>
            public List<Data> Neighbours {
                get {
                    if (_staleNeighbours || _neighbours == null) 
                        UpdateNeighbours();
                    return _neighbours;
                }
            }
            private List<Data> _neighbours;
            /// <summary>
            /// Collection of vacant adjacent indices
            /// </summary>
            public List<Vector3Int> Vacancies {
                get {
                    if (_staleNeighbours || _vacancies == null)
                        UpdateNeighbours();
                    return _vacancies;
                }
            }
            private List<Vector3Int> _vacancies;

            private void UpdateNeighbours() {
                _neighbours ??= new();
                _neighbours.Clear();
                _vacancies ??= new();
                _vacancies.Clear();
                Data neighbour;
                Vector3Int pos;
                if ((neighbour = Parent.GetCubelet(pos = new(Position.x - 1, Position.y, Position.z)))!= null)
                    _neighbours.Add(neighbour); else _vacancies.Add(pos);
                if ((neighbour = Parent.GetCubelet(pos = new(Position.x + 1, Position.y, Position.z)))!= null)
                    _neighbours.Add(neighbour); else _vacancies.Add(pos);
                if ((neighbour = Parent.GetCubelet(pos = new(Position.x, Position.y - 1, Position.z)))!= null)
                    _neighbours.Add(neighbour); else _vacancies.Add(pos);
                if ((neighbour = Parent.GetCubelet(pos = new(Position.x, Position.y + 1, Position.z)))!= null)
                    _neighbours.Add(neighbour); else _vacancies.Add(pos);
                if ((neighbour = Parent.GetCubelet(pos = new(Position.x, Position.y, Position.z - 1)))!= null)
                    _neighbours.Add(neighbour); else _vacancies.Add(pos);
                if ((neighbour = Parent.GetCubelet(pos = new(Position.x, Position.y, Position.z + 1)))!= null)
                    _neighbours.Add(neighbour); else _vacancies.Add(pos);
                _staleNeighbours = false;
            }
            
            public void StaleNeighbours() {
                _staleNeighbours = true;
            }
            private bool _staleNeighbours;
            
            public Data(Vector3Int pos, Cube parent) {
                _staleNeighbours = true;
                Initialized = true;
                _instance = null;
                Position = pos;
                Parent = parent;
                _neighbours = new();
                _vacancies = new();
            }

            /// <summary>
            /// Checks whether any directly adjacent space is empty by looking up all sides on Parent Cube dictionary.
            /// </summary>
            /// <returns>Whether this cubelet is exposed.</returns>
            public bool IsExposed => Neighbours.Count == 6; // Can be determined by simply checking if there are as many neighbours as there are sides of a cube.

            /// <summary>
            /// Instantiates a cubelet prefab to represent this cubelet data in the scene.
            /// </summary>
            /// <returns>The Cubelet (MonoBehaviour) instance that has been added to the scene.</returns>
            public Cubelet Instantiate() {
                if (!_instance) {
                    _instance = UnityEngine.Object.Instantiate(Prefab, Parent.transform);
                    _instance.Position = Position;
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

        public Vector3Int Position { get; protected set; }
        public int size = 1;

    }

}