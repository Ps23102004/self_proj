using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using KartGame.Gameplay;

namespace KartGame.Track
{
    [DisallowMultipleComponent]
    public class RaceSceneBootstrap : MonoBehaviour
    {
        [Header("Settings")]
        public TrackSettings trackSettings;
        public RaceSettings raceSettings;
        public KartSettings kartSettings;
        public int maxPlayers = 4;

        [Header("References")]
        public RaceManager raceManager;

        private TrackBuilder trackBuilder;

        private void Awake()
        {
            if (raceManager == null)
            {
                raceManager = FindObjectOfType<RaceManager>();
            }

            BuildTrack();
            BuildSpawnPoints();
            BuildScenery();
            SetupLighting();

            if (raceManager != null)
            {
                raceManager.ApplySettings(kartSettings, raceSettings);
                raceManager.SetCheckpoints(trackBuilder.Checkpoints);
            }
        }

        private void BuildTrack()
        {
            var trackRoot = new GameObject("TrackRoot");
            trackRoot.transform.SetParent(transform);
            trackBuilder = trackRoot.AddComponent<TrackBuilder>();
            trackBuilder.settings = trackSettings;
            trackBuilder.Build();
        }

        private void BuildSpawnPoints()
        {
            if (raceManager == null || trackBuilder == null || trackBuilder.Checkpoints.Count < 2)
            {
                return;
            }

            var spawnRoot = new GameObject("SpawnPoints");
            spawnRoot.transform.SetParent(transform);

            Vector3 start = trackBuilder.Checkpoints[0].transform.position;
            Vector3 next = trackBuilder.Checkpoints[1].transform.position;
            Vector3 forward = (next - start).normalized;
            Vector3 right = new Vector3(-forward.z, 0f, forward.x);

            float spacing = 2.6f;
            var spawnPoints = new List<Transform>();

            for (int i = 0; i < maxPlayers; i++)
            {
                int row = i / 2;
                int column = i % 2;
                float columnOffset = column == 0 ? -1f : 1f;

                Vector3 position = start - forward * (row * spacing * 1.6f) + right * (columnOffset * spacing);
                var point = new GameObject($"SpawnPoint_{i}");
                point.transform.SetParent(spawnRoot.transform);
                point.transform.position = position + Vector3.up * 0.3f;
                point.transform.rotation = Quaternion.LookRotation(forward, Vector3.up);

                spawnPoints.Add(point.transform);
            }

            raceManager.SetSpawnPoints(spawnPoints.ToArray());
        }

        private void BuildScenery()
        {
            if (trackBuilder == null)
            {
                return;
            }

            var sceneryRoot = new GameObject("Scenery");
            sceneryRoot.transform.SetParent(transform);

            var random = new System.Random(12345);
            var waypoints = trackBuilder.Checkpoints;

            for (int i = 0; i < waypoints.Count; i++)
            {
                Vector3 center = waypoints[i].transform.position;
                Vector3 next = waypoints[(i + 1) % waypoints.Count].transform.position;
                Vector3 dir = (next - center).normalized;
                Vector3 right = new Vector3(-dir.z, 0f, dir.x);

                float offsetDistance = trackSettings != null ? trackSettings.trackWidth * 0.8f : 6f;
                Vector3 leftPos = center - right * (offsetDistance + 6f + (float)random.NextDouble() * 3f);
                Vector3 rightPos = center + right * (offsetDistance + 6f + (float)random.NextDouble() * 3f);

                if (i % 2 == 0)
                {
                    CreateTree(leftPos, sceneryRoot.transform, random);
                    CreateHouse(rightPos, sceneryRoot.transform, random);
                }
                else
                {
                    CreateTree(rightPos, sceneryRoot.transform, random);
                    CreateHouse(leftPos, sceneryRoot.transform, random);
                }
            }
        }

        private void CreateTree(Vector3 position, Transform parent, System.Random random)
        {
            var treeRoot = new GameObject("Tree");
            treeRoot.transform.SetParent(parent);
            treeRoot.transform.position = position;

            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            trunk.transform.SetParent(treeRoot.transform);
            trunk.transform.localPosition = Vector3.up * 1.2f;
            trunk.transform.localScale = new Vector3(0.5f, 1.2f, 0.5f);
            SetColor(trunk, new Color(0.45f, 0.25f, 0.1f));

            var crown = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            crown.transform.SetParent(treeRoot.transform);
            crown.transform.localPosition = Vector3.up * 2.6f;
            float size = 1.6f + (float)random.NextDouble();
            crown.transform.localScale = new Vector3(size, size, size);
            SetColor(crown, new Color(0.2f, 0.85f, 0.35f));
        }

        private void CreateHouse(Vector3 position, Transform parent, System.Random random)
        {
            var houseRoot = new GameObject("House");
            houseRoot.transform.SetParent(parent);
            houseRoot.transform.position = position;

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(houseRoot.transform);
            body.transform.localPosition = Vector3.up * 1.2f;
            body.transform.localScale = new Vector3(2.4f, 2.4f, 2f);
            SetColor(body, RandomBrightColor(random));

            var roof = GameObject.CreatePrimitive(PrimitiveType.Cube);
            roof.transform.SetParent(houseRoot.transform);
            roof.transform.localPosition = Vector3.up * 2.8f;
            roof.transform.localScale = new Vector3(2.6f, 0.6f, 2.2f);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 10f);
            SetColor(roof, new Color(0.9f, 0.3f, 0.3f));
        }

        private void SetupLighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.7f, 0.8f, 0.9f);

            if (FindObjectOfType<Light>() == null)
            {
                var lightObject = new GameObject("Directional Light");
                var light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }

            var skybox = new Material(Shader.Find("Skybox/Procedural"));
            skybox.SetFloat("_SunSize", 0.04f);
            skybox.SetFloat("_AtmosphereThickness", 1f);
            skybox.SetColor("_SkyTint", new Color(0.35f, 0.75f, 0.95f));
            skybox.SetColor("_GroundColor", new Color(0.95f, 0.85f, 0.6f));
            RenderSettings.skybox = skybox;
        }

        private void SetColor(GameObject obj, Color color)
        {
            var renderer = obj.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = color;
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 0.1f);
                renderer.sharedMaterial = mat;
            }
        }

        private Color RandomBrightColor(System.Random random)
        {
            float r = 0.4f + (float)random.NextDouble() * 0.6f;
            float g = 0.4f + (float)random.NextDouble() * 0.6f;
            float b = 0.4f + (float)random.NextDouble() * 0.6f;
            return new Color(r, g, b);
        }
    }
}
