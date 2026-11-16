using System;

namespace YodaFy
{
    public static class Tests
    {
        static int count;
        static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            count++;
        }

        public static int Main()
        {
            try
            {
                Settings settings = new Settings();
                Assert(!settings.AutomationEnabled, "Automation must start opt-in.");
                Assert(AutomationPolicy.AllowedProcess(settings, "Discord", "general"), "Selected desktop process should be allowed.");
                Assert(!AutomationPolicy.AllowedProcess(settings, "chrome", "WhatsApp"), "Browser automation must be opt-in.");
                settings.BrowserEnabled = true;
                Assert(AutomationPolicy.AllowedProcess(settings, "msedge", "WhatsApp - Microsoft Edge"), "Opted-in messaging browser title should work.");
                Assert(!AutomationPolicy.AllowedProcess(settings, "chrome", "Online banking"), "Unmatched browser tab must be excluded.");
                settings.BrowserTitles = new string[] { "", "   " };
                Assert(!AutomationPolicy.AllowedProcess(settings, "chrome", "Anything"), "Empty title matcher must never enable every tab.");
                Assert(AutomationPolicy.IsComposeField("Message #general", ""), "Discord composer should be eligible.");
                Assert(AutomationPolicy.IsComposeField("", "Write a message"), "Composer help text should be eligible.");
                Assert(!AutomationPolicy.IsComposeField("Search messages", ""), "Message search must be excluded.");
                Assert(!AutomationPolicy.IsComposeField("", ""), "Unknown edit controls must be excluded.");
                Assert(!AutomationPolicy.IsComposeField("Rename chat", ""), "Rename controls must be excluded.");
                Assert(AutomationPolicy.WritableTextAttribute(false), "Explicit writable attribute should work.");
                Assert(!AutomationPolicy.WritableTextAttribute(true), "Read-only text must be excluded.");
                Assert(!AutomationPolicy.WritableTextAttribute(null), "Missing writability must be excluded.");
                Assert(!AutomationPolicy.WritableTextAttribute(new object()), "Mixed/unsupported writability must be excluded.");
                Assert(!AutomationPolicy.EligibleDraft(new string('a', 8001)), "Oversized draft must be excluded.");
                Assert(!AutomationPolicy.EligibleDraft("  "), "Empty draft must be excluded.");
                Assert(AutomationPolicy.EligibleDraft("I am ready."), "Ordinary draft should work.");
                Settings copy = settings.Copy(); copy.Processes[0] = "something-else";
                Assert(settings.Processes[0] == "Discord", "Settings copy must not share allow-list arrays.");
                Assert(YodaTransformer.Transform("I am ready.") != "I am ready.", "Transformer should change an ordinary sentence.");
                Console.WriteLine("Passed " + count + " desktop policy tests and " + TransformerTests.Run() + " transformer tests.");
                return 0;
            }
            catch (Exception error) { Console.Error.WriteLine(error.Message); return 1; }
        }
    }
}
