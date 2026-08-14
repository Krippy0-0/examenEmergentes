using System;
using UnityEngine;
using UnityEngine.UI;

namespace AIMAR
{
    public sealed class GameManager : MonoBehaviour
    {
        [Header("Session")]
        [SerializeField, Min(1f)] private float sessionDuration = 30f;
        [SerializeField, Min(1)] private int pointsPerHit = 100;

        [Header("HUD")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;

        [Header("Panel final")]
        [SerializeField] private GameObject finalPanel;
        [SerializeField] private Text finalText;

        /// <summary>
        /// Se dispara al iniciar o reiniciar la sesión. Las dianas se suscriben
        /// para volver a su posición y apariencia original.
        /// </summary>
        public event Action SessionStarted;

        public int Score { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool IsSessionActive { get; private set; }

        /// <summary>
        /// Precisión simple en porcentaje. Devuelve 0 sin disparos para no
        /// dividir por cero al terminar una sesión en la que no se disparó.
        /// </summary>
        public float Accuracy => Shots > 0 ? (float)Hits / Shots * 100f : 0f;

        private void Start()
        {
            ResetSession();
        }

        private void Update()
        {
            if (!IsSessionActive)
            {
                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0f)
            {
                IsSessionActive = false;
                ShowFinalPanel();
            }

            RefreshHud();
        }

        public void ConfigureHud(Text scoreLabel, Text timeLabel, GameObject panel, Text panelLabel)
        {
            scoreText = scoreLabel;
            timeText = timeLabel;
            finalPanel = panel;
            finalText = panelLabel;
            RefreshHud();
        }

        public void ResetSession()
        {
            Score = 0;
            Shots = 0;
            Hits = 0;
            TimeRemaining = sessionDuration;
            IsSessionActive = true;

            if (finalPanel != null)
            {
                finalPanel.SetActive(false);
            }

            SessionStarted?.Invoke();
            RefreshHud();
        }

        public bool RegisterShot()
        {
            if (!IsSessionActive)
            {
                return false;
            }

            Shots++;
            return true;
        }

        public void RegisterHit()
        {
            if (!IsSessionActive)
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

        private void ShowFinalPanel()
        {
            RefreshHud();

            if (finalText != null)
            {
                finalText.text =
                    $"SESIÓN TERMINADA\n\nPuntaje: {Score}\nImpactos: {Hits}\nIntentos: {Shots}\nPrecisión: {Accuracy:0.#}%";
            }

            if (finalPanel != null)
            {
                finalPanel.SetActive(true);
            }
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
