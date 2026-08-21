using System;
using UnityEngine;
using UnityEngine.UI;

namespace AIMAR
{
    public enum GamePhase { Hub, Setup, Playing, Finished }
    public enum TrainingMode { Plano, Grados360 }
    public enum DifficultyLevel { Facil, Medio, Dificil }

    public sealed class GameManager : MonoBehaviour
    {
        [Header("Sesión final")]
        [SerializeField, Min(10f)] private float sessionDuration = 60f;
        [SerializeField] private TrainingMode trainingMode = TrainingMode.Plano;
        [SerializeField] private DifficultyLevel difficulty = DifficultyLevel.Medio;
        [SerializeField] private bool adaptiveMode = true;

        [Header("Contenido")]
        [SerializeField] private Transform arContent;
        [SerializeField] private TargetSpawner targetSpawner;

        [Header("HUD")]
        [SerializeField] private Text scoreText;
        [SerializeField] private Text timeText;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text metricsText;
        [SerializeField] private Text modeText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text adaptiveText;

        [Header("Paneles")]
        [SerializeField] private GameObject hubPanel;
        [SerializeField] private GameObject setupPanel;
        [SerializeField] private GameObject fireButton;
        [SerializeField] private GameObject cancelButton;
        [SerializeField] private GameObject finalPanel;
        [SerializeField] private Text finalText;

        public event Action SessionStarted;
        public event Action SessionFinished;

        public GamePhase Phase { get; private set; }
        public TrainingMode Mode => trainingMode;
        public DifficultyLevel Difficulty => difficulty;
        public bool AdaptiveMode => adaptiveMode;
        public int Score { get; private set; }
        public int Shots { get; private set; }
        public int Hits { get; private set; }
        public int CurrentStreak { get; private set; }
        public int BestStreak { get; private set; }
        public float ReactionTotal { get; private set; }
        public float TimeRemaining { get; private set; }
        public bool IsSessionActive => Phase == GamePhase.Playing;
        public float Accuracy => Shots > 0 ? (float)Hits / Shots * 100f : 0f;
        public float AverageReaction => Hits > 0 ? ReactionTotal / Hits : 0f;

        private Transform contentParent;
        private Vector3 contentLocalPosition;
        private Quaternion contentLocalRotation;
        private Vector3 contentLocalScale;
        private ScoreRepository repository;
        private readonly bool[] recentResults = new bool[8];
        private int recentResultCount;
        private int recentResultIndex;

        private void Awake()
        {
            repository = new ScoreRepository();
            CacheContentTransform();
        }

        private void Start() => EnterHub();

        private void Update()
        {
            if (Phase != GamePhase.Playing) return;
            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            if (TimeRemaining <= 0f) FinishSession();
            RefreshHud();
        }

        public void Configure(
            Transform content, TargetSpawner spawner, Text scoreLabel, Text timeLabel,
            Text instructionLabel, Text liveMetrics, Text modeLabel, Text difficultyLabel, Text adaptiveLabel,
            GameObject hub, GameObject setup, GameObject fire, GameObject cancel,
            GameObject panel, Text panelLabel)
        {
            arContent = content;
            targetSpawner = spawner;
            scoreText = scoreLabel;
            timeText = timeLabel;
            instructionText = instructionLabel;
            metricsText = liveMetrics;
            modeText = modeLabel;
            difficultyText = difficultyLabel;
            adaptiveText = adaptiveLabel;
            hubPanel = hub;
            setupPanel = setup;
            fireButton = fire;
            cancelButton = cancel;
            finalPanel = panel;
            finalText = panelLabel;
            CacheContentTransform();
        }

        public void EnterHub()
        {
            bool wasPlaying = Phase == GamePhase.Playing;
            Phase = GamePhase.Hub;
            targetSpawner?.EndSession();
            ReattachContent();
            if (arContent != null) arContent.gameObject.SetActive(false);
            ResetMetrics();
            ApplyPhaseUi();
            RefreshHud();
            if (wasPlaying) SessionFinished?.Invoke();
        }

        public void OpenSetup()
        {
            if (arContent != null) arContent.gameObject.SetActive(true);
            EnterSetup();
        }

        public void CycleMode()
        {
            if (Phase != GamePhase.Setup) return;
            trainingMode = trainingMode == TrainingMode.Plano ? TrainingMode.Grados360 : TrainingMode.Plano;
            RefreshOptionLabels();
            targetSpawner?.PreviewMode(trainingMode, difficulty);
        }

