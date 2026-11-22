using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Serializing;
using UnityEngine;

namespace PDSWARS.Universe.Cubes {

    public class Cube : NetworkBehaviour {

        public int volume;

        private readonly SyncDictionary<Vector3Int, Cubelet.Data> _cubelets = new();
        
        public override void OnStartServer() {
            Initialize();
        }

        [ServerRpc]
        public void Initialize() {
            for (int i = transform.childCount - 1; i >= 0; i--)
                if (Application.isPlaying)
                    Destroy(transform.GetChild(i).gameObject);
                else
                    DestroyImmediate(transform.GetChild(i).gameObject);
            _cubelets.Collection = GenerateCube(volume, this);
            var writer = new PooledWriter();
            foreach (KeyValuePair<Vector3Int, Cubelet.Data> pair in _cubelets.Collection)
                if (pair.Value.Initialized) {
                    writer.Write(typeof(Cubelet.Data));
                    pair.Value.Instantiate();
                }
            // Set the whole thing dirty, since we just generated a brand-new cube
            _cubelets.DirtyAll();
        }

        /// <summary>
        /// Generates a cube with the given <paramref name="volume"/> and initializes exposed cubelets.
        /// </summary>
        /// <param name="volume">The number of cubelets that will comprise the resulting cube.</param>
        /// <param name="parent">The cube that cubelets should be parented to.</param>
        /// <returns>Volume represented by Vector3Ints.</returns>
        private static Dictionary<Vector3Int, Cubelet.Data> GenerateCube(int volume, Cube parent) {
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
                Cubelet.Data toAdd = default;
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
            return cubelets;
        }

        private static bool IsEnd(int value, int startInclusive, int endExclusive) {
            return value == startInclusive || value == endExclusive - 1;
        }

    }

}