using UnityEngine;

namespace AIMAR
{
    public sealed class ScoreRepository
    {
        private const string ScoreKey = "AIMAR.BestScore";
        private const string AccuracyKey = "AIMAR.BestAccuracy";
        private const string ReactionKey = "AIMAR.BestReaction";
        private const string StreakKey = "AIMAR.BestStreak";

        public int BestScore => PlayerPrefs.GetInt(ScoreKey, 0);
        public float BestAccuracy => PlayerPrefs.GetFloat(AccuracyKey, 0f);
        public float BestReaction => PlayerPrefs.GetFloat(ReactionKey, 0f);
        public int BestStreak => PlayerPrefs.GetInt(StreakKey, 0);

        public void SaveIfBest(int score, float accuracy, float reaction, int streak)
        {
            if (score > BestScore) PlayerPrefs.SetInt(ScoreKey, score);
            if (accuracy > BestAccuracy) PlayerPrefs.SetFloat(AccuracyKey, accuracy);
            if (reaction > 0f && (BestReaction <= 0f || reaction < BestReaction)) PlayerPrefs.SetFloat(ReactionKey, reaction);
            if (streak > BestStreak) PlayerPrefs.SetInt(StreakKey, streak);
            PlayerPrefs.Save();
        }
    }
}
