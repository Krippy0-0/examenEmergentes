using System.Collections.Generic;
using UnityEngine;

namespace AIMAR
{
    public sealed class TargetSpawner : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private Transform arContent;
        [SerializeField] private GameObject flatScenario;
        [SerializeField] private GameObject decor;
        [SerializeField] private Target[] targets;
        [SerializeField, Min(0.5f)] private float ringRadius = 2.2f;
        [SerializeField] private float ringHeight;

        private TrainingMode activeMode;
        private DifficultyLevel activeDifficulty;
        private bool sessionActive;
        private bool adaptiveEnabled;
        private float adaptiveScale = 1f;
        private float nextVisibilityCheck;

        public IReadOnlyList<Target> Targets => targets;
        public float AdaptiveScale => adaptiveScale;

        private void Update()
        {
            if (!sessionActive) return;
            if (activeMode == TrainingMode.Plano && Time.unscaledTime >= nextVisibilityCheck)
            {
                nextVisibilityCheck = Time.unscaledTime + 0.25f;
                EnsureFlatScenarioVisible();
            }
        }

        public void Configure(Camera cameraToUse, Transform content, GameObject scenario, GameObject decorRoot, Target[] targetPool)
        {
            arCamera = cameraToUse;
            arContent = content;
            flatScenario = scenario;
            decor = decorRoot;
            targets = targetPool;
            foreach (Target target in targets) target.ConfigureSpawner(this);
        }

        public void PreviewMode(TrainingMode mode, DifficultyLevel difficulty)
        {
            activeMode = mode;
            activeDifficulty = difficulty;
            SetScenarioVisible(mode == TrainingMode.Plano);
            if (mode == TrainingMode.Plano) ArrangeFlat();
        }

        public void CommitMode(TrainingMode mode, DifficultyLevel difficulty)
        {
            activeMode = mode;
            activeDifficulty = difficulty;
            SetScenarioVisible(mode == TrainingMode.Plano);
            if (mode == TrainingMode.Grados360 && arCamera != null && arContent != null)
            {
                // Cilindro mundial fijo con centro en el jugador. Vuforia ya
                // entrega la pose de la cámara; añadir otra rotación por
                // giroscopio duplicaba/invertía el movimiento observado.
                arContent.SetParent(null, true);
                arContent.position = arCamera.transform.position;
                arContent.rotation = Quaternion.identity;
                arContent.localScale = Vector3.one;
                Arrange360();
            }
            else
            {
                ArrangeFlat();
                EnsureFlatScenarioVisible();
            }
        }

        public void BeginSession(TrainingMode mode, DifficultyLevel difficulty)
        {
            sessionActive = true;
            activeMode = mode;
            activeDifficulty = difficulty;
            CommitMode(mode, difficulty);
            float life = difficulty == DifficultyLevel.Facil ? 5.5f : difficulty == DifficultyLevel.Medio ? 3.8f : 2.6f;
            float scale = difficulty == DifficultyLevel.Facil ? 1.15f : difficulty == DifficultyLevel.Medio ? 0.9f : 0.7f;
            float speed = difficulty == DifficultyLevel.Facil ? 0.7f : difficulty == DifficultyLevel.Medio ? 1f : 1.45f;
            foreach (Target target in targets)
            {
                target.gameObject.SetActive(true);
                target.ConfigureDifficulty(life, scale, speed);
                target.SetAdaptiveScale(adaptiveEnabled ? adaptiveScale : 1f);
                target.ActivateTarget();
            }
            if (mode == TrainingMode.Plano) EnsureFlatScenarioVisible();
        }

        public void EndSession()
        {
            sessionActive = false;
            foreach (Target target in targets) target.SetInteractable(false);
        }

        public void SetAdaptiveEnabled(bool value)
        {
            adaptiveEnabled = value;
            SetAdaptiveScale(value ? adaptiveScale : 1f);
        }

        public void SetAdaptiveScale(float value)
        {
            adaptiveScale = adaptiveEnabled ? Mathf.Clamp(value, 0.62f, 1.28f) : 1f;
            if (targets == null) return;
            foreach (Target target in targets)
                if (target != null) target.SetAdaptiveScale(adaptiveScale);
        }

