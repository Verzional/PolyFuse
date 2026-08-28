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
        public static readonly Color ChevronColor = new Color(0.063f, 0.725f, 0.506f, 1f); // Radiant Emerald Green (#10B981)
        public static readonly Color CrownColor   = new Color(0.545f, 0.361f, 0.965f, 1f); // Royal Amethyst (#8B5CF6)
        public static readonly Color CoreColor    = new Color(0.75f, 0.38f, 0.98f, 1f); // Neon Violet

        private static List<ShapeDefinition> _allShapes;
        private static List<ShapeDefinition> _shardShapes;
        private static List<ShapeDefinition> _bladeShapes;
        private static List<ShapeDefinition> _cleaverShapes;
        private static List<ShapeDefinition> _chevronShapes;
        private static List<ShapeDefinition> _crownShapes;
        private static List<ShapeDefinition> _coreShapes;

        static ShapeCatalog()
        {
            InitializeShapes();
        }

        public static IReadOnlyList<ShapeDefinition> AllShapes => _allShapes;
        public static IReadOnlyList<ShapeDefinition> Shards => _shardShapes;
        public static IReadOnlyList<ShapeDefinition> Blades => _bladeShapes;
        public static IReadOnlyList<ShapeDefinition> Cleavers => _cleaverShapes;
        public static IReadOnlyList<ShapeDefinition> Chevrons => _chevronShapes;
        public static IReadOnlyList<ShapeDefinition> Crowns => _crownShapes;
        public static IReadOnlyList<ShapeDefinition> Cores => _coreShapes;

        private static void InitializeShapes()
        {
            _allShapes = new List<ShapeDefinition>();
            _shardShapes = new List<ShapeDefinition>();
            _bladeShapes = new List<ShapeDefinition>();
            _cleaverShapes = new List<ShapeDefinition>();
            _chevronShapes = new List<ShapeDefinition>();
            _crownShapes = new List<ShapeDefinition>();
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

            // 4. THE CHEVRON (4 Units - Angled V/L Boomerang)
            // +60° Slash Chevron (Z/V form across slash axis)
            AddShape(new ShapeDefinition("chevron_slash_up", "Chevron Slash (Up)", ShapeCategory.Chevron, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, 1), new GridCoord(1, 1), new GridCoord(1, 2) }, ChevronColor), _chevronShapes);
            AddShape(new ShapeDefinition("chevron_slash_down", "Chevron Slash (Down)", ShapeCategory.Chevron, false,
                new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(1, 1), new GridCoord(2, 1) }, ChevronColor), _chevronShapes);

            // 120° Backslash Chevron (Z/V form across backslash axis)
            AddShape(new ShapeDefinition("chevron_backslash_up", "Chevron Backslash (Up)", ShapeCategory.Chevron, true,
                new[] { new GridCoord(0, 0), new GridCoord(0, -1), new GridCoord(1, -1), new GridCoord(1, -2) }, ChevronColor), _chevronShapes);
            AddShape(new ShapeDefinition("chevron_backslash_down", "Chevron Backslash (Down)", ShapeCategory.Chevron, false,
                new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(1, -1), new GridCoord(2, -1) }, ChevronColor), _chevronShapes);

            // Corner Angle / Boomerang (L-Angle across 2 grid axes)
            AddShape(new ShapeDefinition("chevron_angle_up", "Chevron Angle (Up)", ShapeCategory.Chevron, true,
                new[] { new GridCoord(0, 0), new GridCoord(-1, 0), new GridCoord(0, 1), new GridCoord(0, 2) }, ChevronColor), _chevronShapes);
            AddShape(new ShapeDefinition("chevron_angle_down", "Chevron Angle (Down)", ShapeCategory.Chevron, false,
                new[] { new GridCoord(0, 0), new GridCoord(1, 0), new GridCoord(0, 1), new GridCoord(0, 2) }, ChevronColor), _chevronShapes);

            // 5. THE CROWN (5 Units - Hex-Crescent / Regular Hexagon with 1 Triangle Excised)
            // Upward / Downward Cleft Crown
            AddShape(new ShapeDefinition("crown_up", "Crown (Up)", ShapeCategory.Crown, true,
                new[] {
                    new GridCoord(0, -1), new GridCoord(0, 0), new GridCoord(0, 1),
                    new GridCoord(1, -1), new GridCoord(1, 1)
                }, CrownColor), _crownShapes);

            AddShape(new ShapeDefinition("crown_down", "Crown (Down)", ShapeCategory.Crown, false,
                new[] {
                    new GridCoord(-1, -1), new GridCoord(-1, 1),
                    new GridCoord(0, -1), new GridCoord(0, 0), new GridCoord(0, 1)
                }, CrownColor), _crownShapes);

            // Hex-Crescent Left (Facing Left / Open Right)
            AddShape(new ShapeDefinition("crown_left_up", "Crown Crescent Left (Up)", ShapeCategory.Crown, true,
                new[] {
                    new GridCoord(0, -1), new GridCoord(0, 0),
                    new GridCoord(1, -1), new GridCoord(1, 0), new GridCoord(1, 1)
                }, CrownColor), _crownShapes);

            AddShape(new ShapeDefinition("crown_left_down", "Crown Crescent Left (Down)", ShapeCategory.Crown, false,
                new[] {
                    new GridCoord(-1, -1), new GridCoord(-1, 0), new GridCoord(-1, 1),
                    new GridCoord(0, -1), new GridCoord(0, 0)
                }, CrownColor), _crownShapes);

            // Hex-Crescent Right (Facing Right / Open Left)
            AddShape(new ShapeDefinition("crown_right_up", "Crown Crescent Right (Up)", ShapeCategory.Crown, true,
                new[] {
                    new GridCoord(0, 0), new GridCoord(0, 1),
                    new GridCoord(1, -1), new GridCoord(1, 0), new GridCoord(1, 1)
                }, CrownColor), _crownShapes);

            AddShape(new ShapeDefinition("crown_right_down", "Crown Crescent Right (Down)", ShapeCategory.Crown, false,
                new[] {
                    new GridCoord(-1, -1), new GridCoord(-1, 0), new GridCoord(-1, 1),
                    new GridCoord(0, 0), new GridCoord(0, 1)
                }, CrownColor), _crownShapes);

            // 6. THE CORE (6 Units - Regular Hexagon)
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
            if (roll < 18f) list = _shardShapes;
            else if (roll < 42f) list = _bladeShapes;
            else if (roll < 66f) list = _cleaverShapes;
            else if (roll < 82f) list = _chevronShapes;
            else if (roll < 93f) list = _crownShapes;
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
