using System.Collections.Generic;
using System.Linq;
using Level.Generation.Graph;
using Level.Generation.Support;
using UnityEngine;

namespace Level.Generation.Grammar
{
    public class RecipeGrammar
    {
        private readonly List<RecipeTemplate> _templates = new List<RecipeTemplate>();
        private readonly List<RecipeTemplate> _fallbacks = new List<RecipeTemplate>();
        
        public void RegisterFromConfig(List<CompositeRecipe> recipes)
        {
            if (recipes != null)
            {
                foreach (var recipe in recipes)
                {
                    if (recipe == null) continue;
                    _templates.Add(new CompositeRecipeInterpreter(recipe));
                }
            }
            
            RegisterFallbacks();
        }
        
        private void RegisterFallbacks()
        {
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.Hall));
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.Fight));
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.Treasure));
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.Shop));
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.SecretRoom));
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.RestArea));
            _fallbacks.Add(new EmptyPassRecipe(ZoneType.NpcRoom));
        }
        
        public IEnumerable<RecipeTemplate> AllTemplates()
        {
            foreach (var t in _templates) yield return t;
            foreach (var f in _fallbacks) yield return f;
        }
        
        public IEnumerable<RecipeTemplate> UserTemplates()
        {
            return _templates;
        }
        
        public IEnumerable<RecipeTemplate> GetTemplatesForZone(Zone zone)
        {
            var interfaces = zone.InterfaceCount();
            var primary = _templates.Where(t => 
                t.TargetZoneType == zone.type && 
                t.SupportsInterfaceCount(interfaces));
            var fallback = _fallbacks.Where(t => 
                t.TargetZoneType == zone.type && 
                t.SupportsInterfaceCount(interfaces));
            return primary.Concat(fallback);
        }
        
        public RecipeTemplate SelectFromCandidates(List<RecipeTemplate> candidates, Zone zone, Budget budget, SeedManager seed, int remainingSameTypeZones)
        {
            var affordable = candidates.Where(t => t.CanApplyWithBudget(budget, remainingSameTypeZones)).ToList();
            if (affordable.Count == 0) return null;

            var user = affordable.Where(t => !(t is EmptyPassRecipe)).ToList();
            var pool = user.Count > 0 ? user : affordable;

            float totalWeight = pool.Sum(t => t.SelectionWeight);
            float pick = seed.NextFloat() * totalWeight;
            float accum = 0;
            foreach (var t in pool)
            {
                accum += t.SelectionWeight;
                if (pick <= accum) return t;
            }
            return pool[pool.Count - 1];
        }

        public RecipeTemplate SelectTemplate(Zone zone, Budget budget, SeedManager seed, int remainingSameTypeZones)
        {
            var candidates = GetTemplatesForZone(zone).ToList();
            return SelectFromCandidates(candidates, zone, budget, seed, remainingSameTypeZones);
        }
    }
}