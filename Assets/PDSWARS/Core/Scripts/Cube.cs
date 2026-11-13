using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PDSWARS.Core {

    public class Cube : MonoBehaviour {

        public int volume;

        private Dictionary<Vector3Int, Cubelet.Data> _cubelets = new();

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start() { }

        // Update is called once per frame
        void Update() { }

        /// <summary>
        /// Generates a cube with the given <paramref name="volume"/> and initializes exposed cubelets.
        /// </summary>
        /// <param name="volume">The number of cubelets that will comprise the resulting cube.</param>
        /// <returns>Volume represented by Vector3Ints.</returns>
        private static Dictionary<Vector3Int, Cubelet.Data> GenerateCube(int volume) {
            // Calculate the largest side length for a cube that does not exceed the given volume
            // Remember, inverse powers are roots, negative powers are the inverse after exponentiating (yes that is a real word)
            int floorSideLength = Mathf.FloorToInt(Mathf.Pow(volume, 1f / 3f));
            int overflowCount = volume - floorSideLength;
            int startingCoordinate = -Mathf.FloorToInt(floorSideLength / 2f);
            // Create and fill a volume with the largest cube that does not exceed the volume
            Dictionary<Vector3Int, Cubelet.Data> cubelets = new();
            // Also create a "skin" of cubelets,
            // which will be moved to the main cubelets dictionary to compensate for overflow
            Dictionary<Vector3Int, Cubelet.Data> overflow = new();
            for (int x = 0 - 1; x < floorSideLength + 1; x++)
            for (int y = 0 - 1; y < floorSideLength + 1; y++)
            for (int z = 0 - 1; z < floorSideLength + 1; z++) {
                Cubelet.Data toAdd = null;
                // Initialize cubelet at once if it is exposed, or part of the overflow
                if (
                    x <= 0 || x >= floorSideLength - 1 ||
                    y <= 0 || y >= floorSideLength - 1 ||
                    z <= 0 || z >= floorSideLength - 1)
                    toAdd = new Cubelet.Data();
                // Populate cube or overflow
                if (
                    x >= 0 && x <= floorSideLength - 1 &&
                    y >= 0 && y <= floorSideLength - 1 &&
                    z >= 0 && z <= floorSideLength - 1)
                    // If all coordinates are within cube dimensions, fill cube
                    cubelets.Add(new Vector3Int(x - startingCoordinate, y - startingCoordinate, z - startingCoordinate), toAdd);
                else
                    // Otherwise, populate overflow
                    overflow.Add(new Vector3Int(x - startingCoordinate, y - startingCoordinate, z - startingCoordinate), toAdd);
            }
            // Take the overflowCount number of cubelets from the overflow dictionary and move them to the cube
            for (int i = 0; i < overflowCount; i++) {
                Cubelet.Data toAdd = overflow.ElementAt(Random.Range(0, overflow.Count)).Value;
                Vector3Int at = overflow.ElementAt(Random.Range(0, overflow.Count)).Key;
                cubelets.Add(at, toAdd);
            }
            return cubelets;
        }

    }

}