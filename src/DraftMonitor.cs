using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Automation;
using System.Windows.Automation.Text;
using System.Windows.Forms;

namespace YodaFy
{
    public sealed class DraftMonitor : IDisposable
    {
        readonly object gate = new object();
        readonly ManualResetEvent stopping = new ManualResetEvent(false);
        readonly Thread worker;
        Settings settings;
        string lastStatus;
        string lastFocus;
        string lastText;
        string convertedText;
        DateTime stableSince = DateTime.UtcNow;
        public event Action<string> StatusChanged;

        public DraftMonitor(Settings initial)
        {
            settings = initial.Copy();
            worker = new Thread(Run) { IsBackground = true, Name = "YodaFy draft monitor" };
            worker.SetApartmentState(ApartmentState.STA);
            worker.Start();
        }

        public void Update(Settings value) { lock (gate) settings = value.Copy(); }

        void Status(string text)
        {
            if (lastStatus == text) return;
            lastStatus = text;
            Action<string> handler = StatusChanged;
            if (handler != null) handler(text);
        }

        void Reset() { lastFocus = null; lastText = null; convertedText = null; }

        void Run()
        {
            while (!stopping.WaitOne(250))
            {
                try { Check(); }
                catch (ElementNotAvailableException) { Reset(); Status("The focused field changed. Waiting for a message draft."); }
                catch (Exception) { Reset(); Status("This field is unavailable. Use the clipboard shortcut instead."); }
            }
        }

        void Check()
        {
            Settings current;
            lock (gate) current = settings.Copy();
            if (!current.AutomationEnabled) { Reset(); Status("Paused. Your messages stay exactly as you type them."); return; }
            IntPtr window = Native.GetForegroundWindow();
            uint pid;
            Native.GetWindowThreadProcessId(window, out pid);
            if (pid == 0 || pid == (uint)Process.GetCurrentProcess().Id) { Reset(); Status("Ready. Open a selected messaging app to begin."); return; }
            string process;
            using (Process target = Process.GetProcessById((int)pid)) process = target.ProcessName;
            if (!AutomationPolicy.AllowedProcess(current, process, Native.WindowTitle(window)))
            { Reset(); Status("Ready. The active app is outside your selected apps."); return; }
            if (Native.IsComposing(window))
            { Status(process + ": finish composing your input before conversion."); return; }

            AutomationElement focused = AutomationElement.FocusedElement;
            if (focused == null || !focused.Current.HasKeyboardFocus || !focused.Current.IsEnabled || focused.Current.IsPassword)
            { Reset(); Status("Protected or unavailable field. Nothing is read or changed."); return; }
            // Inspect the OS-focused control only; never search descendants or select a page.
            ControlType kind = focused.Current.ControlType;
            if ((kind != ControlType.Edit && kind != ControlType.Document)
                || !AutomationPolicy.IsComposeField(focused.Current.Name, focused.Current.HelpText))
            { Reset(); Status(process + ": focus a labeled message composer. Clipboard mode is always available."); return; }

            ValuePattern value;
            TextPattern text;
            string draft;
            if (!ReadDraft(focused, out value, out text, out draft))
            { Reset(); Status(process + ": this composer does not expose editable text. Use clipboard mode."); return; }
            string identity = String.Join(".", focused.GetRuntimeId());
            if (lastFocus != identity) { Reset(); lastFocus = identity; }
            if (draft == convertedText) { lastText = draft; Status(process + ": draft converted. Review it, then send normally."); return; }
            if (draft != lastText) { lastText = draft; stableSince = DateTime.UtcNow; }
            if (!AutomationPolicy.EligibleDraft(draft)) { Status(process + ": ready for your next message."); return; }
            Status(process + ": watching your draft. Pause typing to convert it.");
            if ((DateTime.UtcNow - stableSince).TotalMilliseconds < current.IdleMilliseconds
                || Native.IdleTime() < current.IdleMilliseconds || Native.ModifierDown()) return;

            string transformed = YodaTransformer.Transform(draft);
            if (transformed == draft) { convertedText = draft; return; }
            uint inputTick = Native.InputTick();
            if (!SameFocus(focused, window)) return;
            ValuePattern checkValue;
            TextPattern checkText;
            string checkDraft;
            if (!ReadDraft(focused, out checkValue, out checkText, out checkDraft) || checkDraft != draft
                || Native.InputTick() != inputTick || Native.IsComposing(window) || !SameFocus(focused, window)) return;

            if (checkValue != null)
            {
                checkValue.SetValue(transformed);
            }
            else
            {
                if (!PasteRange(focused, checkText, window, inputTick, transformed)) return;
            }
            convertedText = transformed;
            lastText = transformed;
            Status(process + ": draft converted. Review it, then send normally.");
        }

