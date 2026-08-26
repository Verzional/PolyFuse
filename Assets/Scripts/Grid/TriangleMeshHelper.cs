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
            Mesh mesh = new Mesh();
            mesh.name = isPointingUp ? $"Triangle_Up_{scale}" : $"Triangle_Down_{scale}";

            float s = scale;
            float h = Height * s;
            float hw = HalfWidth * s;

            Vector3[] vertices = new Vector3[3];
            Vector2[] uvs = new Vector2[3];
            int[] triangles;

            if (isPointingUp)
            {
                // Centroid is at h/3 from base, 2h/3 from apex
                vertices[0] = new Vector3(0f, 2f * h / 3f, 0f);           // Apex (Top)
                vertices[1] = new Vector3(hw, -h / 3f, 0f);               // Bottom-Right
                vertices[2] = new Vector3(-hw, -h / 3f, 0f);              // Bottom-Left

                uvs[0] = new Vector2(0.5f, 1f);
                uvs[1] = new Vector2(1f, 0f);
                uvs[2] = new Vector2(0f, 0f);

                triangles = new int[] { 0, 1, 2 };
            }
            else
            {
                // Centroid is at h/3 from top base, 2h/3 from bottom apex
                vertices[0] = new Vector3(0f, -2f * h / 3f, 0f);          // Apex (Bottom)
                vertices[1] = new Vector3(-hw, h / 3f, 0f);               // Top-Left
                vertices[2] = new Vector3(hw, h / 3f, 0f);                // Top-Right

                uvs[0] = new Vector2(0.5f, 0f);
                uvs[1] = new Vector2(0f, 1f);
                uvs[2] = new Vector2(1f, 1f);

                triangles = new int[] { 0, 1, 2 };
            }

            mesh.vertices = vertices;
            mesh.uv = uvs;
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
