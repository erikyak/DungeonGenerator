using System.Collections.Generic;
using Level.Generation.Old;

namespace Level.Generation.Support
{
    public class Budget
    {
        private readonly Dictionary<Room.RoomType, int> _remaining = new Dictionary<Room.RoomType, int>();
        private readonly Dictionary<Room.RoomType, int> _initial = new Dictionary<Room.RoomType, int>();
        
        public IReadOnlyDictionary<Room.RoomType, int> Remaining => _remaining;
        public IReadOnlyDictionary<Room.RoomType, int> Initial => _initial;
        
        public void SetInitial(Room.RoomType type, int count)
        {
            _initial[type] = count;
            _remaining[type] = count;
        }
        
        public bool CanAfford(Room.RoomType type, int count)
        {
            return _remaining.TryGetValue(type, out var available) && available >= count;
        }
        
        public bool Consume(Room.RoomType type, int count)
        {
            if (!CanAfford(type, count)) return false;
            _remaining[type] -= count;
            return true;
        }
        
        public void Refund(Room.RoomType type, int count)
        {
            if (!_remaining.ContainsKey(type)) _remaining[type] = 0;
            _remaining[type] += count;
        }
        
        public int GetRemaining(Room.RoomType type)
        {
            return _remaining.TryGetValue(type, out var val) ? val : 0;
        }
        
        public int GetInitial(Room.RoomType type)
        {
            return _initial.TryGetValue(type, out var val) ? val : 0;
        }
    }
}