using System;
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
            RaycastHit[] hits = Physics.RaycastAll(ray, maxDistance, targetLayer, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));
            foreach (RaycastHit hit in hits)
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
