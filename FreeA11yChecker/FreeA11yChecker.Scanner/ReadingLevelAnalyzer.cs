using System.Text.Json;
using Microsoft.Playwright;

namespace FreeA11yChecker.Scanner;

/// <summary>
/// Extracts visible text from the page and computes Flesch-Kincaid readability scores.
/// WCAG 3.1.5 (AAA) — Reading Level: content should be at lower secondary education level
/// or provide a simplified alternative.
/// </summary>
public static class ReadingLevelAnalyzer
{
    /// <summary>
    /// Extracts body text from the page and computes readability metrics.
    /// Returns JSON with Flesch-Kincaid Grade Level, Flesch Reading Ease, word/sentence/syllable counts.
    /// </summary>
    public static async Task<string> Run(IPage page)
    {
        try {
            // Extract visible text from the page, excluding scripts, styles, and navigation.
            string text = await page.EvaluateAsync<string>(@"() => {
                // Clone body to avoid mutation.
                const clone = document.body.cloneNode(true);
                // Remove non-content elements.
                clone.querySelectorAll('script, style, noscript, svg, nav, header, footer, [role=""navigation""], [role=""banner""], [role=""contentinfo""]').forEach(el => el.remove());
                // Get text content.
                return (clone.textContent || '').replace(/\s+/g, ' ').trim();
            }") ?? "";

            if (string.IsNullOrWhiteSpace(text) || text.Length < 50) {
                return JsonSerializer.Serialize(new {
                    fleschKincaid = 0.0,
                    fleschEase = 0.0,
                    wordCount = 0,
                    sentenceCount = 0,
                    syllableCount = 0,
                    gradeLevel = "N/A — insufficient text"
                });
            }

            // Compute metrics.
            int wordCount = CountWords(text);
            int sentenceCount = CountSentences(text);
            int syllableCount = CountSyllables(text);

            if (wordCount == 0 || sentenceCount == 0) {
                return JsonSerializer.Serialize(new {
                    fleschKincaid = 0.0,
                    fleschEase = 0.0,
                    wordCount,
                    sentenceCount,
                    syllableCount,
                    gradeLevel = "N/A — insufficient text structure"
                });
            }

            // Flesch-Kincaid Grade Level = 0.39 × (words/sentences) + 11.8 × (syllables/words) − 15.59
            double avgWordsPerSentence = (double)wordCount / sentenceCount;
            double avgSyllablesPerWord = (double)syllableCount / wordCount;
            double fkGrade = 0.39 * avgWordsPerSentence + 11.8 * avgSyllablesPerWord - 15.59;
            fkGrade = Math.Round(Math.Max(0, fkGrade), 1);

            // Flesch Reading Ease = 206.835 − 1.015 × (words/sentences) − 84.6 × (syllables/words)
            double fkEase = 206.835 - 1.015 * avgWordsPerSentence - 84.6 * avgSyllablesPerWord;
            fkEase = Math.Round(Math.Clamp(fkEase, 0, 100), 1);

            string gradeLevel = fkGrade switch {
                <= 6 => "6th grade or below (easy)",
                <= 8 => "7th–8th grade (fairly easy)",
                <= 10 => "9th–10th grade (standard)",
                <= 12 => "11th–12th grade (fairly difficult)",
                <= 14 => "College level (difficult)",
                _ => "Graduate level (very difficult)"
            };

            return JsonSerializer.Serialize(new {
                fleschKincaid = fkGrade,
                fleschEase = fkEase,
                wordCount,
                sentenceCount,
                syllableCount,
                gradeLevel
            });
        } catch {
            return "{}";
        }
    }

    /// <summary>Counts words by splitting on whitespace.</summary>
    private static int CountWords(string text)
    {
        return text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    /// <summary>Counts sentences by looking for sentence-ending punctuation.</summary>
    private static int CountSentences(string text)
    {
        int count = 0;
        for (int i = 0; i < text.Length; i++) {
            char c = text[i];
            if (c == '.' || c == '!' || c == '?') {
                count++;
                // Skip consecutive punctuation (e.g., "..." or "?!")
                while (i + 1 < text.Length && (text[i + 1] == '.' || text[i + 1] == '!' || text[i + 1] == '?'))
                    i++;
            }
        }
        return Math.Max(1, count);
    }

    /// <summary>
    /// Estimates syllable count using a simple English heuristic:
    /// count vowel groups, subtract silent e, minimum 1 per word.
    /// </summary>
    private static int CountSyllables(string text)
    {
        int total = 0;
        string[] words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (string rawWord in words) {
            string word = rawWord.Trim('.', ',', '!', '?', ';', ':', '"', '\'', '(', ')').ToLowerInvariant();
            if (word.Length == 0) continue;

            int syllables = 0;
            bool previousVowel = false;

            for (int i = 0; i < word.Length; i++) {
                bool isVowel = "aeiouy".Contains(word[i]);
                if (isVowel && !previousVowel) syllables++;
                previousVowel = isVowel;
            }

            // Subtract silent 'e' at end.
            if (word.EndsWith('e') && syllables > 1) syllables--;

            // Minimum 1 syllable per word.
            total += Math.Max(1, syllables);
        }

        return total;
    }
}
