using UnityEngine;
using UnityEngine.UI;

namespace AIMAR
{
    public sealed class OffscreenTargetIndicator : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Target[] targets;
        [SerializeField] private RectTransform[] arrows;
        [SerializeField] private float edgePadding = 96f;
        [SerializeField] private float safeMargin = 120f;

        public void Configure(Camera cameraToUse, GameManager manager, Target[] targetPool, RectTransform[] arrowPool)
        {
            arCamera = cameraToUse;
            gameManager = manager;
            targets = targetPool;
            arrows = arrowPool;
        }

        private void LateUpdate()
        {
            if (targets == null || arrows == null) return;
            bool enabled360 = gameManager != null && gameManager.IsSessionActive && gameManager.Mode == TrainingMode.Grados360;

            for (int i = 0; i < arrows.Length; i++)
            {
                RectTransform arrow = arrows[i];
                Target target = i < targets.Length ? targets[i] : null;
                if (!enabled360 || target == null || !target.gameObject.activeInHierarchy || arCamera == null)
                {
                    arrow.gameObject.SetActive(false);
                    continue;
                }

                Vector3 screen = arCamera.WorldToScreenPoint(target.transform.position);
                bool behind = screen.z < 0f;
                if (behind)
                {
                    screen.x = Screen.width - screen.x;
                    screen.y = Screen.height - screen.y;
                }

                // La flecha se mantiene hasta que el centro de la diana entra
                // completamente en una zona alcanzable, no apenas en el borde.
                bool visible = !behind &&
                    screen.x >= safeMargin && screen.x <= Screen.width - safeMargin &&
                    screen.y >= safeMargin && screen.y <= Screen.height - safeMargin;
                arrow.gameObject.SetActive(!visible);
                if (visible) continue;

                Vector2 center = new Vector2(Screen.width, Screen.height) * 0.5f;
                Vector2 direction = ((Vector2)screen - center).normalized;
                if (direction.sqrMagnitude < 0.001f) direction = Vector2.up;
                Vector2 half = center - Vector2.one * edgePadding;
                float scale = Mathf.Min(
                    Mathf.Abs(direction.x) > 0.001f ? half.x / Mathf.Abs(direction.x) : float.MaxValue,
                    Mathf.Abs(direction.y) > 0.001f ? half.y / Mathf.Abs(direction.y) : float.MaxValue);
                arrow.position = center + direction * scale;
                arrow.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }
        }
    }
}
