#if UNITY_EDITOR
using System;
using System.IO;
using AIMAR;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Vuforia;
using Object = UnityEngine.Object;
using UIImage = UnityEngine.UI.Image;

namespace AIMAR.Editor
{
    public static class AIMARSceneBuilder
    {
        private const string Root = "Assets/AIMAR";
        private const string ScenePath = Root + "/Scenes/Entrenamiento.unity";
        private const string PrefabPath = Root + "/Prefabs/Target.prefab";
        private const string MarkerPath = Root + "/Images/AIMAR_Marker.png";
        private const int TargetLayer = 8;

        [InitializeOnLoadMethod]
        private static void CompletePendingPrototypeAfterReload()
        {
            if (!File.Exists(ScenePath) || File.Exists(MarkerPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!EditorApplication.isPlayingOrWillChangePlaymode && !File.Exists(MarkerPath))
                {
                    BuildPrototypeInternal(false);
                }
            };
        }

        [MenuItem("AIM-AR/Construir prototipo de la segunda entrega")]
        public static void BuildPrototype()
        {
            BuildPrototypeInternal(true);
        }

        public static void BuildPrototypeBatch()
        {
            BuildPrototypeInternal(false);
        }

        private static void BuildPrototypeInternal(bool interactive)
        {
            if (interactive && !EditorUtility.DisplayDialog(
                    "AIM-AR",
                    "Se construirá o actualizará Entrenamiento.unity. SampleScene no será modificada. ¿Continuar?",
                    "Construir",
                    "Cancelar"))
            {
                return;
            }

            EnsureFolders();
            EnsureTargetLayer();

            Scene scene = System.IO.File.Exists(ScenePath)
                ? EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single)
                : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject arCameraObject = GetOrCreateArCamera();
            ImageTargetBehaviour imageTarget = GetOrCreateImageTarget();
            ConfigureInstantImageTarget(imageTarget);
            ClearGeneratedContent(imageTarget.gameObject);

            GameManager gameManager = new GameObject("GameManager").AddComponent<GameManager>();
            ShooterController shooter = new GameObject("ShooterController").AddComponent<ShooterController>();

            Transform arContent = new GameObject("ARContent").transform;
            arContent.SetParent(imageTarget.transform, false);

            CreatePlatform(arContent);
            Target target = CreateTargetInstance(arContent, gameManager);
            CreateHud(gameManager, shooter);

            Camera arCamera = arCameraObject.GetComponent<Camera>();
            shooter.Configure(arCamera, gameManager, 1 << TargetLayer);

            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(shooter);
            EditorUtility.SetDirty(target);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = imageTarget.gameObject;
            EditorGUIUtility.PingObject(imageTarget.gameObject);

            if (interactive)
            {
                EditorUtility.DisplayDialog(
                    "Prototipo AIM-AR creado",
                    "Entrenamiento.unity ya contiene ARCamera, ImageTarget, ARContent, plataforma, diana, raycast y HUD.\n\n" +
                    "Paso manual obligatorio: en el ImageTarget seleccionado asigna la base de datos y el marcador. " +
                    "Luego agrega la licencia de Vuforia y prueba con Play.",
                    "Entendido");
            }
        }

        private static void EnsureFolders()
        {
            CreateFolder("Assets", "AIMAR");
            CreateFolder(Root, "Scenes");
            CreateFolder(Root, "Scripts");
            CreateFolder(Root, "Editor");
            CreateFolder(Root, "Prefabs");
            CreateFolder(Root, "Materials");
            CreateFolder(Root, "UI");
            CreateFolder(Root, "Images");
        }

