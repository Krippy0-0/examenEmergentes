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

        private Color[] originalColors;
        private Vector3 originalScale;
        private bool responding;

        private void Awake()
        {
            CacheComponents();
        }

        public void Configure(GameManager manager)
        {
            gameManager = manager;
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

            for (int i = 0; i < targetRenderers.Length; i++)
            {
                targetRenderers[i].material.color = originalColors[i];
            }

            transform.localScale = originalScale;
            SetCollidersEnabled(true);
            responding = false;
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
