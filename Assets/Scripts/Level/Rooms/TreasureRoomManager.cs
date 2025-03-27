using UnityEngine;

namespace Level.Rooms
{
    public class TreasureRoomManager : MonoBehaviour
    {
        [SerializeField] private GameObject dummyPrefab;
        private void Awake()
        {
            Instantiate(dummyPrefab, transform);
        }
    }
}