        static bool ReadDraft(AutomationElement element, out ValuePattern value, out TextPattern text, out string draft)
        {
            value = null; text = null; draft = null;
            if (!element.Current.IsEnabled || element.Current.IsPassword || !element.Current.HasKeyboardFocus) return false;
            // A page document is never a composer. Editable documents additionally need a
            // textbox ARIA role or a native rich-edit class, plus the label check in Check().
            if (element.Current.ControlType == ControlType.Document)
            {
                // .NET Framework's managed UIA wrapper has no AriaRoleProperty member.
                // Lookup succeeds only when the provider registered that modern property.
                AutomationProperty ariaRole = AutomationProperty.LookupById(30101);
                string role = ariaRole == null ? null : element.GetCurrentPropertyValue(ariaRole, true) as string;
                string className = element.Current.ClassName ?? "";
                if (!String.Equals(role, "textbox", StringComparison.OrdinalIgnoreCase)
                    && className.IndexOf("richedit", StringComparison.OrdinalIgnoreCase) < 0) return false;
            }
            object pattern;
            if (element.TryGetCurrentPattern(ValuePattern.Pattern, out pattern))
            {
                ValuePattern candidate = (ValuePattern)pattern;
                if (!candidate.Current.IsReadOnly)
                {
                    value = candidate; draft = candidate.Current.Value;
                    return draft != null && draft.Length <= 8000;
                }
            }
            if (!element.TryGetCurrentPattern(TextPattern.Pattern, out pattern)) return false;
            TextPattern candidateText = (TextPattern)pattern;
            TextPatternRange range = candidateText.DocumentRange;
            if (!AutomationPolicy.WritableTextAttribute(range.GetAttributeValue(TextPattern.IsReadOnlyAttribute))) return false;
            draft = range.GetText(8001);
            if (draft == null || draft.Length > 8000) return false;
            text = candidateText;
            return true;
        }

        static bool SameFocus(AutomationElement element, IntPtr window)
        {
            return Native.GetForegroundWindow() == window && element.Current.HasKeyboardFocus
                && Automation.Compare(element, AutomationElement.FocusedElement);
        }

        static bool PasteRange(AutomationElement element, TextPattern pattern, IntPtr window, uint inputTick, string replacement)
        {
            // Materialize every clipboard format before replacing it; if a delayed/private
            // format cannot be copied, abandon the replacement instead of losing that data.
            uint previousSequence = Native.GetClipboardSequenceNumber();
            IDataObject previous = Clipboard.GetDataObject();
            DataObject snapshot = new DataObject();
            bool empty = previous == null;
            if (previous != null)
            {
                string[] formats = previous.GetFormats(false);
                empty = formats.Length == 0;
                foreach (string format in formats)
                {
                    object content = previous.GetData(format, false);
                    if (content == null) return false;
                    // Delayed clipboard providers may return live streams or images. Make
                    // independent copies before taking ownership; skip opaque formats.
                    if (content is MemoryStream) content = new MemoryStream(((MemoryStream)content).ToArray());
                    else if (content is Image) content = ((Image)content).Clone();
                    else if (content is byte[]) content = ((byte[])content).Clone();
                    else if (content is string[]) content = ((string[])content).Clone();
                    else if (!(content is string) && !(content is int) && !(content is bool)) return false;
                    snapshot.SetData(format, false, content);
                }
            }
            if (Native.GetClipboardSequenceNumber() != previousSequence || Native.InputTick() != inputTick || !SameFocus(element, window)) return false;
            const string ownerFormat = "YodaFy.ClipboardOwner";
            string ownerToken = Guid.NewGuid().ToString("N");
            DataObject payload = new DataObject();
            payload.SetText(replacement, TextDataFormat.UnicodeText);
            payload.SetData(ownerFormat, false, ownerToken);
            Clipboard.SetDataObject(payload, true);
            uint owned = Native.GetClipboardSequenceNumber();
            bool pasted = false;
            try
            {
                if (Native.InputTick() != inputTick || !SameFocus(element, window)) return false;
                pattern.DocumentRange.Select();
                if (Native.InputTick() != inputTick || !SameFocus(element, window)) return false;
                SendKeys.SendWait("^v");
                pasted = true;
                // Let the target read clipboard data before restoring it. This wait occurs
                // only on the background monitor; the app's UI remains responsive.
                Thread.Sleep(180);
                return true;
            }
            finally
            {
                if (Native.GetClipboardSequenceNumber() == owned && Object.Equals(Clipboard.GetData(ownerFormat), ownerToken))
                {
                    if (empty) Clipboard.Clear();
                    else Clipboard.SetDataObject(snapshot, true);
                }
                if (!pasted && SameFocus(element, window))
                {
                    TextPatternRange range = pattern.DocumentRange;
                    range.MoveEndpointByRange(TextPatternRangeEndpoint.Start, range, TextPatternRangeEndpoint.End);
                    range.Select();
                }
            }
        }

        public void Dispose() { stopping.Set(); }
    }
}
