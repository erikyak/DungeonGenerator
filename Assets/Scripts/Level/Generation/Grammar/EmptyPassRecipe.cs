using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Composites;
using Level.Generation.Graph;
using Level.Generation.Support;

namespace Level.Generation.Grammar
{
    public class EmptyPassRecipe : RecipeTemplate
    {
        private readonly ZoneType _targetZoneType;
        
        public EmptyPassRecipe(ZoneType targetType)
        {
            _targetZoneType = targetType;
        }
        
        public ZoneType TargetZoneType => _targetZoneType;
        public float SelectionWeight => 0.01f;
        
        public bool SupportsInterfaceCount(int n) => n >= 1 && n <= 4;

        public bool CanApplyWithBudget(Budget budget, int remainingSameTypeZones)
        {
            int fairShare = budget.GetRemaining(Room.RoomType.Fight) / Mathf.Max(1, remainingSameTypeZones);
            return fairShare >= 1;
        }

        public Dictionary<Room.RoomType, int> GetMinRoomCounts()
        {
            return new Dictionary<Room.RoomType, int> { { Room.RoomType.Fight, 1 } };
        }

        public Composite Apply(int desiredInterfaces, Budget budget, RoomPools pools, SeedManager seed, int remainingSameTypeZones)
        {
            int minDoors = Mathf.Max(desiredInterfaces, 1);
            var footprint = pools.PickAny(Room.RoomType.Fight, minDoors, "", seed);
            if (footprint == null) return null;
            
            var composite = new Composite();
            composite.structureType = CompositeType.Tree;
            
            var node = new CompositeNode(0, Room.RoomType.Fight);
            node.assignedFootprint = footprint;
            composite.AddNode(node);
            
            for (int i = 0; i < desiredInterfaces; i++)
                composite.MarkAsInterface(node, i);
            
            budget.Consume(Room.RoomType.Fight, 1);
            return composite;
        }
    }
}