using System.Collections.Generic;
using System.Linq;
using FishNet;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using PDSWARS.GameState;
using UnityEngine;

namespace PDSWARS.Universe.Cubes {

    public class Cube : NetworkBehaviour {

        public int volume;

        private readonly SyncDictionary<Vector3Int, Cubelet.Data> _cubelets = new();
        private readonly SyncDictionary<Vector3Int, Cubelet.Data> _exposedCubelets = new();
        public string PdsEndpoint { get; private set; }

        public override void OnStartServer() {
            Debug.Log($"PDS Cube OnStartServer {PdsEndpoint}.");
            Initialize();
            InstantiateCubelets();
        }

        public Cubelet.Data GetCubelet(Vector3Int pos) {
            return _cubelets.GetValueOrDefault(pos);
        }

        [Server]
        public void Initialize() {
            // Clear residual cubelets from editor testing
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (Application.isPlaying) {
                    ServerManager.Despawn(transform.GetChild(i).gameObject);
                    Destroy(transform.GetChild(i).gameObject);
                }
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            // Generate cubelet volume

            Dictionary<Vector3Int, Cubelet.Data> exposed = new();
            _cubelets.Collection = GenerateCube(volume, this, exposed);
            // Set the whole thing dirty, since we just generated a brand-new cube
            _cubelets.DirtyAll();
            Debug.Log($"Initialized collection of {_cubelets.Count} cubelets.");
            _exposedCubelets.Collection = exposed;
            _exposedCubelets.DirtyAll();
        }

        /// <summary>
        /// Picks a random exposed cubelet, picks a random exposed vacancy, adds a new cubelet to the cubelets collection. If the new cubelet is exposed, it'll be instantiated, spawned and added to the exposed cubelets collection.
        /// </summary>
        /// <returns>The new cubelet data.</returns>
        [Server]
        public Cubelet.Data AddCubeletRandomly() {
            Cubelet.Data targetNeighbour = _exposedCubelets.Collection.ElementAt(Random.Range(0, _exposedCubelets.Count)).Value;
            Vector3Int targetLocation = targetNeighbour.Vacancies[Random.Range(0, targetNeighbour.Vacancies.Count)];
            Cubelet.Data newCubelet = new Cubelet.Data(targetLocation, this);
            _cubelets.Add(targetLocation, newCubelet);
            _cubelets.Dirty(targetLocation);
            foreach (var neighbour in _cubelets[targetLocation].Neighbours) {
                neighbour.StaleNeighbours();
                _cubelets.Dirty(neighbour.Position);
            }
            if (newCubelet.IsExposed) {
                _exposedCubelets.Add(newCubelet.Position, newCubelet);
                _exposedCubelets.Dirty(newCubelet.Position);
                newCubelet.Instantiate();
                ServerManager.Spawn(newCubelet.Instance);
            }
            return newCubelet;
        }

        /// <summary>
        /// Destroys, despawns, and removes <paramref name="cubelet"/> from the cubelets (and exposed cubelets, if applicable) collection.
        /// </summary>
        /// <param name="cubelet">The Cubelet to remove.</param>
        [Server]
        public void RemoveCubelet(Cubelet.Data cubelet) {
            foreach (var neighbour in cubelet.Neighbours) {
                neighbour.Instantiate();
                neighbour.StaleNeighbours();
            }
            ServerManager.Despawn(cubelet.Instance, DespawnType.Pool);
            Destroy(cubelet.Instance);
            _cubelets.Remove(cubelet.Position);
            _exposedCubelets.Remove(cubelet.Position);
        }
        
        /// <summary>
        /// Instantiates Cubelet prefab GameObjects according to Cubelet Data collection.
        /// </summary>
        [Server]
        public void InstantiateCubelets() {
            Debug.Log($"Instantiating collection of {_cubelets.Count} cubelets.");
            foreach (KeyValuePair<Vector3Int, Cubelet.Data> pair in _cubelets.Collection)
                if (pair.Value is { Initialized: true }) {
                    var instance = pair.Value.Instantiate();
                    InstanceFinder.ServerManager.Spawn(instance.gameObject);
                }
        }

        [Server]
        public static Cube New(string endpoint, Vector3 position) {
            Cube newCube = Instantiate(Resources.Load<Cube>("Prefabs/PDS"));
            var mngr = InstanceFinder.ServerManager;
            newCube.transform.position = position;
            newCube.PdsEndpoint = endpoint;
            newCube.gameObject.name = $"Cube {endpoint} (PDS)";
            mngr.Spawn(newCube.gameObject);
            return newCube;
        }

