using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace StageFlow.Editor
{
    public static class SampleProjectGenerator
    {
        public const string Root = "Assets/StageFlow";
        public const string ScenePath = Root + "/Scenes/StageFlowDemo.unity";

        [MenuItem("Tools/StageFlow/Create Sample Assets")]
        public static void CreateSampleAssets()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before creating sample assets.");
            // 배치 프로세스의 빈 초기 씬만 재사용한다. 사용자 씬은 저장하거나 교체하지 않는다.
            var previous = SceneManager.GetActiveScene();
            var useBatchScene = Application.isBatchMode && SceneManager.sceneCount == 1 &&
                string.IsNullOrEmpty(previous.path) && !previous.isDirty && previous.rootCount == 0;
            var useOpenSampleScene = previous.path == ScenePath;
            if (!useBatchScene)
                for (var i = 0; i < SceneManager.sceneCount; i++)
                    if (string.IsNullOrEmpty(SceneManager.GetSceneAt(i).path))
                        throw new InvalidOperationException("Save untitled scenes before generating assets. No open scene was changed.");
            foreach (var folder in new[] { "Materials", "Prefabs", "Data", "Scenes" })
                EnsureFolder(Root + "/" + folder);

            var scratch = useBatchScene || useOpenSampleScene ? previous :
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(scratch);
            try
            {
                var blue = MaterialAt("CubeBlue", new Color(0.12f, 0.55f, 0.95f));
                var orange = MaterialAt("CapsuleOrange", new Color(1f, 0.45f, 0.12f));
                var magenta = MaterialAt("DroneMagenta", new Color(0.88f, 0.2f, 0.72f));
                var ground = MaterialAt("Ground", new Color(0.22f, 0.27f, 0.32f));
                var arrival = MaterialAt("Arrival", new Color(0.2f, 0.85f, 0.48f));
                var cube = PrefabAt("CubeEnemy", PrimitiveType.Cube, blue);
                var capsule = PrefabAt("CapsuleEnemy", PrimitiveType.Capsule, orange);
                var cylinder = PrefabAt("DroneEnemy", PrimitiveType.Cylinder, magenta);
                var scout = EnemyAt("Scout", "scout", "Cube Scout", 2.8f, cube);
                var walker = EnemyAt("Walker", "walker", "Capsule Walker", 1.9f, capsule);
                var drone = EnemyAt("Drone", "drone", "Cylinder Drone", 2.4f, cylinder);
                var stageOne = StageAt("StageOne", "stage-one", "Stage One (5)", scout, 3, walker, 2);
                var stageTwo = StageAt("StageTwo", "stage-two", "Stage Two (8)", walker, 4, scout, 4);
                var upper = RouteAt("UpperRoute", "upper", "Upper Route", -1.45f, 0.35f);
                var center = RouteAt("CenterRoute", "center", "Center Route", 0f, 0.8f);
                var lower = RouteAt("LowerRoute", "lower", "Lower Route", 1.45f, -0.35f);
                var normal = RuntimeStageAt("StageRuntimeNormal", "runtime-normal",
                    "Stage Runtime Normal (150)", false, scout, walker, drone, upper, center, lower);
                var stress = RuntimeStageAt("StageRuntimeStress", "runtime-stress",
                    "Stage Runtime Stress (2400)", true, scout, walker, drone, upper, center, lower);

                if (!File.Exists(ScenePath))
                {
                    if (File.Exists(ScenePath + ".meta"))
                        throw new InvalidOperationException("Orphan scene meta found; restore or review it first: " + ScenePath);
                    var floor = Primitive("Floor", PrimitiveType.Cube, ground);
                    floor.transform.position = new Vector3(0, -0.3f, 0);
                    floor.transform.localScale = new Vector3(14, 0.5f, 5);
                    var spawn = new GameObject("Spawn Point").transform;
                    spawn.position = new Vector3(-5, 0.6f, 0);
                    var target = new GameObject("Destination").transform;
                    target.position = new Vector3(5, 0.6f, 0);
                    var marker = Primitive("Arrival Marker", PrimitiveType.Cube, arrival);
                    marker.transform.position = new Vector3(5, 0.03f, 0);
                    marker.transform.localScale = new Vector3(0.3f, 0.08f, 3);
                    var camera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                    camera.tag = "MainCamera";
                    camera.transform.position = new Vector3(-1, 10, -15);
                    camera.transform.LookAt(new Vector3(-1, 0, 0));
                    var view = camera.GetComponent<Camera>();
                    view.orthographic = true;
                    view.orthographicSize = 7.5f;
                    view.clearFlags = CameraClearFlags.SolidColor;
                    view.backgroundColor = new Color(0.055f, 0.075f, 0.11f);
                    var light = new GameObject("Directional Light", typeof(Light));
                    light.transform.rotation = Quaternion.Euler(50, -30, 0);
                    light.GetComponent<Light>().type = LightType.Directional;
                    light.GetComponent<Light>().intensity = 1.5f;
                    var controller = new GameObject("StageFlow", typeof(StageRunner), typeof(StageFlowPanel));
                    controller.GetComponent<StageRunner>().Configure(
                        new[] { stageOne, stageTwo, normal, stress }, spawn, target);
                    if (!EditorSceneManager.SaveScene(scratch, ScenePath))
                        throw new IOException("Could not save " + ScenePath);
                }
                else
                {
                    var sample = SceneManager.GetSceneByPath(ScenePath);
                    var openedForUpdate = !sample.IsValid() || !sample.isLoaded;
                    if (openedForUpdate) sample = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
                    try
                    {
                        StageRunner runner = null;
                        Transform spawn = null;
                        Transform target = null;
                        foreach (var root in sample.GetRootGameObjects())
                        {
                            if (root.TryGetComponent<StageRunner>(out var found)) runner = found;
                            if (root.name == "Spawn Point") spawn = root.transform;
                            if (root.name == "Destination") target = root.transform;
                        }
                        if (runner == null || spawn == null || target == null)
                            throw new InvalidOperationException("The StageFlow sample scene is missing its runner or endpoints.");
                        runner.Configure(new[] { stageOne, stageTwo, normal, stress }, spawn, target);
                        if (!EditorSceneManager.SaveScene(sample)) throw new IOException("Could not update " + ScenePath);
                    }
                    finally
                    {
                        if (openedForUpdate) EditorSceneManager.CloseScene(sample, true);
                    }
                }
                Debug.Log("StageFlow sample assets ready: " + ScenePath + ". Existing assets were preserved.");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                if (!useBatchScene && !useOpenSampleScene) EditorSceneManager.CloseScene(scratch, true);
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static T Existing<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            if (File.Exists(path) || File.Exists(path + ".meta"))
                throw new InvalidOperationException("Existing or unimported file requires review: " + path);
            return null;
        }

        private static Material MaterialAt(string name, Color color)
        {
            var path = Root + "/Materials/" + name + ".mat";
            var existing = Existing<Material>(path);
            if (existing != null) return existing;
            var pipeline = GraphicsSettings.currentRenderPipeline;
            var shaderName = pipeline == null ? "Standard" :
                pipeline.GetType().Name.Contains("Universal") ? "Universal Render Pipeline/Lit" :
                pipeline.GetType().Name.Contains("HDRender") ? "HDRP/Lit" : null;
            var shader = shaderName == null ? null : Shader.Find(shaderName);
            if (shader == null) throw new InvalidOperationException("No supported shader for current render pipeline.");
            var material = new Material(shader) { name = name, color = color };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject Primitive(string name, PrimitiveType type, Material material)
        {
            var instance = GameObject.CreatePrimitive(type);
            instance.name = name;
            UnityEngine.Object.DestroyImmediate(instance.GetComponent<Collider>());
            instance.GetComponent<Renderer>().sharedMaterial = material;
            return instance;
        }

        private static GameObject PrefabAt(string name, PrimitiveType type, Material material)
        {
            var path = Root + "/Prefabs/" + name + ".prefab";
            var existing = Existing<GameObject>(path);
            if (existing != null) return existing;
            var instance = Primitive(name, type, material);
            try
            {
                if (type == PrimitiveType.Capsule) instance.transform.localScale = new Vector3(0.7f, 0.6f, 0.7f);
                instance.AddComponent<EnemyMover>();
                var prefab = PrefabUtility.SaveAsPrefabAsset(instance, path);
                if (prefab == null) throw new IOException("Could not save " + path);
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(instance); }
        }

        private static EnemyDefinition EnemyAt(string name, string id, string label, float speed, GameObject prefab)
        {
            var path = Root + "/Data/" + name + ".asset";
            var existing = Existing<EnemyDefinition>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<EnemyDefinition>();
            data.Id = id; data.DisplayName = label; data.MoveSpeed = speed; data.Prefab = prefab;
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static StageDefinition StageAt(string name, string id, string label,
            EnemyDefinition first, int firstCount, EnemyDefinition second, int secondCount)
        {
            var path = Root + "/Data/" + name + ".asset";
            var existing = Existing<StageDefinition>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<StageDefinition>();
            data.Id = id; data.DisplayName = label; data.SpawnInterval = 0.6f;
            data.Entries = new[] { new SpawnEntry { Enemy = first, Count = firstCount },
                new SpawnEntry { Enemy = second, Count = secondCount } };
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static RouteDefinition RouteAt(string name, string id, string label, float laneZ, float bendZ)
        {
            var path = Root + "/Data/" + name + ".asset";
            var existing = Existing<RouteDefinition>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<RouteDefinition>();
            data.Id = id;
            data.DisplayName = label;
            data.Waypoints = new[]
            {
                new Vector3(-5f, 0.6f, laneZ),
                new Vector3(-2.5f, 0.6f, laneZ),
                new Vector3(0f, 0.6f, laneZ + bendZ),
                new Vector3(2.5f, 0.6f, laneZ),
                new Vector3(5f, 0.6f, laneZ)
            };
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static StageDefinition RuntimeStageAt(string name, string id, string label, bool stress,
            EnemyDefinition scout, EnemyDefinition walker, EnemyDefinition drone,
            RouteDefinition upper, RouteDefinition center, RouteDefinition lower)
        {
            var path = Root + "/Data/" + name + ".asset";
            var existing = Existing<StageDefinition>(path);
            if (existing != null) return existing;
            var data = ScriptableObject.CreateInstance<StageDefinition>();
            data.Id = id;
            data.DisplayName = label;
            data.Entries = Array.Empty<SpawnEntry>();
            data.Waves = stress
                ? StressWaves(scout, walker, drone, upper, center, lower)
                : NormalWaves(scout, walker, drone, upper, center, lower);
            AssetDatabase.CreateAsset(data, path);
            return data;
        }

        private static WaveDefinition[] NormalWaves(EnemyDefinition scout, EnemyDefinition walker,
            EnemyDefinition drone, RouteDefinition upper, RouteDefinition center, RouteDefinition lower)
        {
            return new[]
            {
                Wave("Wave 1", Group("Upper Scouts", scout, 12, 0f, 0.16f, upper),
                    Group("Center Walkers", walker, 10, 0.45f, 0.22f, center),
                    Group("Lower Drones", drone, 8, 0.9f, 0.18f, lower)),
                Wave("Wave 2", Group("Lower Scouts", scout, 15, 0f, 0.14f, lower),
                    Group("Upper Drones", drone, 12, 0.3f, 0.17f, upper),
                    Group("Center Walkers", walker, 8, 0.75f, 0.24f, center)),
                Wave("Wave 3", Group("Center Scouts", scout, 18, 0f, 0.13f, center),
                    Group("Lower Walkers", walker, 10, 0.4f, 0.21f, lower),
                    Group("Upper Drones", drone, 12, 0.65f, 0.16f, upper)),
                Wave("Wave 4", Group("Upper Scouts", scout, 20, 0f, 0.12f, upper),
                    Group("Center Drones", drone, 15, 0.25f, 0.14f, center),
                    Group("Lower Walkers", walker, 10, 0.55f, 0.2f, lower))
            };
        }

        private static WaveDefinition[] StressWaves(EnemyDefinition scout, EnemyDefinition walker,
            EnemyDefinition drone, RouteDefinition upper, RouteDefinition center, RouteDefinition lower)
        {
            var waves = new WaveDefinition[10];
            for (var index = 0; index < waves.Length; index++)
            {
                var rotate = index % 3;
                var routes = rotate == 0 ? new[] { upper, center, lower } :
                    rotate == 1 ? new[] { center, lower, upper } : new[] { lower, upper, center };
                waves[index] = Wave("Wave " + (index + 1),
                    Group("Scout A", scout, 50, 0f, 0.018f, routes[0]),
                    Group("Drone A", drone, 45, 0.08f, 0.022f, routes[1]),
                    Group("Walker A", walker, 40, 0.16f, 0.026f, routes[2]),
                    Group("Scout B", scout, 35, 0.24f, 0.02f, routes[1]),
                    Group("Drone B", drone, 40, 0.32f, 0.021f, routes[2]),
                    Group("Walker B", walker, 30, 0.4f, 0.028f, routes[0]));
            }
            return waves;
        }

        private static WaveDefinition Wave(string name, params SpawnGroupDefinition[] groups)
        {
            return new WaveDefinition { DisplayName = name, Groups = groups };
        }

        private static SpawnGroupDefinition Group(string name, EnemyDefinition enemy, int count,
            float delay, float interval, RouteDefinition route)
        {
            return new SpawnGroupDefinition
            {
                DisplayName = name,
                Enemy = enemy,
                Count = count,
                StartDelay = delay,
                SpawnInterval = interval,
                Route = route
            };
        }
    }
}