        private static void CreateFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void EnsureTargetLayer()
        {
            SerializedObject tagManager = new SerializedObject(
                AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty layers = tagManager.FindProperty("layers");
            SerializedProperty layer = layers.GetArrayElementAtIndex(TargetLayer);

            if (!string.IsNullOrEmpty(layer.stringValue) && layer.stringValue != "Target")
            {
                throw new InvalidOperationException(
                    $"La capa {TargetLayer} ya está ocupada por '{layer.stringValue}'. Libérala o cambia TargetLayer en AIMARSceneBuilder.");
            }

            layer.stringValue = "Target";
            tagManager.ApplyModifiedProperties();
        }

        private static GameObject GetOrCreateArCamera()
        {
            VuforiaBehaviour existing = Object.FindAnyObjectByType<VuforiaBehaviour>();
            if (existing != null)
            {
                return existing.gameObject;
            }

            EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/AR Camera");
            existing = Object.FindAnyObjectByType<VuforiaBehaviour>();
            if (existing != null)
            {
                existing.gameObject.name = "ARCamera";
                return existing.gameObject;
            }

            GameObject cameraObject = new GameObject("ARCamera");
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            cameraObject.AddComponent<VuforiaBehaviour>();
            cameraObject.AddComponent<DefaultInitializationErrorHandler>();
            return cameraObject;
        }

        private static ImageTargetBehaviour GetOrCreateImageTarget()
        {
            ImageTargetBehaviour existing = Object.FindAnyObjectByType<ImageTargetBehaviour>();
            if (existing != null)
            {
                return existing;
            }

            EditorApplication.ExecuteMenuItem("GameObject/Vuforia Engine/Image Target");
            existing = Object.FindAnyObjectByType<ImageTargetBehaviour>();
            if (existing != null)
            {
                existing.gameObject.name = "ImageTarget_AIMAR";
                return existing;
            }

            GameObject targetObject = new GameObject("ImageTarget_AIMAR");
            existing = targetObject.AddComponent<ImageTargetBehaviour>();
            targetObject.AddComponent<DefaultObserverEventHandler>();
            return existing;
        }

        private static void ConfigureInstantImageTarget(ImageTargetBehaviour imageTarget)
        {
            Texture2D marker = AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
            if (marker == null)
            {
                marker = GenerateMarkerTexture();
            }

            SerializedObject serializedTarget = new SerializedObject(imageTarget);
            serializedTarget.FindProperty("mImageTargetType").intValue = 3; // ImageTargetType.INSTANT
            serializedTarget.FindProperty("mRuntimeTexture").objectReferenceValue = marker;
            serializedTarget.FindProperty("mAspectRatio").floatValue = 1f;
            serializedTarget.FindProperty("mWidth").floatValue = 0.2f;
            serializedTarget.FindProperty("mHeight").floatValue = 0.2f;
            serializedTarget.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(imageTarget);
        }

        private static Texture2D GenerateMarkerTexture()
        {
            const int size = 1024;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGB24, false);
            Color32 white = new Color32(245, 245, 240, 255);
            Color32 dark = new Color32(17, 35, 55, 255);
            Color32 red = new Color32(196, 48, 35, 255);
            Color32 blue = new Color32(28, 99, 151, 255);

            Color32[] pixels = new Color32[size * size];
            Array.Fill(pixels, white);
            texture.SetPixels32(pixels);

            FillRect(texture, 36, 36, 952, 952, dark);
            FillRect(texture, 68, 68, 888, 888, white);

            const int grid = 24;
            const int cell = 34;
            const int origin = 104;
            uint state = 0xA1F4C93Du;
            for (int y = 0; y < grid; y++)
            {
                for (int x = 0; x < grid; x++)
                {
                    state = state * 1664525u + 1013904223u;
                    if ((state & 0x80000000u) != 0)
                    {
                        FillRect(texture, origin + x * cell, origin + y * cell, cell - 4, cell - 4, dark);
                    }
                }
            }

            DrawFinder(texture, 118, 118, 196, dark, white, red);
            DrawFinder(texture, 710, 130, 174, dark, white, blue);
            DrawFinder(texture, 132, 712, 158, dark, white, blue);

            FillRect(texture, 392, 390, 240, 240, white);
            FillRect(texture, 414, 412, 196, 196, red);
            FillRect(texture, 450, 448, 124, 124, white);
            FillRect(texture, 486, 484, 52, 52, dark);

            for (int i = 0; i < 9; i++)
            {
                FillRect(texture, 690 + i * 20, 700 + i * 13, 14, 90 - i * 7, i % 2 == 0 ? red : blue);
            }

            texture.Apply(false, false);
            File.WriteAllBytes(MarkerPath, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(MarkerPath, ImportAssetOptions.ForceSynchronousImport);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(MarkerPath);
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.maxTextureSize = size;
            importer.SaveAndReimport();

            return AssetDatabase.LoadAssetAtPath<Texture2D>(MarkerPath);
        }

        private static void DrawFinder(Texture2D texture, int x, int y, int size, Color32 outer, Color32 inner, Color32 center)
        {
            FillRect(texture, x, y, size, size, outer);
            int inset = size / 6;
            FillRect(texture, x + inset, y + inset, size - inset * 2, size - inset * 2, inner);
            FillRect(texture, x + inset * 2, y + inset * 2, size - inset * 4, size - inset * 4, center);
        }

        private static void FillRect(Texture2D texture, int x, int y, int width, int height, Color32 color)
        {
            Color32[] block = new Color32[width * height];
            Array.Fill(block, color);
            texture.SetPixels32(x, y, width, height, block);
        }

        private static void ClearGeneratedContent(GameObject imageTarget)
        {
            DestroyNamedRoot("GameManager");
            DestroyNamedRoot("ShooterController");
            DestroyNamedRoot("AIMAR_HUD");
            DestroyNamedRoot("EventSystem");

            Transform oldContent = imageTarget.transform.Find("ARContent");
            if (oldContent != null)
            {
                Object.DestroyImmediate(oldContent.gameObject);
            }
        }

        private static void DestroyNamedRoot(string objectName)
        {
            GameObject found = GameObject.Find(objectName);
            if (found != null)
            {
                Object.DestroyImmediate(found);
            }
        }

        private static void CreatePlatform(Transform parent)
        {
            GameObject platform = GameObject.CreatePrimitive(PrimitiveType.Cube);
            platform.name = "Plataforma";
            platform.transform.SetParent(parent, false);
            platform.transform.localPosition = new Vector3(0f, 0.015f, 0f);
            platform.transform.localScale = new Vector3(1.2f, 0.03f, 0.85f);
            platform.GetComponent<Renderer>().sharedMaterial = CreateMaterial(
                Root + "/Materials/Platform.mat",
                new Color(0.08f, 0.22f, 0.34f, 1f));
        }

        private static Target CreateTargetInstance(Transform parent, GameManager gameManager)
        {
            GameObject prefab = CreateTargetPrefab();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = "Target_01";
            instance.transform.localPosition = new Vector3(0f, 0.16f, 0f);
            instance.transform.localRotation = Quaternion.identity;

            Target target = instance.GetComponent<Target>();
            target.Configure(gameManager);
            return target;
        }

        private static GameObject CreateTargetPrefab()
        {
            GameObject root = new GameObject("Target");
            root.layer = TargetLayer;
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.025f, 0f);
            collider.size = new Vector3(0.48f, 0.08f, 0.48f);
            root.AddComponent<Target>();

            CreateTargetDisc(root.transform, "Outer_Red", 0.48f, 0f,
                CreateMaterial(Root + "/Materials/TargetRed.mat", new Color(0.84f, 0.1f, 0.07f, 1f)));
            CreateTargetDisc(root.transform, "Middle_White", 0.32f, 0.035f,
                CreateMaterial(Root + "/Materials/TargetWhite.mat", new Color(0.95f, 0.95f, 0.92f, 1f)));
            CreateTargetDisc(root.transform, "Center_Red", 0.15f, 0.07f,
                CreateMaterial(Root + "/Materials/TargetCenter.mat", new Color(0.72f, 0.04f, 0.03f, 1f)));

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateTargetDisc(Transform parent, string name, float diameter, float height, Material material)
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = name;
            disc.layer = TargetLayer;
            disc.transform.SetParent(parent, false);
            disc.transform.localPosition = new Vector3(0f, height, 0f);
            disc.transform.localScale = new Vector3(diameter, 0.025f, diameter);
            Object.DestroyImmediate(disc.GetComponent<Collider>());
            disc.GetComponent<Renderer>().sharedMaterial = material;
        }

