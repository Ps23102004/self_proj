using System.Collections.Generic;
using UnityEngine;

namespace KartGame.Track
{
    [DisallowMultipleComponent]
    public class TrackBuilder : MonoBehaviour
    {
        [Header("Settings")]
        public TrackSettings settings;
        public Material trackMaterial;
        public Material wallMaterial;
        public Material groundMaterial;

        public List<Checkpoint> Checkpoints { get; private set; } = new List<Checkpoint>();

        private static readonly Vector3[] DefaultWaypoints =
        {
            new Vector3(0f, 0f, 30f),
            new Vector3(20f, 0f, 20f),
            new Vector3(30f, 0f, 0f),
            new Vector3(20f, 0f, -20f),
            new Vector3(0f, 0f, -30f),
            new Vector3(-20f, 0f, -20f),
            new Vector3(-30f, 0f, 0f),
            new Vector3(-20f, 0f, 20f)
        };

        public void Build()
        {
            ClearChildren();

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<TrackSettings>();
            }

            var waypoints = GetWaypoints();
            if (waypoints.Length < 3)
            {
                waypoints = DefaultWaypoints;
            }

            var trackObject = new GameObject("TrackMesh");
            trackObject.transform.SetParent(transform);
            trackObject.transform.localPosition = Vector3.zero;
            trackObject.transform.localRotation = Quaternion.identity;

            Mesh trackMesh = BuildTrackMesh(waypoints, settings.trackWidth);
            var meshFilter = trackObject.AddComponent<MeshFilter>();
            var meshRenderer = trackObject.AddComponent<MeshRenderer>();
            var meshCollider = trackObject.AddComponent<MeshCollider>();

            meshFilter.sharedMesh = trackMesh;
            meshCollider.sharedMesh = trackMesh;

            if (trackMaterial == null)
            {
                trackMaterial = CreateMaterial(new Color(0.15f, 0.75f, 0.95f));
            }
            meshRenderer.sharedMaterial = trackMaterial;

            BuildWalls(waypoints);
            BuildCheckpoints(waypoints);
            BuildGround(waypoints);
        }

        private Vector3[] GetWaypoints()
        {
            if (settings != null && settings.waypoints != null && settings.waypoints.Length >= 3)
            {
                return settings.waypoints;
            }

            return DefaultWaypoints;
        }

        private Mesh BuildTrackMesh(Vector3[] centerPoints, float width)
        {
            int count = centerPoints.Length;
            var vertices = new List<Vector3>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            float distance = 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 prev = centerPoints[(i - 1 + count) % count];
                Vector3 next = centerPoints[(i + 1) % count];
                Vector3 dir = (next - prev).normalized;
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x);

                Vector3 left = centerPoints[i] - perp * (width * 0.5f);
                Vector3 right = centerPoints[i] + perp * (width * 0.5f);

                vertices.Add(left);
                vertices.Add(right);

                if (i > 0)
                {
                    distance += Vector3.Distance(centerPoints[i - 1], centerPoints[i]);
                }

