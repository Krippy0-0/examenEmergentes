using UnityEngine;

namespace AIMAR
{
    public sealed class ShooterController : MonoBehaviour
    {
        [SerializeField] private Camera arCamera;
        [SerializeField] private GameManager gameManager;
        [SerializeField, Min(0.1f)] private float maxDistance = 100f;
        [SerializeField] private LayerMask targetLayer;

        public void Configure(Camera cameraToUse, GameManager manager, LayerMask layerMask)
        {
            arCamera = cameraToUse;
            gameManager = manager;
            targetLayer = layerMask;
        }

        public void Shoot()
        {
            if (arCamera == null || gameManager == null || !gameManager.RegisterShot())
            {
                return;
            }

            Ray ray = new Ray(arCamera.transform.position, arCamera.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayer))
            {
                Target target = hit.collider.GetComponentInParent<Target>();
                if (target != null && target.ReceiveHit(hit.point))
                {
                    return;
                }
            }

            gameManager.RegisterMiss();
        }
    }
}
