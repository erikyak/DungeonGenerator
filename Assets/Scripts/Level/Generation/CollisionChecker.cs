using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Level.Generation
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
            return _placedBounds.Any(newBounds.Intersects);
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