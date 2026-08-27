using UnityEngine;

namespace PolyFuse.Grid
{
    public static class TriangleMeshHelper
    {
        public const float SideLength = 1.0f;
        public static readonly float Height = SideLength * Mathf.Sqrt(3f) * 0.5f; // ~0.8660254f
        public static readonly float HalfWidth = SideLength * 0.5f;

        private static Material _defaultTileMaterial;

        public static Mesh CreateTriangleMesh(bool isPointingUp, float scale = 0.94f)
        {
            return CreateBeveledTriangleMesh(isPointingUp, scale);
        }

        public static Mesh CreateBeveledTriangleMesh(bool isPointingUp, float scale = 0.94f)
        {
            Mesh mesh = new Mesh();
            mesh.name = isPointingUp ? $"Triangle_Bevel_Up_{scale}" : $"Triangle_Bevel_Down_{scale}";

            float s = scale;
            float h = Height * s;
            float hw = HalfWidth * s;

            // Inset factor for the bevel chamfer border
            float inset = 0.76f;
            float inH = h * inset;
            float inHW = hw * inset;

            Vector3[] vertices = new Vector3[7];
            Vector2[] uvs = new Vector2[7];
            Color[] colors = new Color[7];

            if (isPointingUp)
            {
                // Outer corners
                vertices[0] = new Vector3(0f, 2f * h / 3f, 0f);           // Apex (Top)
                vertices[1] = new Vector3(hw, -h / 3f, 0f);               // Bottom-Right
                vertices[2] = new Vector3(-hw, -h / 3f, 0f);              // Bottom-Left

                // Inset corners
                vertices[3] = new Vector3(0f, 2f * inH / 3f, 0f);
                vertices[4] = new Vector3(inHW, -inH / 3f, 0f);
                vertices[5] = new Vector3(-inHW, -inH / 3f, 0f);

                // Center
                vertices[6] = Vector3.zero;

                // Lighting colors (Top-left highlight, bottom shadow)
                colors[0] = new Color(1.22f, 1.22f, 1.22f, 1f); // Top highlight
                colors[1] = new Color(0.80f, 0.80f, 0.80f, 1f); // Bottom-right shadow
                colors[2] = new Color(1.10f, 1.10f, 1.10f, 1f); // Left highlight
                colors[3] = new Color(1.15f, 1.15f, 1.15f, 1f); // Inset top
                colors[4] = new Color(0.90f, 0.90f, 0.90f, 1f); // Inset bottom-right
                colors[5] = new Color(1.05f, 1.05f, 1.05f, 1f); // Inset left
                colors[6] = new Color(1.00f, 1.00f, 1.00f, 1f); // Face center
            }
            else
            {
                // Outer corners
                vertices[0] = new Vector3(0f, -2f * h / 3f, 0f);          // Apex (Bottom)
                vertices[1] = new Vector3(-hw, h / 3f, 0f);               // Top-Left
                vertices[2] = new Vector3(hw, h / 3f, 0f);                // Top-Right

                // Inset corners
                vertices[3] = new Vector3(0f, -2f * inH / 3f, 0f);
                vertices[4] = new Vector3(-inHW, inH / 3f, 0f);
                vertices[5] = new Vector3(inHW, inH / 3f, 0f);

                // Center
                vertices[6] = Vector3.zero;

                // Lighting colors (Top horizontal facet highlight, bottom shadow)
                colors[0] = new Color(0.78f, 0.78f, 0.78f, 1f); // Bottom shadow
                colors[1] = new Color(1.25f, 1.25f, 1.25f, 1f); // Top-left highlight
                colors[2] = new Color(1.12f, 1.12f, 1.12f, 1f); // Top-right highlight
                colors[3] = new Color(0.88f, 0.88f, 0.88f, 1f); // Inset bottom
                colors[4] = new Color(1.18f, 1.18f, 1.18f, 1f); // Inset top-left
                colors[5] = new Color(1.06f, 1.06f, 1.06f, 1f); // Inset top-right
                colors[6] = new Color(1.00f, 1.00f, 1.00f, 1f); // Face center
            }

            uvs[0] = new Vector2(0.5f, 1f);
            uvs[1] = new Vector2(1f, 0f);
            uvs[2] = new Vector2(0f, 0f);
            uvs[3] = new Vector2(0.5f, 0.8f);
            uvs[4] = new Vector2(0.85f, 0.15f);
            uvs[5] = new Vector2(0.15f, 0.15f);
            uvs[6] = new Vector2(0.5f, 0.5f);

            // 9 Subtriangles: 3 center face + 6 border chamfer quads
            int[] triangles = new int[]
            {
                // Inner Face
                3, 4, 6,
                4, 5, 6,
                5, 3, 6,
                // Border Chamfers (Side 0-1)
                0, 1, 4,
                0, 4, 3,
                // Border Chamfers (Side 1-2)
                1, 2, 5,
                1, 5, 4,
                // Border Chamfers (Side 2-0)
                2, 0, 3,
                2, 3, 5
            };

            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors = colors;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        public static Vector3 GridToWorldPosition(int row, int col, int radius = 3)
        {
            bool isUp = (((row + col) % 2) + 2) % 2 == 0;
            float x = col * HalfWidth;
            float y = (row - radius) * Height + (isUp ? (Height / 3f) : (2f * Height / 3f));
            return new Vector3(x, y, 0f);
        }

        public static Material GetDefaultMaterial()
        {
            if (_defaultTileMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null) shader = Shader.Find("Sprites/Default");
                if (shader == null) shader = Shader.Find("Unlit/Color");
                if (shader == null) shader = Shader.Find("Legacy Shaders/Diffuse");
                _defaultTileMaterial = new Material(shader != null ? shader : Shader.Find("UI/Default"));
            }
            return _defaultTileMaterial;
        }
    }
}
