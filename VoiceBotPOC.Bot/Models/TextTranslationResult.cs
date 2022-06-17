using System.Collections;
using System.Collections.Generic;

namespace VoiceBotPOC.Bot.Models
{
    public class TextTranslationResult
    {
        public DetectedLanguage detectedLanguage { get; set; }
        public IEnumerable<Translation> Translations { get; set; }
    }
}