using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Level.Generation.Old
{
    public class CollisionChecker
    {
        // A simple list of bounds from all already-placed objects.
        private readonly List<Bounds> _placedBounds = new();

        /// <summary>
        ///     Checks if the provided bounds intersect any previously registered bounds.
        /// </summary>
        public bool CheckCollision(Bounds newBounds)
        {
            // Optionally, warn if bounds are zero (can be handled separately if needed)
            if (newBounds.size == Vector3.zero)
            {
                Debug.LogWarning("CheckCollision: Bounds size is zero.");
                return false;
            }

            return _placedBounds.Any(bounds => newBounds.Intersects(bounds));
        }

        /// <summary>
        ///     Registers new bounds in the collision checker.
        /// </summary>
        public void Register(Bounds newBounds)
        {
            _placedBounds.Add(newBounds);
        }

        /// <summary>
        ///     Clears all stored bounds.
        /// </summary>
        public void Clear()
        {
            _placedBounds.Clear();
        }
    }
}