        public void CycleDifficulty()
        {
            if (Phase != GamePhase.Setup) return;
            difficulty = (DifficultyLevel)(((int)difficulty + 1) % 3);
            RefreshOptionLabels();
            targetSpawner?.PreviewMode(trainingMode, difficulty);
        }

        public void CycleAdaptive()
        {
            if (Phase != GamePhase.Setup) return;
            adaptiveMode = !adaptiveMode;
            targetSpawner?.SetAdaptiveEnabled(adaptiveMode);
            RefreshOptionLabels();
        }

        public void EnterSetup()
        {
            Phase = GamePhase.Setup;
            if (arContent != null) arContent.gameObject.SetActive(true);
            ReattachContent();
            ResetMetrics();
            targetSpawner?.PreviewMode(trainingMode, difficulty);
            targetSpawner?.SetAdaptiveEnabled(adaptiveMode);
            SessionStarted?.Invoke();
            ApplyPhaseUi();
            RefreshHud();
        }

        public void ConfirmPlacement()
        {
            if (Phase != GamePhase.Setup) return;
            if (arContent != null) arContent.SetParent(null, true);
            targetSpawner?.CommitMode(trainingMode, difficulty);
            StartSession();
        }

        public void ResetSession()
        {
            if (Phase == GamePhase.Setup) return;
            StartSession();
        }

        public bool RegisterShot()
        {
            if (Phase != GamePhase.Playing) return false;
            Shots++;
            return true;
        }

        public void RegisterHit(float reactionSeconds, float centerFactor)
        {
            if (Phase != GamePhase.Playing) return;
            Hits++;
            CurrentStreak++;
            BestStreak = Mathf.Max(BestStreak, CurrentStreak);
            ReactionTotal += Mathf.Max(0f, reactionSeconds);
            float reactionBonus = Mathf.Clamp01(1f - reactionSeconds / 5f);
            int basePoints = difficulty == DifficultyLevel.Facil ? 80 : difficulty == DifficultyLevel.Medio ? 100 : 130;
            Score += Mathf.RoundToInt(basePoints * (0.65f + centerFactor * 0.75f + reactionBonus * 0.35f));
            RecordAdaptiveResult(true);
            RefreshHud();
        }

        public void RegisterMiss()
        {
            if (Phase != GamePhase.Playing) return;
            CurrentStreak = 0;
            RecordAdaptiveResult(false);
            RefreshHud();
        }

        public void RegisterExpiredTarget()
        {
            if (Phase != GamePhase.Playing) return;
            // Una diana ya no se relocaliza por tiempo. Se conserva este
            // método para compatibilidad, pero no cuenta disparos fantasma.
            RefreshHud();
        }

        private void StartSession()
        {
            Phase = GamePhase.Playing;
            ResetMetrics();
            TimeRemaining = sessionDuration;
            targetSpawner?.BeginSession(trainingMode, difficulty);
            targetSpawner?.SetAdaptiveEnabled(adaptiveMode);
            SessionStarted?.Invoke();
            ApplyPhaseUi();
            RefreshHud();
        }

        private void FinishSession()
        {
            if (Phase == GamePhase.Finished) return;
            Phase = GamePhase.Finished;
            targetSpawner?.EndSession();
            repository.SaveIfBest(Score, Accuracy, AverageReaction, BestStreak);
            ShowFinalPanel();
            SessionFinished?.Invoke();
        }

        private void ResetMetrics()
        {
            Score = 0;
            Shots = 0;
            Hits = 0;
            CurrentStreak = 0;
            BestStreak = 0;
            ReactionTotal = 0f;
            TimeRemaining = sessionDuration;
            recentResultCount = 0;
            recentResultIndex = 0;
            Array.Clear(recentResults, 0, recentResults.Length);
            targetSpawner?.SetAdaptiveScale(1f);
        }

        private void CacheContentTransform()
        {
            if (arContent == null || arContent.parent == null) return;
            contentParent = arContent.parent;
            contentLocalPosition = arContent.localPosition;
            contentLocalRotation = arContent.localRotation;
            contentLocalScale = arContent.localScale;
        }

