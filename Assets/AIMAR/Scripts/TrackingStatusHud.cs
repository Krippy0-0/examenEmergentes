using UnityEngine;
using UnityEngine.UI;

namespace AIMAR
{
    /// <summary>
    /// Muestra el estado del seguimiento en el HUD.
    /// No depende de Vuforia: el builder conecta los eventos del observador a
    /// <see cref="ShowTracked"/> y <see cref="ShowSearching"/>, de modo que
    /// Assembly-CSharp compila aunque el paquete no esté presente.
    /// </summary>
    public sealed class TrackingStatusHud : MonoBehaviour
    {
        [SerializeField] private Text statusText;
        [SerializeField] private string searchingMessage = "Buscando marcador";
        [SerializeField] private string trackedMessage = "Marcador detectado";
        [SerializeField] private Color searchingColor = new Color(1f, 0.78f, 0.24f, 1f);
        [SerializeField] private Color trackedColor = new Color(0.35f, 1f, 0.5f, 1f);

        private void Start()
        {
            ShowSearching();
        }

        public void Configure(Text label)
        {
            statusText = label;
            Apply(searchingMessage, searchingColor);
        }

        public void ShowTracked()
        {
            Apply(trackedMessage, trackedColor);
        }

        public void ShowSearching()
        {
            Apply(searchingMessage, searchingColor);
        }

        private void Apply(string message, Color color)
        {
            if (statusText == null)
            {
                return;
            }

            statusText.text = message;
            statusText.color = color;
        }
    }
}
