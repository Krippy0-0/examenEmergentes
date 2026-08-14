using System.Collections;
using UnityEngine;

namespace AIMAR
{
    public sealed class Target : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Renderer[] targetRenderers;
        [SerializeField] private Collider[] targetColliders;
        [SerializeField] private Color hitColor = new Color(0.2f, 1f, 0.35f, 1f);
        [SerializeField, Min(0.05f)] private float responseDuration = 0.35f;

        [Header("Reubicación tras el impacto")]
        [SerializeField] private bool relocateOnHit = true;
        [SerializeField] private Vector3 relocationAreaMin = new Vector3(-0.36f, 0.12f, -0.14f);
        [SerializeField] private Vector3 relocationAreaMax = new Vector3(0.36f, 0.24f, 0.16f);
        [SerializeField, Min(0f)] private float minimumRelocationDistance = 0.18f;

        private FloatingTarget floating;
        private Color[] originalColors;
        private Vector3 originalScale;
        private Vector3 homeLocalPosition;
        private bool responding;

        private void Awake()
        {
            floating = GetComponent<FloatingTarget>();
            homeLocalPosition = transform.localPosition;
            CacheComponents();
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.SessionStarted += HandleSessionStarted;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.SessionStarted -= HandleSessionStarted;
            }
        }

        public void Configure(GameManager manager)
        {
            gameManager = manager;
        }

        public void ConfigureRelocation(Vector3 areaMin, Vector3 areaMax, float minimumDistance)
        {
            relocationAreaMin = areaMin;
            relocationAreaMax = areaMax;
            minimumRelocationDistance = minimumDistance;
        }

        public bool ReceiveHit(Vector3 hitPoint)
        {
            if (responding || gameManager == null || !gameManager.IsSessionActive)
            {
                return false;
            }

            gameManager.RegisterHit();
            StartCoroutine(ShowHitResponse());
            return true;
        }

        private void CacheComponents()
        {
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (targetColliders == null || targetColliders.Length == 0)
            {
                targetColliders = GetComponentsInChildren<Collider>(true);
            }

            originalScale = transform.localScale;
            originalColors = new Color[targetRenderers.Length];
            for (int i = 0; i < targetRenderers.Length; i++)
            {
                originalColors[i] = targetRenderers[i].material.color;
            }
        }

        private IEnumerator ShowHitResponse()
        {
            responding = true;
            SetCollidersEnabled(false);

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                targetRenderers[i].material.color = hitColor;
            }

            transform.localScale = originalScale * 0.82f;
            yield return new WaitForSeconds(responseDuration);

            RestoreAppearance();
            Relocate();
            SetCollidersEnabled(true);
            responding = false;
        }

        private void RestoreAppearance()
        {
            for (int i = 0; i < targetRenderers.Length && i < originalColors.Length; i++)
            {
                targetRenderers[i].material.color = originalColors[i];
            }

            transform.localScale = originalScale;
        }

        private void Relocate()
        {
            if (!relocateOnHit)
            {
                return;
            }

            Vector3 current = floating != null ? floating.BasePosition : transform.localPosition;
            Vector3 candidate = current;

            for (int attempt = 0; attempt < 8; attempt++)
            {
                candidate = new Vector3(
                    Random.Range(relocationAreaMin.x, relocationAreaMax.x),
                    Random.Range(relocationAreaMin.y, relocationAreaMax.y),
                    Random.Range(relocationAreaMin.z, relocationAreaMax.z));

                if (Vector3.Distance(candidate, current) >= minimumRelocationDistance)
                {
                    break;
                }
            }

            ApplyLocalPosition(candidate);
        }

        private void ApplyLocalPosition(Vector3 localPosition)
        {
            transform.localPosition = localPosition;
            if (floating != null)
            {
                floating.SetBasePosition(localPosition);
            }
        }

        private void HandleSessionStarted()
        {
            StopAllCoroutines();
            responding = false;

            if (originalColors != null)
            {
                RestoreAppearance();
            }

            ApplyLocalPosition(homeLocalPosition);
            SetCollidersEnabled(true);
        }

        private void SetCollidersEnabled(bool value)
        {
            foreach (Collider targetCollider in targetColliders)
            {
                if (targetCollider != null)
                {
                    targetCollider.enabled = value;
                }
            }
        }
    }
}