        /// <summary>
        /// Generates a cube with the given <paramref name="volume"/> and initializes exposed cubelets.
        /// </summary>
        /// <param name="volume">The number of cubelets that will comprise the resulting cube.</param>
        /// <param name="parent">The cube that cubelets should be parented to.</param>
        /// <param name="exposed">Collection of cubelets that are exposed</param>
        /// <returns>Volume represented by Vector3Ints.</returns>
        private static Dictionary<Vector3Int, Cubelet.Data> GenerateCube(int volume, Cube parent, Dictionary<Vector3Int, Cubelet.Data> exposed) {
            // Calculate the largest side length for a cube that does not exceed the given volume
            // Remember, inverse powers are roots, negative powers are the inverse after exponentiating (yes that is a real word)
            int floorSideLength = Mathf.FloorToInt(Mathf.Pow(volume, 1f / 3f));
            int overflowCount = volume - (floorSideLength * floorSideLength * floorSideLength);
            int startingCoordinate = -Mathf.FloorToInt(floorSideLength / 2f);
            // Create and fill a volume with the largest cube that does not exceed the volume
            Dictionary<Vector3Int, Cubelet.Data> cubelets = new();
            // Also create a "skin" of cubelets,
            // which will be moved to the main cubelets dictionary to compensate for overflow
            Dictionary<Vector3Int, Cubelet.Data> overflow = new();
            for (int x = 0 - 1; x < floorSideLength + 1; x++)
            for (int y = 0 - 1; y < floorSideLength + 1; y++)
            for (int z = 0 - 1; z < floorSideLength + 1; z++) {
                // Skip cube edges to avoid overhanging cubes on overflow layer
                if (IsEnd(x, -1, floorSideLength + 1) && IsEnd(y, -1, floorSideLength + 1) ||
                    IsEnd(y, -1, floorSideLength + 1) && IsEnd(z, -1, floorSideLength + 1) ||
                    IsEnd(x, -1, floorSideLength + 1) && IsEnd(z, -1, floorSideLength + 1)) continue;
                Cubelet.Data toAdd = null;
                Vector3Int pos = new Vector3Int(x + startingCoordinate, y + startingCoordinate, z + startingCoordinate);
                // Initialize cubelet at once if it is exposed, or part of the overflow
                if (
                    x <= 0 || x >= floorSideLength - 1 ||
                    y <= 0 || y >= floorSideLength - 1 ||
                    z <= 0 || z >= floorSideLength - 1)
                    toAdd = new Cubelet.Data(pos, parent);
                // Populate cube or overflow
                if (
                    x >= 0 && x <= floorSideLength - 1 &&
                    y >= 0 && y <= floorSideLength - 1 &&
                    z >= 0 && z <= floorSideLength - 1)
                    // If all coordinates are within cube dimensions, fill cube
                    cubelets.Add(pos, toAdd);
                else
                    // Otherwise, populate overflow
                    overflow.Add(pos, toAdd);
            }
            // Take the overflowCount number of cubelets from the overflow dictionary and move them to the cube
            for (int i = 0; i < overflowCount; i++) {
                int randomIndex = Random.Range(0, overflow.Count);
                KeyValuePair<Vector3Int, Cubelet.Data> pair = overflow.ElementAt(randomIndex);
                overflow.Remove(pair.Key);
                cubelets.Add(pair.Key, pair.Value);
            }
            // Once all the cubelets have been initialized, check how many are exposed and add them to the exposed collection
            if (exposed != null) {
                exposed.Clear();
                foreach (var cubeletsValue in cubelets) {
                    if (IsExposed(cubeletsValue.Key, cubelets)) {
                        exposed.Add(cubeletsValue.Key, cubeletsValue.Value);
                    }
                }
            }
            //Breaks before reaching this break point?!
            return cubelets;
        }

        private static bool IsEnd(int value, int startInclusive, int endExclusive) {
            return value == startInclusive || value == endExclusive - 1;
        }

        private static bool IsExposed(Vector3Int pos, Dictionary<Vector3Int, Cubelet.Data> collection) {
            if (!collection.ContainsKey(new(pos.x - 1, pos.y, pos.z)))
                return true;
            if (!collection.ContainsKey(new(pos.x + 1, pos.y, pos.z)))
                return true;
            if (!collection.ContainsKey(new(pos.x, pos.y - 1, pos.z)))
                return true;
            if (!collection.ContainsKey(new(pos.x, pos.y + 1, pos.z)))
                return true;
            if (!collection.ContainsKey(new(pos.x, pos.y, pos.z - 1)))
                return true;
            if (!collection.ContainsKey(new(pos.x, pos.y, pos.z + 1)))
                return true;
            return false;
        }

    }

}