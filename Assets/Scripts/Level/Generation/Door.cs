using System;
using UnityEngine;

namespace Level.Generation
{
    public class Door : MonoBehaviour
    {
        public enum Direction
        {
            North,
            South,
            East,
            West
        }

        [NonSerialized] public Vector3 offset;
        public Direction direction;
        public bool isConnected;

        private void Awake()
        {
            offset = direction switch
            {
                Direction.North => Vector2.up * transform.lossyScale.y,
                Direction.South => Vector2.down * transform.lossyScale.y,
                Direction.East => Vector2.right * transform.lossyScale.x,
                Direction.West => Vector2.left * transform.lossyScale.x,
                _ => Vector2.zero
            };
        }
    }
}