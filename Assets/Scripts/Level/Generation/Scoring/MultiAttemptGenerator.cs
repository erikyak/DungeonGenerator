using System.Collections.Generic;
using UnityEngine;
using Level.Generation.Corridors;
using Level.Generation.Grammar;
using Level.Generation.Graph;
using Level.Generation.Layout;
using Level.Generation.Support;

namespace Level.Generation.Scoring
{
    public class MultiAttemptGenerator
    {
        public class Result
        {
            public AssembledLevel level;
            public LevelMetrics metrics;
            public float score;
            public int seed;
        }

        public List<Result> allAttempts = new List<Result>();

        public Result GenerateBest(GenerationConfig config, ScoringFunction scoring, int attemptCount)
        {
            allAttempts.Clear();
            if (config == null || attemptCount <= 0) return null;

            var rng = new System.Random();
            Result best = null;

            for (int i = 0; i < attemptCount; i++)
            {
                int seedValue = config.seed >= 0 ? config.seed + i : rng.Next();
                var attempt = RunSingle(config, seedValue);
                if (attempt == null) continue;
                attempt.metrics = scoring.ComputeMetrics(attempt.level);
                attempt.score = scoring.Score(attempt.metrics);
                allAttempts.Add(attempt);
                if (best == null || attempt.score > best.score) best = attempt;
            }
            return best;
        }

        private Result RunSingle(GenerationConfig config, int seedValue)
        {
            var seed = new SeedManager(seedValue);
            var pools = new RoomPools();
            var budget = new Budget();
            foreach (var req in config.roomRequirements)
            {
                budget.SetInitial(req.type, req.maximumRequiredCount);
                if (req.prefabs != null)
                    foreach (var prefab in req.prefabs)
                        pools.AddPrefab(prefab);
            }

            var grammar = new RecipeGrammar();
            grammar.RegisterFromConfig(config.recipes);

            var builder = new MacroGraphBuilder();
            var graph = builder.Build(config.roomRequirements, grammar, seed);
            if (graph == null) return null;

            var zoneInjector = new ZoneInjector
            {
                secretRoomChance = config.secretRoomChance,
                restAreaChance = config.restAreaChance
            };
            zoneInjector.Inject(graph, grammar, seed);

            var corridorInjector = new CorridorInjector
            {
                maxLoopEdges = config.maxLoopEdges,
                maxShortcutEdges = config.maxShortcutEdges,
                maxDeepShortcutEdges = config.maxDeepShortcutEdges
            };
            corridorInjector.Inject(graph, grammar, seed);

            var expander = new ZoneExpander();
            if (!expander.Expand(graph, grammar, budget, pools, seed)) return null;

            var segmentFootprints = new List<CorridorSegmentFootprint>();
            if (config.corridorSegmentPrefabs != null)
                foreach (var prefab in config.corridorSegmentPrefabs)
                {
                    var fp = CorridorSegmentFootprint.FromPrefab(prefab);
                    if (fp != null) segmentFootprints.Add(fp);
                }
            if (segmentFootprints.Count == 0) return null;

            var assembler = new MacroAssembler();
            var assembled = assembler.Assemble(graph, segmentFootprints, seed);
            if (assembled == null) return null;

            return new Result { level = assembled, seed = seedValue };
        }
    }
}
