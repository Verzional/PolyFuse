using System.Collections;
using System.Collections.Generic;
using PolyFuse.Grid;
using UnityEngine;

namespace PolyFuse.Juice
{
    public class ProceduralParticleManager : MonoBehaviour
    {
        public static ProceduralParticleManager Instance { get; private set; }

        private Material _particleMaterial;
        private Mesh _upShardMesh;
        private Mesh _downShardMesh;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            _upShardMesh = TriangleMeshHelper.CreateTriangleMesh(true, 0.40f);
            _downShardMesh = TriangleMeshHelper.CreateTriangleMesh(false, 0.40f);
            _particleMaterial = TriangleMeshHelper.GetDefaultMaterial();
        }

        public void SpawnTileShatter(Vector3 position, Color color, bool isPointingUp)
        {
            int shardCount = 6;
            for (int i = 0; i < shardCount; i++)
            {
                GameObject shard = new GameObject("ShardParticle");
                shard.transform.position = position;
                shard.transform.SetParent(transform, true);

                MeshFilter mf = shard.AddComponent<MeshFilter>();
                MeshRenderer mr = shard.AddComponent<MeshRenderer>();

                mf.sharedMesh = (i % 2 == 0) ? _upShardMesh : _downShardMesh;
                mr.sharedMaterial = _particleMaterial;
                mr.sortingOrder = 25;

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                mr.GetPropertyBlock(block);
                Color glowColor = Color.Lerp(color, Color.white, 0.35f);
                block.SetColor("_Color", glowColor);
                block.SetColor("_BaseColor", glowColor);
                mr.SetPropertyBlock(block);

                float angle = (i / (float)shardCount) * 360f + Random.Range(-20f, 20f);
                Vector2 dir = Quaternion.Euler(0f, 0f, angle) * Vector2.right;
                float speed = Random.Range(2.5f, 6.0f);
                float rotSpeed = Random.Range(-360f, 360f);

                StartCoroutine(AnimateShard(shard, dir * speed, rotSpeed, glowColor));
            }
        }

        public void SpawnAxisLaser(Vector3 startPos, Vector3 endPos, Color color)
        {
            GameObject laserObj = new GameObject("AxisLaser");
            laserObj.transform.SetParent(transform, true);

            LineRenderer lr = laserObj.AddComponent<LineRenderer>();
            lr.sharedMaterial = _particleMaterial;
            lr.startWidth = 0.35f;
            lr.endWidth = 0.35f;
            lr.positionCount = 2;
            lr.SetPosition(0, startPos);
            lr.SetPosition(1, endPos);
            lr.sortingOrder = 30;

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(color, 0.5f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.5f), new GradientAlphaKey(1f, 1f) }
            );
            lr.colorGradient = gradient;

            StartCoroutine(AnimateLaser(laserObj, lr));
        }

        private IEnumerator AnimateShard(GameObject shard, Vector2 velocity, float rotSpeed, Color baseColor)
        {
            MeshRenderer mr = shard.GetComponent<MeshRenderer>();
            MaterialPropertyBlock block = new MaterialPropertyBlock();

            float elapsed = 0f;
            float duration = Random.Range(0.35f, 0.55f);
            Vector3 startScale = shard.transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;

                shard.transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);
                velocity *= Mathf.Pow(0.1f, Time.unscaledDeltaTime); // Drag
                shard.transform.Rotate(0f, 0f, rotSpeed * Time.unscaledDeltaTime);
                shard.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t * t);

                if (mr != null)
                {
                    mr.GetPropertyBlock(block);
                    Color c = baseColor;
                    c.a = 1f - t;
                    block.SetColor("_Color", c);
                    block.SetColor("_BaseColor", c);
                    mr.SetPropertyBlock(block);
                }

                yield return null;
            }

            Destroy(shard);
        }

        private IEnumerator AnimateLaser(GameObject laserObj, LineRenderer lr)
        {
            float elapsed = 0f;
            float duration = 0.25f;
            float initialWidth = lr.startWidth;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / duration;
                float w = Mathf.Lerp(initialWidth, 0f, t * t);
                lr.startWidth = w;
                lr.endWidth = w;
                yield return null;
            }

            Destroy(laserObj);
        }
    }
}
