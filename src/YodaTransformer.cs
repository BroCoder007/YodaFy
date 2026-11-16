using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace YodaFy
{
    // Offline, deterministic English grammar heuristics; never sends text over a network.
    public static class YodaTransformer
    {
        private static readonly HashSet<string> Aux = new HashSet<string>("am is are was were will would can could shall should may might must do does did have has had".Split(' '));
        private static readonly HashSet<string> Verbs = new HashSet<string>("like likes love loves need needs want wants know knows think thinks feel feels see sees hear hears enjoy enjoys understand understands prefer prefers remember remembers miss misses trust trusts".Split(' '));
        private static readonly Regex Lowerable = new Regex(@"^(I|You|We|They|He|She|It|This|That|These|Those|The|A|An|My|Your|Our|Their|His|Her|Its)$");
        private static readonly Dictionary<string, string> Contractions = new Dictionary<string, string> {
            {"i'm","I am"},{"you're","You are"},{"we're","We are"},{"they're","They are"},{"he's","He is"},{"she's","She is"},{"it's","It is"},
            {"i'll","I will"},{"you'll","You will"},{"we'll","We will"},{"they'll","They will"},{"i've","I have"},{"we've","We have"},
            {"can't","can not"},{"cannot","can not"},{"won't","will not"},{"don't","do not"},{"doesn't","does not"},{"didn't","did not"},
            {"isn't","is not"},{"aren't","are not"},{"wasn't","was not"},{"weren't","were not"},{"shouldn't","should not"},{"wouldn't","would not"},{"couldn't","could not"}
        };
        private static readonly Regex Protected = new Regex(@"\[[^\]\r\n]*\]\([^\s\r\n]*\)|https?://[^\s<>]+|www\.[^\s<>]+|[\w.+-]+@[\w.-]+\.[a-z]{2,}|<[^>\r\n]+>", RegexOptions.IgnoreCase);
        private static readonly Regex Contract = new Regex(@"\b(?:I'm|you're|we're|they're|he's|she's|it's|I'll|you'll|we'll|they'll|I've|we've|can't|cannot|won't|don't|doesn't|didn't|isn't|aren't|wasn't|weren't|shouldn't|wouldn't|couldn't)\b", RegexOptions.IgnoreCase);
        private static string Capitalize(string value) { return Regex.Replace(value, @"^[a-z]", m => m.Value.ToUpperInvariant()); }
        private static string Sentence(string value)
        {
            Match match = Regex.Match(value, @"^(\s*)([\s\S]*?)(\s*)$");
            string raw = match.Groups[2].Value;
            if (raw.Length == 0 || Regex.IsMatch(raw, @"[,;:?]") || Regex.IsMatch(raw, @"^(?:[/#>!]|[-*]\s)")) return value;
            Match ending = Regex.Match(raw, @"([.!]+)$");
            string punctuation = ending.Success ? ending.Value : ".";
            string core = ending.Success ? raw.Substring(0, raw.Length - ending.Length) : raw;
            core = Contract.Replace(core, m => Contractions[m.Value.ToLowerInvariant()]);
            string[] words = Regex.Split(core, @"[ \t]+");
            if (words.Length < 3) return value;
            int pivot = -1;
            bool auxiliary = false;
            for (int i = 1; i < Math.Min(words.Length - 1, 7); i++)
            {
                string word = words[i].ToLowerInvariant();
                if (Aux.Contains(word) || Verbs.Contains(word)) { pivot = i; auxiliary = Aux.Contains(word); break; }
            }
            if (pivot < 0) return value;
            int end = pivot;
            if (auxiliary && Regex.IsMatch(words[end + 1], @"^(not|never)$", RegexOptions.IgnoreCase)) end++;
            if (end >= words.Length - 1 || Regex.IsMatch(words[end + 1], @"^(and|but|or|because|if|when|that|not)$", RegexOptions.IgnoreCase)) return value;
            string[] subject = new string[pivot];
            Array.Copy(words, subject, pivot);
            if (subject[0] != "I" && Lowerable.IsMatch(subject[0])) subject[0] = subject[0].ToLowerInvariant();
            string rest = String.Join(" ", words, end + 1, words.Length - end - 1);
            return match.Groups[1].Value + Capitalize(rest) + ", " + String.Join(" ", subject) + " " + String.Join(" ", words, pivot, end - pivot + 1) + punctuation + match.Groups[3].Value;
        }
        public static string Transform(string text)
        {
            if (text == null) throw new ArgumentNullException("text");
            if (text.Contains("\uE000")) return text;
            bool inFence = false;
            string[] lines = Regex.Split(text, @"(\r\n|\r|\n)");
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (Regex.IsMatch(line, @"^\s*(?:```|~~~)")) { inFence = !inFence; continue; }
                if (inFence || line.Contains("`") || Regex.IsMatch(line, @"^[\r\n]+$")) continue;
                List<string> spans = new List<string>();
                string masked = Protected.Replace(line, m => {
                    string token = m.Value, suffix = "";
                    if (Regex.IsMatch(token, @"^(?:https?://|www\.)", RegexOptions.IgnoreCase))
                    {
                        Match ending = Regex.Match(token, @"[.!?,;:]+$");
                        if (ending.Success) { suffix = ending.Value; token = token.Substring(0, token.Length - suffix.Length); }
                    }
                    int id = spans.Count; spans.Add(token);
                    return "\uE000" + id.ToString() + "\uE001" + suffix;
                });
                string[] sentences = Regex.Split(masked, @"(?<=[.!?])([ \t]+)(?=\S)");
                for (int j = 0; j < sentences.Length; j++) sentences[j] = Sentence(sentences[j]);
                lines[i] = Regex.Replace(String.Join("", sentences), "\uE000(\\d+)\uE001", m => spans[Int32.Parse(m.Groups[1].Value)]);
            }
            return String.Join("", lines);
        }
    }
}
