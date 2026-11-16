using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace YodaFy
{
    public static class AutomationPolicy
    {
        static readonly string[] Browsers = { "chrome", "msedge", "firefox", "brave", "vivaldi", "opera" };
        static readonly Regex Compose = new Regex(@"\b(message|compose|reply|chat|write|send|typing)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        static readonly Regex Excluded = new Regex(@"\b(search|find|address|password|username|email|phone|rename|title|subject)\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        public static bool AllowedProcess(Settings settings, string processName, string title)
        {
            if (String.IsNullOrWhiteSpace(processName)) return false;
            if (settings.Processes.Any(delegate(string item) { return String.Equals(item, processName, StringComparison.OrdinalIgnoreCase); })) return true;
            return settings.BrowserEnabled && Browsers.Contains(processName, StringComparer.OrdinalIgnoreCase)
                && settings.BrowserTitles.Any(delegate(string item) { return !String.IsNullOrWhiteSpace(item) && (title ?? "").IndexOf(item, StringComparison.OrdinalIgnoreCase) >= 0; });
        }

        public static bool IsComposeField(string name, string helpText)
        {
            string description = (name ?? "") + " " + (helpText ?? "");
            return !Excluded.IsMatch(description) && Compose.IsMatch(description);
        }

        public static bool WritableTextAttribute(object attribute) { return attribute is bool && !(bool)attribute; }

        public static bool EligibleDraft(string text)
        {
            return !String.IsNullOrWhiteSpace(text) && text.Length <= 8000 && text.Trim().Length >= 4;
        }
    }
}
