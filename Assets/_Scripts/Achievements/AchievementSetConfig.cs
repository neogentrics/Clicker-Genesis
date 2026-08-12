using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Achievements
{
    /// <summary>A list of achievements, same role ScribeSetConfig plays for a book's scribe
    /// roster. GameLoopController can hold several of these (e.g. one hand-authored "headline"
    /// set plus one generated per-book set) and merges them into a single AchievementSystem -
    /// keeps the generated family regenerable without touching hand-authored content.</summary>
    [CreateAssetMenu(fileName = "AchievementSetConfig", menuName = "Clicker Genesis/Achievements/Achievement Set Config")]
    public class AchievementSetConfig : ScriptableObject
    {
        public string setName;
        public List<AchievementDefinition> achievements = new List<AchievementDefinition>();
    }
}