        private void ReattachContent()
        {
            if (arContent == null || contentParent == null) return;
            arContent.SetParent(contentParent, false);
            arContent.localPosition = contentLocalPosition;
            arContent.localRotation = contentLocalRotation;
            arContent.localScale = contentLocalScale;
        }

        private void ApplyPhaseUi()
        {
            if (hubPanel != null) hubPanel.SetActive(Phase == GamePhase.Hub);
            if (setupPanel != null) setupPanel.SetActive(Phase == GamePhase.Setup);
            if (fireButton != null) fireButton.SetActive(Phase == GamePhase.Playing);
            if (cancelButton != null) cancelButton.SetActive(Phase == GamePhase.Playing);
            if (finalPanel != null) finalPanel.SetActive(Phase == GamePhase.Finished);
            if (instructionText != null)
                instructionText.text = Phase == GamePhase.Hub
                    ? string.Empty
                    : Phase == GamePhase.Setup
                    ? "Elige modo y dificultad, encuadra el marcador y presiona COLOCAR"
                    : trainingMode == TrainingMode.Grados360
                        ? "Gira para encontrar las dianas; las flechas indican las que están fuera de cámara"
                        : "Apunta con la retícula y presiona FUEGO";
            RefreshOptionLabels();
        }

        private void RefreshOptionLabels()
        {
            if (modeText != null) modeText.text = trainingMode == TrainingMode.Plano ? "MODO: PLANO" : "MODO: 360°";
            if (difficultyText != null) difficultyText.text = $"DIFICULTAD: {DifficultyName()}";
            if (adaptiveText != null) adaptiveText.text = adaptiveMode ? "ADAPTATIVO: ACTIVADO" : "ADAPTATIVO: DESACTIVADO";
        }

        private string DifficultyName() => difficulty == DifficultyLevel.Facil ? "FÁCIL" : difficulty == DifficultyLevel.Medio ? "MEDIA" : "DIFÍCIL";

        private void ShowFinalPanel()
        {
            RefreshHud();
            if (finalText == null) return;
            finalText.text =
                $"SESIÓN TERMINADA — {(trainingMode == TrainingMode.Plano ? "PLANO" : "360°")} / {DifficultyName()}\n\n" +
                $"Puntaje: {Score}\nImpactos: {Hits}/{Shots}\nPrecisión: {Accuracy:0.#}%\n" +
                $"Reacción promedio: {AverageReaction:0.00} s\nMejor racha: {BestStreak}\n\n" +
                $"RÉCORD HISTÓRICO\nPuntaje: {repository.BestScore}  |  Precisión: {repository.BestAccuracy:0.#}%\n" +
                $"Mejor reacción: {repository.BestReaction:0.00} s  |  Racha: {repository.BestStreak}";
            ApplyPhaseUi();
        }

        private void RefreshHud()
        {
            if (scoreText != null) scoreText.text = $"Puntaje: {Score}";
            if (timeText != null) timeText.text = $"Tiempo: {Mathf.CeilToInt(TimeRemaining)}";
            if (metricsText != null)
                metricsText.text = $"Impactos {Hits}/{Shots}  •  Precisión {Accuracy:0.#}%  •  Racha {CurrentStreak}" +
                    (adaptiveMode && targetSpawner != null ? $"  •  Escala {targetSpawner.AdaptiveScale * 100f:0}%" : string.Empty);
        }

        private void RecordAdaptiveResult(bool hit)
        {
            if (!adaptiveMode || targetSpawner == null) return;

            recentResults[recentResultIndex] = hit;
            recentResultIndex = (recentResultIndex + 1) % recentResults.Length;
            recentResultCount = Mathf.Min(recentResultCount + 1, recentResults.Length);
            if (recentResultCount < 4)
            {
                targetSpawner.SetAdaptiveScale(1f);
                return;
            }

            int recentHits = 0;
            for (int i = 0; i < recentResultCount; i++)
                if (recentResults[i]) recentHits++;

            float ratio = (float)recentHits / recentResultCount;
            float scale = ratio >= 0.60f
                ? Mathf.Lerp(1f, 0.62f, Mathf.InverseLerp(0.60f, 1f, ratio))
                : ratio <= 0.45f
                    ? Mathf.Lerp(1.28f, 1f, Mathf.InverseLerp(0f, 0.45f, ratio))
                    : 1f;
            targetSpawner.SetAdaptiveScale(scale);
        }
    }
}
