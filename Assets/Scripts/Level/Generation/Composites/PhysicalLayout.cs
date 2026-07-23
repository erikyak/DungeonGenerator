using UnityEngine;
using Level.Generation.Support;

namespace Level.Generation.Composites
{
    public class PhysicalLayout
    {
        private const int MaxIterations = 200;
        private const float RepulsionStrength = 1.0f;
        private const float AttractionStrength = 0.15f;
        private const float DesiredEdgeLength = 8f;
        private const float Padding = 2f;
        private const float Damping = 0.1f;
        private const float ConvergenceThreshold = 0.5f;
        
        public bool ArrangeRooms(Composite composite, SeedManager seed)
        {
            if (composite.nodes.Count == 0) return true;
            
            InitializePositions(composite, seed);
            
            for (int iter = 0; iter < MaxIterations; iter++)
            {
                var forces = new Vector2[composite.nodes.Count];
                
                ApplyRepulsion(composite, forces);
                ApplyAttraction(composite, forces);
                
                float totalMovement = ApplyForces(composite, forces);
                if (totalMovement < ConvergenceThreshold) break;
            }
            
            return RoundAndNormalize(composite);
        }
        
        private void InitializePositions(Composite composite, SeedManager seed)
        {
            float averageSize = 0;
            foreach (var node in composite.nodes)
            {
                averageSize += (node.assignedFootprint.size.x + node.assignedFootprint.size.y) * 0.5f;
            }
            averageSize /= composite.nodes.Count;
            
            float radius = Mathf.Sqrt(composite.nodes.Count) * averageSize * 2f;
            
            foreach (var node in composite.nodes)
            {
                float angle = seed.NextFloat() * Mathf.PI * 2;
                float r = seed.NextFloat() * radius;
                node.floatPosition = new Vector2(Mathf.Cos(angle) * r, Mathf.Sin(angle) * r);
            }
        }
        
        private void ApplyRepulsion(Composite composite, Vector2[] forces)
        {
            for (int i = 0; i < composite.nodes.Count; i++)
            {
                for (int j = i + 1; j < composite.nodes.Count; j++)
                {
                    var nodeA = composite.nodes[i];
                    var nodeB = composite.nodes[j];
                    
                    var sizeA = new Vector2(nodeA.assignedFootprint.size.x, nodeA.assignedFootprint.size.y);
                    var sizeB = new Vector2(nodeB.assignedFootprint.size.x, nodeB.assignedFootprint.size.y);
                    
                    var halfA = sizeA * 0.5f + Vector2.one * Padding;
                    var halfB = sizeB * 0.5f + Vector2.one * Padding;
                    
                    var delta = nodeA.floatPosition - nodeB.floatPosition;
                    var absDelta = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
                    var overlap = (halfA + halfB) - absDelta;
                    
                    if (overlap.x > 0 && overlap.y > 0)
                    {
                        Vector2 direction;
                        float magnitude;
                        if (overlap.x < overlap.y)
                        {
                            direction = new Vector2(Mathf.Sign(delta.x), 0);
                            magnitude = overlap.x;
                        }
                        else
                        {
                            direction = new Vector2(0, Mathf.Sign(delta.y));
                            magnitude = overlap.y;
                        }
                        if (direction.magnitude < 0.01f) direction = new Vector2(1, 0);
                        
                        var force = direction * magnitude * RepulsionStrength;
                        forces[i] += force;
                        forces[j] -= force;
                    }
                }
            }
        }
        
        private void ApplyAttraction(Composite composite, Vector2[] forces)
        {
            for (int i = 0; i < composite.edges.Count; i++)
            {
                var edge = composite.edges[i];
                int idxA = composite.nodes.IndexOf(edge.a);
                int idxB = composite.nodes.IndexOf(edge.b);
                
                var delta = edge.b.floatPosition - edge.a.floatPosition;
                float dist = delta.magnitude;
                if (dist < 0.01f) continue;
                
                if (dist > DesiredEdgeLength)
                {
                    var direction = delta / dist;
                    var force = direction * (dist - DesiredEdgeLength) * AttractionStrength;
                    forces[idxA] += force;
                    forces[idxB] -= force;
                }
            }
        }
        
        private float ApplyForces(Composite composite, Vector2[] forces)
        {
            float totalMovement = 0;
            for (int i = 0; i < composite.nodes.Count; i++)
            {
                var displacement = forces[i] * Damping;
                composite.nodes[i].floatPosition += displacement;
                totalMovement += displacement.magnitude;
            }
            return totalMovement;
        }
        
        private bool RoundAndNormalize(Composite composite)
        {
            Vector2Int min = new Vector2Int(int.MaxValue, int.MaxValue);
            
            foreach (var node in composite.nodes)
            {
                var pos = new Vector2Int(
                    Mathf.RoundToInt(node.floatPosition.x - node.assignedFootprint.size.x * 0.5f),
                    Mathf.RoundToInt(node.floatPosition.y - node.assignedFootprint.size.y * 0.5f)
                );
                node.position = pos;
                
                if (pos.x < min.x) min.x = pos.x;
                if (pos.y < min.y) min.y = pos.y;
            }
            
            foreach (var node in composite.nodes)
            {
                node.position = node.position.Value - min;
            }
            
            var checker = new LocalCollisionChecker();
            foreach (var node in composite.nodes)
            {
                if (!checker.IsAreaFree(node.position.Value, node.assignedFootprint.size)) return false;
                checker.OccupyRoom(node.position.Value, node.assignedFootprint.size);
            }
            
            return true;
        }
    }
}