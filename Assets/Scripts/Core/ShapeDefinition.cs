using System;
using UnityEngine;

namespace PolyFuse.Core
{
    [Serializable]
    public class ShapeDefinition
    {
        public string id;
        public string displayName;
        public ShapeCategory category;
        public bool anchorRequiresUp;
        public GridCoord[] relativeOffsets;
        public Color defaultColor;

        public ShapeDefinition(string id, string displayName, ShapeCategory category, bool anchorRequiresUp, GridCoord[] relativeOffsets, Color color)
        {
            this.id = id;
            this.displayName = displayName;
            this.category = category;
            this.anchorRequiresUp = anchorRequiresUp;
            this.relativeOffsets = relativeOffsets;
            this.defaultColor = color;
        }

        public int UnitCount => relativeOffsets != null ? relativeOffsets.Length : 0;
    }
}