                uvs.Add(new Vector2(0f, distance));
                uvs.Add(new Vector2(1f, distance));
            }

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                int baseIndex = i * 2;
                int nextIndex = next * 2;

                triangles.Add(baseIndex);
                triangles.Add(nextIndex);
                triangles.Add(baseIndex + 1);

                triangles.Add(baseIndex + 1);
                triangles.Add(nextIndex);
                triangles.Add(nextIndex + 1);
            }

            Mesh mesh = new Mesh
            {
                name = "TrackMesh",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray(),
                uv = uvs.ToArray()
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            return mesh;
        }

        private void BuildWalls(Vector3[] centerPoints)
        {
            int count = centerPoints.Length;
            float width = settings.trackWidth;
            float height = settings.wallHeight;
            float thickness = settings.wallThickness;

            if (wallMaterial == null)
            {
                wallMaterial = CreateMaterial(new Color(1f, 0.4f, 0.2f));
            }

            for (int i = 0; i < count; i++)
            {
                Vector3 current = centerPoints[i];
                Vector3 next = centerPoints[(i + 1) % count];
                Vector3 dir = (next - current).normalized;
                Vector3 perp = new Vector3(-dir.z, 0f, dir.x);

                Vector3 leftStart = current - perp * (width * 0.5f);
                Vector3 leftEnd = next - perp * (width * 0.5f);
                Vector3 rightStart = current + perp * (width * 0.5f);
                Vector3 rightEnd = next + perp * (width * 0.5f);

                CreateWallSegment("Wall_L_" + i, leftStart, leftEnd, height, thickness);
                CreateWallSegment("Wall_R_" + i, rightStart, rightEnd, height, thickness);
            }
        }

        private void CreateWallSegment(string name, Vector3 start, Vector3 end, float height, float thickness)
        {
            Vector3 mid = (start + end) * 0.5f;
            float length = Vector3.Distance(start, end);
            Quaternion rot = Quaternion.LookRotation((end - start).normalized, Vector3.up);

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(transform);
            wall.transform.position = mid + Vector3.up * (height * 0.5f);
            wall.transform.rotation = rot;
            wall.transform.localScale = new Vector3(thickness, height, length);

            var renderer = wall.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = wallMaterial;
            }
        }

        private void BuildCheckpoints(Vector3[] centerPoints)
        {
            Checkpoints.Clear();
            int count = centerPoints.Length;

            for (int i = 0; i < count; i++)
            {
                Vector3 current = centerPoints[i];
                Vector3 next = centerPoints[(i + 1) % count];
                Vector3 dir = (next - current).normalized;
                Quaternion rot = Quaternion.LookRotation(dir, Vector3.up);

                GameObject checkpoint = new GameObject($"Checkpoint_{i}");
                checkpoint.transform.SetParent(transform);
                checkpoint.transform.position = current + Vector3.up * 0.25f;
                checkpoint.transform.rotation = rot;

                var collider = checkpoint.AddComponent<BoxCollider>();
                collider.isTrigger = true;
                collider.size = new Vector3(settings.checkpointWidth, 1f, settings.checkpointDepth);

                var cp = checkpoint.AddComponent<Checkpoint>();
                cp.Index = i;
                Checkpoints.Add(cp);

                var marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Marker";
                marker.transform.SetParent(checkpoint.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(settings.checkpointWidth, 0.05f, settings.checkpointDepth);
                var markerRenderer = marker.GetComponent<MeshRenderer>();
                if (markerRenderer != null)
                {
                    markerRenderer.sharedMaterial = CreateMaterial(i == 0 ? new Color(1f, 1f, 0.3f) : new Color(0.9f, 0.9f, 0.9f));
                }
            }
        }

        private void BuildGround(Vector3[] centerPoints)
        {
            var bounds = new Bounds(centerPoints[0], Vector3.zero);
            for (int i = 1; i < centerPoints.Length; i++)
            {
                bounds.Encapsulate(centerPoints[i]);
            }

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.SetParent(transform);
            ground.transform.position = new Vector3(bounds.center.x, -0.1f, bounds.center.z);
            float scale = Mathf.Max(bounds.size.x, bounds.size.z) / 10f + 4f;
            ground.transform.localScale = new Vector3(scale, 1f, scale);

            if (groundMaterial == null)
            {
                groundMaterial = CreateMaterial(new Color(0.35f, 0.85f, 0.35f));
            }

            var renderer = ground.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = groundMaterial;
            }
        }

        private Material CreateMaterial(Color color)
        {
            var material = new Material(Shader.Find("Standard"));
            material.color = color;
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 0.1f);
            return material;
        }

        private void ClearChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                if (Application.isPlaying)
                {
                    Destroy(transform.GetChild(i).gameObject);
                }
                else
                {
                    DestroyImmediate(transform.GetChild(i).gameObject);
                }
            }
        }
    }
}