        private static Material CreateMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.color = color;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void CreateHud(GameManager gameManager, ShooterController shooter)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new GameObject("AIMAR_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            Text score = CreateText(canvas.transform, "ScoreText", "Puntaje: 0", font, 42, TextAnchor.MiddleLeft);
            SetRect(score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -35f), new Vector2(390f, 85f));

            Text time = CreateText(canvas.transform, "TimeText", "Tiempo: 30", font, 42, TextAnchor.MiddleRight);
            SetRect(time.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -35f), new Vector2(390f, 85f));

            Text instruction = CreateText(canvas.transform, "InstructionText", "Apunta con la retícula y presiona FUEGO", font, 30, TextAnchor.MiddleCenter);
            SetRect(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(850f, 70f));

            CreateCrosshair(canvas.transform);
            Button fireButton = CreateFireButton(canvas.transform, font);
            UnityEventTools.AddPersistentListener(fireButton.onClick, shooter.Shoot);

            gameManager.ConfigureHud(score, time);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.raycastTarget = false;
            return text;
        }

        private static void CreateCrosshair(Transform parent)
        {
            CreateCrosshairLine(parent, "Crosshair_Horizontal", new Vector2(115f, 8f));
            CreateCrosshairLine(parent, "Crosshair_Vertical", new Vector2(8f, 115f));
        }

        private static void CreateCrosshairLine(Transform parent, string name, Vector2 size)
        {
            GameObject line = new GameObject(name, typeof(RectTransform), typeof(UIImage));
            line.transform.SetParent(parent, false);
            UIImage image = line.GetComponent<UIImage>();
            image.color = Color.white;
            image.raycastTarget = false;
            SetRect(line.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        }

        private static Button CreateFireButton(Transform parent, Font font)
        {
            GameObject buttonObject = new GameObject("FireButton", typeof(RectTransform), typeof(UIImage), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            UIImage image = buttonObject.GetComponent<UIImage>();
            image.color = new Color(0.78f, 0.12f, 0.08f, 0.96f);
            SetRect(buttonObject.GetComponent<RectTransform>(), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-55f, 55f), new Vector2(300f, 150f));

            Text label = CreateText(buttonObject.transform, "Label", "FUEGO", font, 48, TextAnchor.MiddleCenter);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            return buttonObject.GetComponent<Button>();
        }

        private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = new Vector2(anchorMax.x, anchorMax.y);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;
        }

        private static void AddSceneToBuildSettings()
        {
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path == ScenePath)
                {
                    return;
                }
            }

            EditorBuildSettingsScene[] previous = EditorBuildSettings.scenes;
            EditorBuildSettingsScene[] updated = new EditorBuildSettingsScene[previous.Length + 1];
            Array.Copy(previous, updated, previous.Length);
            updated[^1] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
#endif
