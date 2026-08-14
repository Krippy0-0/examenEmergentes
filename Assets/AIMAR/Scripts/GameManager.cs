using System;
using UnityEngine;
using UnityEngine.UI;

namespace AIMAR
{
    public enum GamePhase
    {
        /// <summary>Colocando el campo sobre la pared. No se dispara ni corre el tiempo.</summary>
        Setup,

        /// <summary>Sesión en curso: el tiempo corre y se puede disparar.</summary>
        Playing,

        /// <summary>Tiempo agotado. Panel de resultados a la vista.</summary>
        Finished
    }

    public sealed class GameManager : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField, Min(1f)] private float sessionDuration = 30f;
        [SerializeField, Min(1)] private int pointsPerHit = 100;

        [Header("Contenido")]
        [Tooltip("Raíz del campo de entrenamiento. Al confirmar la colocación se " +
                 "desprende del ImageTarget y queda fija en el mundo.")]
        [SerializeField] private Transform arContent;

        [Header("HUD")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text instructionText;

        [Header("Fases")]
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject fireButton;
        [SerializeField] private GameObject finalPanel;
        [SerializeField] private Text finalText;

        [Header("Mensajes")]
        [SerializeField] private string setupMessage = "Apunta el marcador a la pared y presiona COLOCAR";
        [SerializeField] private string playMessage = "Apunta con la retícula y presiona FUEGO";

        /// <summary>
        /// Se dispara al comenzar cada sesión. Las dianas se suscriben para
        /// volver a su posición y apariencia original.
        /// </summary>
        public event Action SessionStarted;

        public GamePhase Phase { get; private set; }
        public int Score { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public float TimeRemaining { get; private set; }

        public bool IsSessionActive => Phase == GamePhase.Playing;

        /// <summary>
        /// Precisión simple en porcentaje. Devuelve 0 sin disparos para no
        /// dividir por cero al terminar una sesión en la que no se disparó.
        /// </summary>
        public float Accuracy => Shots > 0 ? (float)Hits / Shots * 100f : 0f;

        private Transform contentParent;
        private Vector3 contentLocalPosition;
        private Quaternion contentLocalRotation;
        private Vector3 contentLocalScale;

        private void Awake()
        {
            if (arContent != null)
            {
                contentParent = arContent.parent;
                contentLocalPosition = arContent.localPosition;
                contentLocalRotation = arContent.localRotation;
                contentLocalScale = arContent.localScale;
            }
        }

        private void Start()
        {
            EnterSetup();
        }

        private void Update()
        {
            if (Phase != GamePhase.Playing)
            {
                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0f)
            {
                Phase = GamePhase.Finished;
                ShowFinalPanel();
            }

            RefreshHud();
        }

        public void Configure(
            Transform content,
            Text scoreLabel,
            Text timeLabel,
            Text instructionLabel,
            GameObject setup,
            GameObject fire,
            GameObject panel,
            Text panelLabel)
        {
            arContent = content;
            scoreText = scoreLabel;
            timeText = timeLabel;
            instructionText = instructionLabel;
            setupPanel = setup;
            fireButton = fire;
            finalPanel = panel;
            finalText = panelLabel;
        }

        /// <summary>
        /// Vuelve a la etapa de colocación: el campo se reengancha al marcador
        /// y sigue sus movimientos hasta que se confirme de nuevo.
        /// </summary>
        public void EnterSetup()
        {
            Phase = GamePhase.Setup;
            ReattachContent();

            Score = 0;
            Shots = 0;
            Hits = 0;
            TimeRemaining = sessionDuration;

            SessionStarted?.Invoke();
            ApplyPhaseUi();
            RefreshHud();
        }

        /// <summary>
        /// Fija el campo donde esté en ese momento y arranca la sesión. A partir
        /// de aquí el contenido ya no depende del marcador: podés bajar la cámara
        /// y las dianas siguen en la pared.
        /// </summary>
        public void ConfirmPlacement()
        {
            if (Phase != GamePhase.Setup)
            {
                return;
            }

            if (arContent != null)
            {
                arContent.SetParent(null, true);
            }

            StartSession();
        }

        /// <summary>Repite la sesión conservando la colocación ya confirmada.</summary>
        public void ResetSession()
        {
            if (Phase == GamePhase.Setup)
            {
                return;
            }

            StartSession();
        }

        public bool RegisterShot()
        {
            if (Phase != GamePhase.Playing)
            {
                return false;
            }

            Shots++;
            return true;
        }

        public void RegisterHit()
        {
            if (Phase != GamePhase.Playing)
            {
                return;
            }

            Hits++;
            Score += pointsPerHit;
            RefreshHud();
        }

        public void RegisterMiss()
        {
            // El intento ya fue contabilizado por RegisterShot.
            RefreshHud();
        }

        private void StartSession()
        {
            Phase = GamePhase.Playing;

            Score = 0;
            Shots = 0;
            Hits = 0;
            TimeRemaining = sessionDuration;

            SessionStarted?.Invoke();
            ApplyPhaseUi();
            RefreshHud();
        }

        private void ReattachContent()
        {
            if (arContent == null || contentParent == null)
            {
                return;
            }

            arContent.SetParent(contentParent, false);
            arContent.localPosition = contentLocalPosition;
            arContent.localRotation = contentLocalRotation;
            arContent.localScale = contentLocalScale;
        }

        private void ApplyPhaseUi()
        {
            if (setupPanel != null)
            {
                setupPanel.SetActive(Phase == GamePhase.Setup);
            }

            if (fireButton != null)
            {
                fireButton.SetActive(Phase == GamePhase.Playing);
            }

            if (finalPanel != null)
            {
                finalPanel.SetActive(Phase == GamePhase.Finished);
            }

            if (instructionText != null)
            {
                instructionText.text = Phase == GamePhase.Setup ? setupMessage : playMessage;
            }
        }

        private void ShowFinalPanel()
        {
            RefreshHud();

            if (finalText != null)
            {
                finalText.text =
                    $"SESIÓN TERMINADA\n\nPuntaje: {Score}\nImpactos: {Hits}\nIntentos: {Shots}\nPrecisión: {Accuracy:0.#}%";
            }

            ApplyPhaseUi();
        }

        private void RefreshHud()
        {
            if (scoreText != null)
            {
                scoreText.text = $"Puntaje: {Score}";
            }

            if (timeText != null)
            {
                timeText.text = $"Tiempo: {Mathf.CeilToInt(TimeRemaining)}";
            }
        }
    }
}
