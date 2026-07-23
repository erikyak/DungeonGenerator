namespace Level.Generation.Graph
{
    public enum InjectionCategory
    {
        None,
        Loop,
        Shortcut,
        DeepShortcut
    }
    
    public class ZoneEdge
    {
        public Zone a;
        public Zone b;
        public bool isCycleEdge;
        public bool isInjected;
        public InjectionCategory category = InjectionCategory.None;
        
        public ZoneEdge(Zone a, Zone b, bool isCycleEdge = false)
        {
            this.a = a;
            this.b = b;
            this.isCycleEdge = isCycleEdge;
        }
        
        public Zone Other(Zone from)
        {
            if (from == a) return b;
            if (from == b) return a;
            throw new System.ArgumentException($"Zone {from} is not incident to this edge");
        }
        
        public override string ToString() => $"Edge({a}--{b})";
    }
}