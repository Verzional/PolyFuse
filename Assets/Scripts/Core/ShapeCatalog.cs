using System.Collections.Generic;
using UnityEngine;

namespace PolyFuse.Core
{
    public static class ShapeCatalog
    {
        // Visual Palette for Polyforms
        public static readonly Color ShardColor   = new Color(0.20f, 0.85f, 0.95f, 1f); // Vibrant Cyan
        public static readonly Color BladeColor   = new Color(0.98f, 0.72f, 0.15f, 1f); // Warm Amber
        public static readonly Color CleaverColor = new Color(0.96f, 0.35f, 0.42f, 1f); // Vibrant Coral/Ruby
        public static readonly Color CoreColor    = new Color(0.75f, 0.38f, 0.98f, 1f); // Neon Violet

        private static List<ShapeDefinition> _allShapes;
        private static List<ShapeDefinition> _shardShapes;
        private static List<ShapeDefinition> _bladeShapes;
        private static List<ShapeDefinition> _cleaverShapes;
        private static List<ShapeDefinition> _coreShapes;

        static ShapeCatalog()
        {
            InitializeShapes();
        }

        public static IReadOnlyList<ShapeDefinition> AllShapes => _allShapes;
        public static IReadOnlyList<ShapeDefinition> Shards => _shardShapes;
        public static IReadOnlyList<ShapeDefinition> Blades => _bladeShapes;
        public static IReadOnlyList<ShapeDefinition> Cleavers => _cleaverShapes;
        public static IReadOnlyList<ShapeDefinition> Cores => _coreShapes;

        private static void InitializeShapes()
        {
            _allShapes = new List<ShapeDefinition>();
            _shardShapes = new List<ShapeDefinition>();
            _bladeShapes = new List<ShapeDefinition>();
            _cleaverShapes = new List<ShapeDefinition>();
            _coreShapes = new List<ShapeDefinition>();

            // 1. THE SHARD (1 Unit)
            AddShape(new ShapeDefinition("shard_up", "Shard (Up)", ShapeCategory.Shard, true, 
                new[] { new GridCoord(0, 0) }, ShardColor), _shardShapes);

            AddShape(new ShapeDefinition("shard_down", "Shard (Down)", ShapeCategory.Shard, false, 
                new[] { new GridCoord(0, 0) }, ShardColor), _shardShapes);

            // 2. THE BLADE (2 Units - True Diamond / Rhombus sharing an edge)
            // Vertical Diamond (base-to-base)
            AddShape(new ShapeDefinition("blade_vert_up", "Blade Vertical (Up)", ShapeCategory.Blade, true,
                new[] { new GridCoord(0, 0), new GridCoord(-1, 0) }, BladeColor), _bladeShapes);
            AddShape(new ShapeDefinition("blade_vert_down", "Blade Vertical (Down)", ShapeCategory.Blade, false,
                new[] { new GridCoord(0, 0), new GridCoord(1, 0) }, BladeColor), _bladeShapes);

            // +60° Slanted Diamond
            AddShape(new ShapeDefinition("blade_slash_up", "Blade Slash (Up)", ShapeCategory.Blade, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, 1) }, BladeColor), _bladeShapes);
            AddShape(new ShapeDefinition("blade_slash_down", "Blade Slash (Down)", ShapeCategory.Blade, false,
                new[] { new GridCoord(0, 0), new GridCoord(0, 1) }, BladeColor), _bladeShapes);

            // 120° Slanted Diamond
            AddShape(new ShapeDefinition("blade_backslash_up", "Blade Backslash (Up)", ShapeCategory.Blade, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, -1) }, BladeColor), _bladeShapes);
            AddShape(new ShapeDefinition("blade_backslash_down", "Blade Backslash (Down)", ShapeCategory.Blade, false,
                new[] { new GridCoord(0, 0), new GridCoord(0, -1) }, BladeColor), _bladeShapes);

            // 3. THE CLEAVER (3 Units - Trapezoid / 3-Triangle Strip)
            // Horizontal Strip (Up-Down-Up)
            AddShape(new ShapeDefinition("cleaver_horiz_up", "Cleaver Horizontal (Up)", ShapeCategory.Cleaver, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(0, 2) }, CleaverColor), _cleaverShapes);

            // Horizontal Strip (Down-Up-Down)
            AddShape(new ShapeDefinition("cleaver_horiz_down", "Cleaver Horizontal (Down)", ShapeCategory.Cleaver, false,
                new[] { new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(0, 2) }, CleaverColor), _cleaverShapes);

            // +60° Diagonal Strip
            AddShape(new ShapeDefinition("cleaver_slash_up", "Cleaver Slash (Up)", ShapeCategory.Cleaver, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(1, 1) }, CleaverColor), _cleaverShapes);
            AddShape(new ShapeDefinition("cleaver_slash_down", "Cleaver Slash (Down)", ShapeCategory.Cleaver, false,
                new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(1, 1) }, CleaverColor), _cleaverShapes);

            // 120° Diagonal Strip
            AddShape(new ShapeDefinition("cleaver_backslash_up", "Cleaver Backslash (Up)", ShapeCategory.Cleaver, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, -1), new GridCoord(1, -1) }, CleaverColor), _cleaverShapes);
            AddShape(new ShapeDefinition("cleaver_backslash_down", "Cleaver Backslash (Down)", ShapeCategory.Cleaver, false,
                new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(1, -1) }, CleaverColor), _cleaverShapes);

            // 4. THE CORE (6 Units - Regular Hexagon)
            AddShape(new ShapeDefinition("core_hex_up", "Hex Core (Up)", ShapeCategory.Core, true,
                new[] {
                    new GridCoord(0, -1), new GridCoord(0, 0), new GridCoord(0, 1),
                    new GridCoord(1, -1), new GridCoord(1, 0), new GridCoord(1, 1)
                }, CoreColor), _coreShapes);

            AddShape(new ShapeDefinition("core_hex_down", "Hex Core (Down)", ShapeCategory.Core, false,
                new[] {
                    new GridCoord(-1, -1), new GridCoord(-1, 0), new GridCoord(-1, 1),
                    new GridCoord(0, -1), new GridCoord(0, 0), new GridCoord(0, 1)
                }, CoreColor), _coreShapes);
        }

        private static void AddShape(ShapeDefinition shape, List<ShapeDefinition> categoryList)
        {
            _allShapes.Add(shape);
            categoryList.Add(shape);
        }

        public static ShapeDefinition GetRandomWeightedShape(bool? requireUp = null)
        {
            float roll = Random.Range(0f, 100f);
            List<ShapeDefinition> list;
            if (roll < 25f) list = _shardShapes;
            else if (roll < 55f) list = _bladeShapes;
            else if (roll < 85f) list = _cleaverShapes;
            else list = _coreShapes;

            if (requireUp.HasValue)
            {
                List<ShapeDefinition> filtered = list.FindAll(s => s.anchorRequiresUp == requireUp.Value);
                if (filtered.Count > 0)
                {
                    return filtered[Random.Range(0, filtered.Count)];
                }
            }

            return list[Random.Range(0, list.Count)];
        }
    }
}
