using System.Collections.Generic;
using UnityEngine;

namespace ClickerGenesis.Economy
{
    /// <summary>
    /// A book's full scribe/manager roster. One asset per book (Genesis is the pilot template —
    /// see project docs). Other books get their own asset authored when that book is built.
    /// </summary>
    [CreateAssetMenu(fileName = "ScribeSetConfig", menuName = "Clicker Genesis/Economy/Scribe Set Config")]
    public class ScribeSetConfig : ScriptableObject
    {
        public string bookName;
        public List<ScribeDefinition> tiers = new List<ScribeDefinition>();
    }
}
