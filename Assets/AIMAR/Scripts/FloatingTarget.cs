using UnityEngine;

namespace AIMAR
{
    public sealed class FloatingTarget : MonoBehaviour
    {
        [Header("Rotación")]
        [SerializeField] private float rotationSpeed = 35f;

        [Header("Oscilación vertical")]
        [SerializeField, Min(0f)] private float floatAmplitude = 0.025f;
        [SerializeField, Min(0f)] private float floatSpeed = 1.1f;

        [Header("Órbita horizontal")]
        [SerializeField, Min(0f)] private float orbitRadius = 0f;
        [SerializeField, Min(0f)] private float orbitSpeed = 0f;

        [Header("Desfase")]
        [SerializeField] private float phaseOffset = 0f;

        private Vector3 basePosition;
        private float elapsed;

        public Vector3 BasePosition => basePosition;

        private void Awake()
        {
            basePosition = transform.localPosition;
            elapsed = phaseOffset;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (!Mathf.Approximately(rotationSpeed, 0f))
            {
                transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.Self);
            }

            Vector3 offset = Vector3.zero;
            offset.y = Mathf.Sin(elapsed * floatSpeed) * floatAmplitude;

            if (orbitRadius > 0f)
            {
                float angle = elapsed * orbitSpeed;
                offset.x = Mathf.Cos(angle) * orbitRadius;
                offset.z = Mathf.Sin(angle) * orbitRadius;
            }

            transform.localPosition = basePosition + offset;
        }

        /// <summary>
        /// Reubica el centro alrededor del cual oscila la diana.
        /// La usa <see cref="Target"/> tras un impacto para no pelear con esta animación.
        /// </summary>
        public void SetBasePosition(Vector3 localPosition)
        {
            basePosition = localPosition;
        }

        public void Configure(
            float rotation,
            float amplitude,
            float verticalSpeed,
            float orbit,
            float orbitalSpeed,
            float phase)
        {
            rotationSpeed = rotation;
            floatAmplitude = amplitude;
            floatSpeed = verticalSpeed;
            orbitRadius = orbit;
            orbitSpeed = orbitalSpeed;
            phaseOffset = phase;
        }
    }
}