        public void Relocate(Target target)
        {
            if (target == null) return;
            if (activeMode == TrainingMode.Grados360) Relocate360(target);
            else RelocateFlat(target);
            target.SetAdaptiveScale(adaptiveEnabled ? adaptiveScale : 1f);
            target.ActivateTarget();
        }

        private void ArrangeFlat()
        {
            if (targets == null || targets.Length == 0) return;
            Vector3[] positions =
            {
                new Vector3(-0.36f, 0.14f, -0.22f),
                new Vector3(0.02f, 0.20f, 0.02f),
                new Vector3(0.34f, 0.15f, 0.24f)
            };
            for (int i = 0; i < targets.Length; i++)
            {
                targets[i].transform.localPosition = positions[i % positions.Length];
                targets[i].transform.localRotation = Quaternion.identity;
                targets[i].SyncFloatingBase();
            }
        }

        private void Arrange360()
        {
            if (targets == null || targets.Length == 0) return;
            float start = Random.Range(0f, 120f);
            for (int i = 0; i < targets.Length; i++)
            {
                float angle = (start + i * 360f / targets.Length) * Mathf.Deg2Rad;
                PlaceOnRing(targets[i], angle, ringHeight + Random.Range(-0.42f, 0.42f));
            }
        }

        private void RelocateFlat(Target target)
        {
            Vector3 candidate = target.transform.localPosition;
            float bestSeparation = 0f;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector3 proposed = new Vector3(Random.Range(-0.38f, 0.38f), Random.Range(0.12f, 0.22f), Random.Range(-0.30f, 0.30f));
                float nearest = float.MaxValue;
                foreach (Target other in targets)
                {
                    if (other == null || other == target) continue;
                    Vector2 delta = new Vector2(proposed.x - other.transform.localPosition.x, proposed.z - other.transform.localPosition.z);
                    nearest = Mathf.Min(nearest, delta.magnitude);
                }
                if (nearest > bestSeparation) { bestSeparation = nearest; candidate = proposed; }
                if (nearest >= 0.28f) break;
            }
            target.transform.localPosition = candidate;
            target.transform.localRotation = Quaternion.identity;
            target.SyncFloatingBase();
        }

        private void Relocate360(Target target)
        {
            PlaceOnRing(target, FindSeparatedAngle(target), ringHeight + Random.Range(-0.42f, 0.42f));
        }

        private float FindSeparatedAngle(Target relocatingTarget)
        {
            float bestAngle = Random.Range(0f, Mathf.PI * 2f);
            float bestSeparation = 0f;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float candidate = Random.Range(0f, Mathf.PI * 2f);
                float nearest = Mathf.PI * 2f;
                foreach (Target other in targets)
                {
                    if (other == null || other == relocatingTarget) continue;
                    float otherAngle = Mathf.Atan2(other.transform.localPosition.x, other.transform.localPosition.z);
                    float separation = Mathf.Abs(Mathf.DeltaAngle(candidate * Mathf.Rad2Deg, otherAngle * Mathf.Rad2Deg));
                    nearest = Mathf.Min(nearest, separation * Mathf.Deg2Rad);
                }
                if (nearest > bestSeparation) { bestSeparation = nearest; bestAngle = candidate; }
                if (nearest >= 55f * Mathf.Deg2Rad) break;
            }
            return bestAngle;
        }

        private void PlaceOnRing(Target target, float angle, float height)
        {
            Vector3 local = new Vector3(Mathf.Sin(angle) * ringRadius, height, Mathf.Cos(angle) * ringRadius);
            target.transform.localPosition = local;
            target.transform.localRotation = Quaternion.FromToRotation(Vector3.up, (-local).normalized);
            target.SyncFloatingBase();
        }

        private void SetScenarioVisible(bool value)
        {
            if (flatScenario != null) flatScenario.SetActive(value);
            if (decor != null) decor.SetActive(value);
        }

        private void EnsureFlatScenarioVisible()
        {
            SetScenarioVisible(true);
            EnableRenderers(flatScenario);
            EnableRenderers(decor);
            foreach (Target target in targets)
                if (target != null) target.EnsureVisible();
        }

        private static void EnableRenderers(GameObject root)
        {
            if (root == null) return;
            foreach (Renderer rendererComponent in root.GetComponentsInChildren<Renderer>(true)) rendererComponent.enabled = true;
        }

    }
}
