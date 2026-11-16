# YodaFy

A Windows desktop app that turns ordinary English into Yoda-style messages, locally and without an account or API key.

## Run

Windows 10/11 with .NET Framework 4.8 is required. Open PowerShell in this repository:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\start.ps1
```

This builds `dist/YodaFy.exe` using Windows' .NET Framework C# compiler, then opens it. You can subsequently launch the executable directly. No Visual Studio, .NET SDK, Node, Python or paid translation service is required.

## Automatic conversion

1. Open YodaFy and select the messaging apps you want to use.
2. Click **Enable automatic conversion**. Automatic mode starts disabled on first launch.
3. Open a selected app and type in its message composer. Pause typing for 1.8 seconds (adjustable) to convert the draft.
4. Review the changed draft and send it normally. YodaFy never presses Send.

Discord, WhatsApp, Telegram, Slack, Teams and Signal process names are provided. **Add app** lets you allow another messaging app by its process name from Task Manager, without `.exe`. For supported browser versions, enable **Also allow messaging tabs in browsers**. The browser window title must match one of the semicolon-separated title phrases. Browsers supported by the process allow-list are Chrome, Edge, Firefox, Brave, Vivaldi and Opera.

Closing the window keeps YodaFy in the system tray. The tray menu can pause, reopen or quit it. Selected apps, browser titles, typing pause and enabled/paused state are saved under `%LOCALAPPDATA%\YodaFy\settings.xml`. Remove this file to reset preferences.

## Composer compatibility

This uses Windows UI Automation, not a browser extension or messaging service integration. Automatic conversion requires the app to expose a **focused editable composer** whose accessible name/help text identifies it as a message, compose, reply, chat, write or send field. Search, address, password, rename, title and account fields are excluded. Unlabeled controls are skipped. A matching app name does not guarantee that its current version exposes its composer in a compatible way.

The app reads only that focused field, validates that it is writable, waits for both a stable draft and idle user input, and checks focus and content again before replacing. Native `ValuePattern` is used where available. Rich text uses its exact editable `TextPattern` range, temporary clipboard contents and paste. Read-only text, passwords and unknown controls are skipped. Clipboard contents are restored only if the clipboard has not changed since YodaFy wrote to it; if a clipboard format cannot be copied safely, that automatic replacement is skipped. Rich formatting, cursor position and the app's native undo history may be affected. Drafts over 8,000 characters are skipped.

Some app versions require accessibility support to be enabled; others do not expose a usable editable range at all. The status message tells you when to use clipboard mode. Browser tab title matching is an explicit allow-list; keep title phrases specific to your messaging tabs.

Fast messages sent before the pause are not converted. This app does not intercept Enter or delay sending. UI Automation cannot guarantee a race-free edit if a target app ignores focus updates or the user starts typing at the instant a replacement happens. Use the preview/clipboard workflow for sensitive drafts. This release supports outgoing drafts; it cannot rewrite incoming conversations inside other apps. It supports Windows only.

The automatic watcher skips active composition exposed through the Windows IME compatibility API. Some modern input methods do not expose their composition there; pause automatic mode while using those input methods. Automatic composer detection currently recognizes English accessibility labels.

## Works in other apps through the clipboard

Copy text, press **Ctrl + Alt + Y**, then paste. This shortcut intentionally changes clipboard text only; it never sends or pastes automatically. The main window and tray also have a **Convert clipboard** button. If the shortcut is already registered by another program, those buttons remain available.

The preview panel also converts text immediately and offers **Copy Yoda text**. Conversion is a deterministic, rule-based English approximation. Complex sentences may sound awkward; review them before sending. Links, code and names are handled by the transformer but it is not a full language model.

## Development

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\build.ps1 -Test
```

`src/YodaTransformer.cs` contains the offline conversion engine. `DraftMonitor.cs` implements the foreground-only watcher and guarded replacement. `AutomationPolicy.cs` holds the process, composer and writability rules. `MainForm.cs` contains the native desktop interface. No message history is written to disk or sent over a network.

This repository is independent of **YodaFy-Discord**. Neither project needs files from the other to run. The earlier Android prototype is separate historical material and is not used by this desktop app.

API references: [Microsoft UI Automation text model](https://learn.microsoft.com/en-us/dotnet/framework/ui-automation/ui-automation-textpattern-overview), [ValuePattern.SetValue](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.valuepattern.setvalue?view=netframework-4.8.1), [writability attributes](https://learn.microsoft.com/en-us/dotnet/api/system.windows.automation.textpattern.isreadonlyattribute?view=netframework-4.8.1), [clipboard sequence numbers](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getclipboardsequencenumber).
