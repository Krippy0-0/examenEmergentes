#if UNITY_EDITOR
using System;
using System.IO;
using System.Reflection;
using AIMAR;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
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

        // El ImageTarget tiene la imagen en el plano XZ y su eje Y es la normal.
        // Con el marcador colgado en una pared eso significa:
        //   X local -> horizontal sobre la pared
        //   Z local -> vertical sobre la pared   (hacia arriba)
        //   Y local -> sale de la pared hacia el jugador
        // Toda la escena se compone con esa lectura. Si al probar quedara al
        // revés en vertical, basta invertir este vector.
        private static readonly Vector3 WallUp = Vector3.forward;

        // Área local donde una diana puede reaparecer tras ser alcanzada.
        // X e Z se mueven sobre la pared; Y es cuánto sobresale de ella.
        private static readonly Vector3 RelocationAreaMin = new Vector3(-0.38f, 0.12f, -0.30f);
        private static readonly Vector3 RelocationAreaMax = new Vector3(0.38f, 0.22f, 0.30f);
        private const float MinimumRelocationDistance = 0.20f;

        private struct TargetSetup
        {
            public string Name;
            public Vector3 LocalPosition;
            public float Scale;
            public float RotationSpeed;
            public float FloatAmplitude;
            public float FloatSpeed;
            public float OrbitRadius;
            public float OrbitSpeed;
            public float Phase;
        }

        private static readonly TargetSetup[] Targets =
        {
            new TargetSetup
            {
                Name = "Target_01",
                LocalPosition = new Vector3(-0.36f, 0.14f, -0.22f),
                Scale = 0.85f,
                RotationSpeed = 30f,
                FloatAmplitude = 0.022f,
                FloatSpeed = 1.0f,
                OrbitRadius = 0f,
                OrbitSpeed = 0f,
                Phase = 0f
            },
            new TargetSetup
            {
                Name = "Target_02",
                LocalPosition = new Vector3(0.02f, 0.20f, 0.02f),
                Scale = 0.70f,
                RotationSpeed = -45f,
                FloatAmplitude = 0.035f,
                FloatSpeed = 1.45f,
                OrbitRadius = 0.045f,
                OrbitSpeed = 0.9f,
                Phase = 1.6f
            },
            new TargetSetup
            {
                Name = "Target_03",
                LocalPosition = new Vector3(0.34f, 0.15f, 0.24f),
                Scale = 0.78f,
                RotationSpeed = 60f,
                FloatAmplitude = 0.018f,
                FloatSpeed = 0.8f,
                OrbitRadius = 0.030f,
                OrbitSpeed = 1.3f,
                Phase = 3.1f
            }
        };

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

            Scene scene = File.Exists(ScenePath)
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
            CreateDecor(arContent);

            GameObject prefab = CreateTargetPrefab();
            foreach (TargetSetup setup in Targets)
            {
                CreateTargetInstance(prefab, arContent, gameManager, setup);
            }

            HudReferences hud = CreateHud(gameManager, shooter, arContent);
            WireTrackingStatus(imageTarget, hud.StatusHud);

            Camera arCamera = arCameraObject.GetComponent<Camera>();
            shooter.Configure(arCamera, gameManager, 1 << TargetLayer);

            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(shooter);

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
                    "Entrenamiento.unity contiene ARCamera, ImageTarget, plataforma, decorado, tres dianas con " +
                    "movimiento, raycast, HUD, panel final y botón Reiniciar.\n\n" +
                    "Verifica en la configuración de Vuforia que la licencia esté asignada y prueba con Play.",
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
            Transform platform = new GameObject("Plataforma").transform;
            platform.SetParent(parent, false);

            Material deck = CreateMaterial(Root + "/Materials/Platform.mat", new Color(0.08f, 0.22f, 0.34f, 1f));
            Material rail = CreateMaterial(Root + "/Materials/PlatformRail.mat", new Color(0.85f, 0.62f, 0.12f, 1f));

            CreateBlock(platform, "Base", new Vector3(0f, 0.015f, 0f), new Vector3(1.30f, 0.03f, 0.90f), deck);
            CreateBlock(platform, "Riel_Fondo", new Vector3(0f, 0.055f, -0.425f), new Vector3(1.30f, 0.05f, 0.05f), rail);
            CreateBlock(platform, "Riel_Izquierdo", new Vector3(-0.625f, 0.055f, 0f), new Vector3(0.05f, 0.05f, 0.90f), rail);
            CreateBlock(platform, "Riel_Derecho", new Vector3(0.625f, 0.055f, 0f), new Vector3(0.05f, 0.05f, 0.90f), rail);
        }

        private static void CreateDecor(Transform parent)
        {
            Transform decor = new GameObject("Decorado").transform;
            decor.SetParent(parent, false);

            Material crate = CreateMaterial(Root + "/Materials/Crate.mat", new Color(0.45f, 0.30f, 0.16f, 1f));
            Material metal = CreateMaterial(Root + "/Materials/Metal.mat", new Color(0.55f, 0.58f, 0.62f, 1f));
            Material flag = CreateMaterial(Root + "/Materials/Flag.mat", new Color(0.90f, 0.30f, 0.10f, 1f));

            // Cajas apoyadas contra la pared: se apilan hacia arriba (Z local) y
            // sobresalen poco (Y local). Apilarlas en Y las haría crecer hacia
            // el jugador en vez de hacia arriba.
            CreateBlock(decor, "Caja_01", new Vector3(-0.46f, 0.075f, -0.34f), new Vector3(0.17f, 0.09f, 0.17f), crate);
            CreateBlock(decor, "Caja_02", new Vector3(-0.46f, 0.070f, -0.17f), new Vector3(0.15f, 0.08f, 0.15f), crate);
            CreateBlock(decor, "Caja_03", new Vector3(-0.27f, 0.065f, -0.36f), new Vector3(0.13f, 0.07f, 0.13f), crate);

            // Mástil tumbado sobre la pared: el cilindro nace con su eje en Y,
            // así que se gira 90° en X para que corra en vertical (Z local).
            CreateCylinder(decor, "Mastil", new Vector3(0.50f, 0.042f, -0.02f),
                new Vector3(0.014f, 0.20f, 0.014f), metal, new Vector3(90f, 0f, 0f));

            // Banderín plano contra la pared, junto al extremo alto del mástil.
            CreateBlock(decor, "Banderin", new Vector3(0.565f, 0.048f, 0.145f),
                new Vector3(0.12f, 0.010f, 0.075f), flag);
        }

        private static GameObject CreateBlock(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = localPosition;
            block.transform.localRotation = Quaternion.Euler(localEuler);
            block.transform.localScale = localScale;
            Object.DestroyImmediate(block.GetComponent<Collider>());
            block.GetComponent<Renderer>().sharedMaterial = material;
            return block;
        }

        private static GameObject CreateCylinder(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEuler = default)
        {
            GameObject cylinder = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cylinder.name = name;
            cylinder.transform.SetParent(parent, false);
            cylinder.transform.localPosition = localPosition;
            cylinder.transform.localRotation = Quaternion.Euler(localEuler);
            cylinder.transform.localScale = localScale;
            Object.DestroyImmediate(cylinder.GetComponent<Collider>());
            cylinder.GetComponent<Renderer>().sharedMaterial = material;
            return cylinder;
        }

        private static void CreateTargetInstance(GameObject prefab, Transform parent, GameManager gameManager, TargetSetup setup)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            instance.name = setup.Name;
            instance.transform.localPosition = setup.LocalPosition;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * setup.Scale;

            Target target = instance.GetComponent<Target>();
            target.Configure(gameManager);
            target.ConfigureRelocation(RelocationAreaMin, RelocationAreaMax, MinimumRelocationDistance);

            FloatingTarget floating = instance.GetComponent<FloatingTarget>();
            floating.Configure(
                setup.RotationSpeed,
                setup.FloatAmplitude,
                setup.FloatSpeed,
                setup.OrbitRadius,
                setup.OrbitSpeed,
                setup.Phase,
                WallUp);

            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            PrefabUtility.RecordPrefabInstancePropertyModifications(floating);
            EditorUtility.SetDirty(target);
            EditorUtility.SetDirty(floating);
        }

        private static GameObject CreateTargetPrefab()
        {
            GameObject root = new GameObject("Target");
            root.layer = TargetLayer;

            // El collider cubre los discos y la aguja indicadora, sin sobresalir.
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 0.048f, 0f);
            collider.size = new Vector3(0.48f, 0.146f, 0.48f);

            root.AddComponent<Target>();
            root.AddComponent<FloatingTarget>();

            CreateTargetDisc(root.transform, "Outer_Red", 0.48f, 0f,
                CreateMaterial(Root + "/Materials/TargetRed.mat", new Color(0.84f, 0.1f, 0.07f, 1f)));
            CreateTargetDisc(root.transform, "Middle_White", 0.32f, 0.035f,
                CreateMaterial(Root + "/Materials/TargetWhite.mat", new Color(0.95f, 0.95f, 0.92f, 1f)));
            CreateTargetDisc(root.transform, "Center_Red", 0.15f, 0.07f,
                CreateMaterial(Root + "/Materials/TargetCenter.mat", new Color(0.72f, 0.04f, 0.03f, 1f)));

            // Marca descentrada: hace visible la rotación, que en discos
            // concéntricos sería imperceptible.
            GameObject indicator = CreateBlock(root.transform, "Indicador",
                new Vector3(0.18f, 0.095f, 0f), new Vector3(0.05f, 0.05f, 0.05f),
                CreateMaterial(Root + "/Materials/TargetIndicator.mat", new Color(0.98f, 0.80f, 0.15f, 1f)));
            indicator.layer = TargetLayer;

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

        private sealed class HudReferences
        {
            public TrackingStatusHud StatusHud;
        }

        private static HudReferences CreateHud(GameManager gameManager, ShooterController shooter, Transform arContent)
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            GameObject canvasObject = new GameObject("AIMAR_HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            // Banda oscura detrás del marcador y el tiempo: sin ella el texto
            // blanco se pierde sobre el video de la cámara.
            CreateTopBar(canvas.transform, 230f, new Color(0f, 0f, 0f, 0.55f));

            Text score = CreateText(canvas.transform, "ScoreText", "Puntaje: 0", font, 46, TextAnchor.MiddleLeft);
            SetRect(score.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(40f, -35f), new Vector2(430f, 85f));

            Text time = CreateText(canvas.transform, "TimeText", "Tiempo: 30", font, 46, TextAnchor.MiddleRight);
            SetRect(time.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-40f, -35f), new Vector2(430f, 85f));

            Text status = CreateText(canvas.transform, "StatusText", "Buscando marcador", font, 34, TextAnchor.MiddleCenter);
            status.color = new Color(1f, 0.78f, 0.24f, 1f);
            SetRect(status.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -130f), new Vector2(700f, 60f));

            Text instruction = CreateText(canvas.transform, "InstructionText", "Apunta con la retícula y presiona FUEGO", font, 30, TextAnchor.MiddleCenter);
            SetRect(instruction.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(900f, 60f));

            CreateCrosshair(canvas.transform);

            GameObject setupPanel = CreateSetupPanel(canvas.transform, font, gameManager);

            Button fireButton = CreateFireButton(canvas.transform, font);
            UnityEventTools.AddPersistentListener(fireButton.onClick, shooter.Shoot);

            GameObject finalPanel = CreateFinalPanel(canvas.transform, font, gameManager, out Text finalText);

            gameManager.Configure(
                arContent,
                score,
                time,
                instruction,
                setupPanel,
                fireButton.gameObject,
                finalPanel,
                finalText);

            TrackingStatusHud statusHud = canvasObject.AddComponent<TrackingStatusHud>();
            statusHud.Configure(status);

            GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            eventSystem.transform.SetAsLastSibling();

            return new HudReferences { StatusHud = statusHud };
        }

        /// <summary>
        /// Etapa previa al juego: mientras esté visible, el campo sigue al
        /// marcador y no se puede disparar ni corre el tiempo. Al confirmar,
        /// el campo queda fijo en la pared y arranca la sesión.
        /// </summary>
        private static GameObject CreateSetupPanel(Transform parent, Font font, GameManager gameManager)
        {
            GameObject panel = new GameObject("SetupPanel", typeof(RectTransform));
            panel.transform.SetParent(parent, false);

            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 60f);
            rect.sizeDelta = new Vector2(760f, 330f);

            GameObject backing = new GameObject("Backing", typeof(RectTransform), typeof(UIImage));
            backing.transform.SetParent(panel.transform, false);
            backing.GetComponent<UIImage>().color = new Color(0f, 0f, 0f, 0.55f);
            backing.GetComponent<UIImage>().raycastTarget = false;
            StretchFull(backing.GetComponent<RectTransform>());

            Text hint = CreateText(panel.transform, "SetupHint",
                "Encuadra el marcador en la pared.\nCuando el campo esté donde querés, confirma.",
                font, 32, TextAnchor.UpperCenter);
            RectTransform hintRect = hint.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -28f);
            hintRect.sizeDelta = new Vector2(680f, 130f);

            Button confirm = CreateCardButton(panel.transform, font, "ConfirmButton", "COLOCAR",
                new Color(0.12f, 0.62f, 0.30f, 0.98f), new Vector2(0f, 40f), new Vector2(420f, 140f), 48);
            UnityEventTools.AddPersistentListener(confirm.onClick, gameManager.ConfirmPlacement);

            panel.SetActive(true);
            return panel;
        }

        private static Button CreateCardButton(Transform parent, Font font, string name, string label, Color color, Vector2 anchoredPosition, Vector2 size, int fontSize)
        {
            GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(UIImage), typeof(Button));
            buttonObject.transform.SetParent(parent, false);
            buttonObject.GetComponent<UIImage>().color = color;

            RectTransform rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = CreateText(buttonObject.transform, "Label", label, font, fontSize, TextAnchor.MiddleCenter);
            StretchFull(text.rectTransform);

            return buttonObject.GetComponent<Button>();
        }

        private static GameObject CreateFinalPanel(Transform parent, Font font, GameManager gameManager, out Text finalText)
        {
            GameObject panel = new GameObject("FinalPanel", typeof(RectTransform), typeof(UIImage));
            panel.transform.SetParent(parent, false);
            UIImage backdrop = panel.GetComponent<UIImage>();
            backdrop.color = new Color(0f, 0f, 0f, 0.78f);
            backdrop.raycastTarget = true; // bloquea el botón FUEGO al terminar
            StretchFull(panel.GetComponent<RectTransform>());

            GameObject card = new GameObject("Card", typeof(RectTransform), typeof(UIImage));
            card.transform.SetParent(panel.transform, false);
            card.GetComponent<UIImage>().color = new Color(0.06f, 0.14f, 0.22f, 0.97f);
            RectTransform cardRect = card.GetComponent<RectTransform>();
            cardRect.anchorMin = new Vector2(0.5f, 0.5f);
            cardRect.anchorMax = new Vector2(0.5f, 0.5f);
            cardRect.pivot = new Vector2(0.5f, 0.5f);
            cardRect.anchoredPosition = Vector2.zero;
            cardRect.sizeDelta = new Vector2(820f, 620f);

            finalText = CreateText(card.transform, "FinalText", "SESIÓN TERMINADA", font, 46, TextAnchor.MiddleCenter);
            RectTransform finalRect = finalText.rectTransform;
            finalRect.anchorMin = new Vector2(0.5f, 1f);
            finalRect.anchorMax = new Vector2(0.5f, 1f);
            finalRect.pivot = new Vector2(0.5f, 1f);
            finalRect.anchoredPosition = new Vector2(0f, -60f);
            finalRect.sizeDelta = new Vector2(720f, 340f);

            // Reiniciar conserva la colocación ya confirmada, para poder
            // encadenar sesiones sin volver a apuntar el marcador.
            Button restart = CreateCardButton(card.transform, font, "RestartButton", "REINICIAR",
                new Color(0.12f, 0.62f, 0.30f, 0.98f), new Vector2(-175f, 60f), new Vector2(320f, 130f), 38);
            UnityEventTools.AddPersistentListener(restart.onClick, gameManager.ResetSession);

            // Recolocar vuelve a enganchar el campo al marcador.
            Button replace = CreateCardButton(card.transform, font, "RelocateButton", "RECOLOCAR",
                new Color(0.20f, 0.36f, 0.55f, 0.98f), new Vector2(175f, 60f), new Vector2(320f, 130f), 38);
            UnityEventTools.AddPersistentListener(replace.onClick, gameManager.EnterSetup);

            panel.SetActive(false);
            return panel;
        }

        private static void CreateTopBar(Transform parent, float height, Color color)
        {
            GameObject bar = new GameObject("TopBar", typeof(RectTransform), typeof(UIImage));
            bar.transform.SetParent(parent, false);
            UIImage image = bar.GetComponent<UIImage>();
            image.color = color;
            image.raycastTarget = false;

            RectTransform rect = bar.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(0f, height);
        }

        private static void StretchFull(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Text CreateText(Transform parent, string name, string value, Font font, int size, TextAnchor alignment)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            textObject.transform.SetParent(parent, false);

            Text text = textObject.GetComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = size;
            text.color = Color.white;
            text.alignment = alignment;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;

            Outline outline = textObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(2.5f, -2.5f);

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
            StretchFull(label.rectTransform);

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

        /// <summary>
        /// Conecta el texto de estado a los eventos del observador de Vuforia.
        /// Usa reflexión a propósito: si la versión instalada renombra esos
        /// campos, el builder sigue compilando y solo se pierde el aviso.
        /// </summary>
        private static void WireTrackingStatus(ImageTargetBehaviour imageTarget, TrackingStatusHud statusHud)
        {
            DefaultObserverEventHandler handler = imageTarget.GetComponent<DefaultObserverEventHandler>();
            if (handler == null)
            {
                Debug.LogWarning("AIM-AR: el ImageTarget no tiene DefaultObserverEventHandler. El texto de estado quedará fijo en 'Buscando marcador'.");
                return;
            }

            bool found = TryBindEvent(handler, "OnTargetFound", statusHud.ShowTracked);
            bool lost = TryBindEvent(handler, "OnTargetLost", statusHud.ShowSearching);

            if (found && lost)
            {
                EditorUtility.SetDirty(handler);
                return;
            }

            Debug.LogWarning("AIM-AR: no se pudieron conectar OnTargetFound/OnTargetLost en esta versión de Vuforia. El texto de estado quedará fijo; no afecta al resto del prototipo.");
        }

        private static bool TryBindEvent(Component handler, string fieldName, UnityAction callback)
        {
            FieldInfo field = handler.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance);
            UnityEvent unityEvent = field?.GetValue(handler) as UnityEvent;
            if (unityEvent == null)
            {
                return false;
            }

            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                UnityEventTools.RemovePersistentListener(unityEvent, i);
            }

            UnityEventTools.AddPersistentListener(unityEvent, callback);
            return true;
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
