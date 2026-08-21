using System.Collections;
using UnityEngine;

namespace AIMAR
{
    public sealed class Target : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private TargetSpawner spawner;
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Collider[] targetColliders;
        [SerializeField] private ParticleSystem hitParticles;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private Color hitColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField, Min(0.05f)] private float responseDuration = 0.25f;

        private FloatingTarget floating;
        private Color[] originalColors;
        private Vector3 originalScale;
        private Vector3 difficultyScale;
        private Vector3 configuredScale;
        private float adaptiveScale = 1f;
        private bool responding;
        private bool interactable;
        private float activatedAt;

        private void Awake()
        {
            floating = GetComponent<FloatingTarget>();
            CacheComponents();
            EnsureFeedback();
        }

        public void Configure(GameManager manager) => gameManager = manager;
        public void ConfigureSpawner(TargetSpawner targetSpawner) => spawner = targetSpawner;

        public void ConfigureDifficulty(float lifetimeSeconds, float scaleMultiplier, float speedMultiplier)
        {
            difficultyScale = originalScale * scaleMultiplier;
            ApplyAdaptiveScale();
            floating?.SetSpeedMultiplier(speedMultiplier);
        }

        public void SetAdaptiveScale(float multiplier)
        {
            adaptiveScale = Mathf.Clamp(multiplier, 0.6f, 1.3f);
            ApplyAdaptiveScale();
        }

        public void ActivateTarget()
        {
            responding = false;
            interactable = true;
            activatedAt = Time.time;
            RestoreAppearance();
            SetRenderersEnabled(true);
            SetCollidersEnabled(true);
        }

        public void EnsureVisible()
        {
            if (!gameObject.activeSelf) gameObject.SetActive(true);
            SetRenderersEnabled(true);
            if (interactable && !responding) SetCollidersEnabled(true);
        }

        public void SetInteractable(bool value)
        {
            interactable = value;
            SetCollidersEnabled(value);
        }

        public bool ReceiveHit(Vector3 hitPoint)
        {
            if (responding || !interactable || gameManager == null || !gameManager.IsSessionActive) return false;

            Vector3 local = transform.InverseTransformPoint(hitPoint);
            float radialDistance = new Vector2(local.x, local.z).magnitude;
            float centerFactor = 1f - Mathf.Clamp01(radialDistance / 0.24f);
            gameManager.RegisterHit(Time.time - activatedAt, centerFactor);
            StartCoroutine(ShowHitResponse());
            return true;
        }

        public void SyncFloatingBase() => floating?.SetBasePosition(transform.localPosition);

        private void CacheComponents()
        {
            if (targetRenderers == null || targetRenderers.Length == 0) targetRenderers = GetComponentsInChildren<Renderer>(true);
            if (targetColliders == null || targetColliders.Length == 0) targetColliders = GetComponentsInChildren<Collider>(true);
            originalScale = transform.localScale;
            difficultyScale = originalScale;
            configuredScale = originalScale;
            originalColors = new Color[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++) originalColors[i] = targetRenderers[i].material.color;
        }

        private IEnumerator ShowHitResponse()
        {
            responding = true;
            interactable = false;
            SetCollidersEnabled(false);
            foreach (Renderer targetRenderer in targetRenderers) targetRenderer.material.color = hitColor;
            transform.localScale *= 0.82f;
            if (hitParticles != null) hitParticles.Play();
            PlayImpactTone();
            yield return new WaitForSeconds(responseDuration);
            RestoreAppearance();
            spawner?.Relocate(this);
        }

        private void RestoreAppearance()
        {
            for (int i = 0; i < targetRenderers.Length && i < originalColors.Length; i++)
                targetRenderers[i].material.color = originalColors[i];
            transform.localScale = configuredScale;
        }

        private void ApplyAdaptiveScale()
        {
            configuredScale = difficultyScale * adaptiveScale;
            if (!responding) transform.localScale = configuredScale;
        }

        private void EnsureFeedback()
        {
            if (hitParticles == null) hitParticles = GetComponentInChildren<ParticleSystem>(true);
            if (audioSource == null) audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0.35f;
        }

        private void PlayImpactTone()
        {
            const int sampleRate = 22050;
            const float duration = 0.09f;
            int count = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[count];
            for (int i = 0; i < count; i++)
            {
                float t = (float)i / sampleRate;
                samples[i] = Mathf.Sin(2f * Mathf.PI * 720f * t) * Mathf.Exp(-28f * t) * 0.28f;
            }
            AudioClip clip = AudioClip.Create("AIMAR_Impact", count, 1, sampleRate, false);
            clip.SetData(samples, 0);
            audioSource.PlayOneShot(clip);
            Destroy(clip, duration + 0.1f);
        }

        private void SetCollidersEnabled(bool value)
        {
            foreach (Collider targetCollider in targetColliders)
                if (targetCollider != null) targetCollider.enabled = value;
        }

        private void SetRenderersEnabled(bool value)
        {
            foreach (Renderer targetRenderer in targetRenderers)
                if (targetRenderer != null) targetRenderer.enabled = value;
        }
    }
}
