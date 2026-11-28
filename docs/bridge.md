# The JARVIS bridge

The bridge logic lives in the class library `src/Jarvis.VoiceBridge.Core`; `src/Jarvis.VoiceBridge` is the console app on top of it,
and the desktop tray app (`src/HomeApps.Desktop`, see [desktop.md](desktop.md)) hosts the same Core in-process. The console app runs
on the Mac (it also runs on Linux): it only listens for "Hey Jarvis"
locally and carries sound, everything else happens on the HomeApps server (`/jarvis/voice`, `/devices/link`; see
HomeApps `docs/jarvis.md` and `docs/devices.md` for the server side).

```bash
dotnet run --project src/Jarvis.VoiceBridge -- --server https://home-apps.dev --device mac-biuro
# dev: --server https://app.localhost --insecure
```

- **First run:** downloads the three openWakeWord models (v0.5.1, checked by hash) into `~/.homeapps/voice-bridge`,
  macOS asks for microphone access for the terminal, and the browser opens HomeApps' consent screen ("Zezwól"). The
  bridge then keeps its refresh token in the Keychain and appears under "Połączone aplikacje" as "JARVIS: most głosowy".
  `--logout` forgets it on this Mac.
- **Using it:** "Hey Jarvis" (a rising chime) → say the request → after ~0.7 s of quiet a falling chime, the transcript
  and JARVIS's answer are printed and spoken (`afplay`). When he asks to confirm, a chime and 8 s for "tak" / "nie"
  without the wake word (console only: the tray app answers on screen, see [desktop.md](desktop.md)). Enter starts without the wake word, ends the utterance early, or stops JARVIS mid-answer;
  `q` quits. While he speaks the microphone is ignored, except for "stop" / "cisza" with local speech (see below); Enter always interrupts him.
- **Tuning:** `--threshold` (0.5) is the wake word's score. `--save-recordings <folder>` keeps each turn as a WAV on
  this Mac only (V7); `--check-wake <file.wav>` prints the best score in a 16 kHz recording, e.g. one of yours, to pick a
  threshold. Synthetic English "hey jarvis" scores 0.96-0.99, unrelated speech 0.00; a Polish accent may score lower.
- **Speech on this Mac (S1, D10):** speech is local by default (spec 2.1: the audio never leaves this computer) and only
  text goes to the server. `--speech server` opts out (the server hears and speaks). Without `--engine` Whisper
  (Whisper.net, GGML) hears and Piper (ONNX Runtime) speaks inside the bridge, so the server gets `?speech=local`
  (text turns) and no Speaches container is needed. `--engine http://host:8000` selects an external OpenAI-audio
  engine (Speaches) instead. See "Speech without Speaches" below.
- **Follow-up window (D11):** after EVERY answer the microphone stays open for 8 s without the wake word; 8 s of
  silence closes it. While JARVIS speaks only "stop" or "cisza" gets through, checked by the echo gate against the
  playback; it needs local speech (the default), because the bridge must hear the word itself. The gate is described
  below, under "Speech without Speaches".
- **One computer at a time (Q2 A):** when the server answers `{"type":"lease_conflict","code":"turn_in_progress"}`,
  another computer of the same person holds the turn: this bridge goes idle at once, sends nothing, plays nothing.
- **Computer actions:** `--actions <file>` also connects this Mac as a device, so JARVIS can run the actions from the
  file (see "Computer actions" below). `--link` connects it even without actions, so the computer shows in Konto →
  Komputery (the hologram app always passes it).
- **Satellite of a household (S-1e):** `--pair ABCD-EFGH` spends a code made by the household's admin on the
  Urządzenia page; the bridge keeps the device id and secret in the Keychain (a file under `~/.homeapps/voice-bridge/tokens`
  elsewhere) and from then on speaks for the household instead of a person: no browser login, 15-minute device
  tokens, always connected to `/devices/link` as a satellite. `--unpair` makes it a person's bridge again; `--logout`
  forgets both. If the household removes the satellite or makes a new code, the bridge forgets its pairing and asks
  for a new `--pair`.
- **Engine for the hologram app:** `--ipc`, see below. With `--ipc` the bridge registers in "Połączone aplikacje" as
  "JARVIS na komputerze" instead (a bridge registered before keeps its old name until `--logout`).
- **Licence:** the "hey jarvis" model is CC BY-NC-SA 4.0 (non-commercial), fine for a home.

## As the hologram app's engine: `--ipc` (S-2b)

With `--ipc` nobody types into the bridge: the hologram app (S-2c) starts it as a child process and talks to it in
JSON lines. **stdout** carries only events, one JSON object per line; everything people read (connecting, downloads,
`(przerwane)`) goes to **stderr**, so the app can log it. **stdin** takes commands; when stdin closes, the bridge quits.

```bash
dotnet run --project src/Jarvis.VoiceBridge -- --server https://app.localhost --insecure --ipc
{"type":"talk"}            # typed by hand to try it: like Enter
```

Events (stdout):

| `type` | Fields | When |
| --- | --- | --- |
| `state` | `state`: `connecting`, `idle`, `listening`, `thinking`, `speaking`, `muted` | only when it changes |
| `level` | `level`: 0 to 1 | ~30 times a second while JARVIS speaks (loudness relative to that sentence), then 0 |
| `heard` | `text` | what the server (or local speech) understood |
| `reply` | `text`, `actions`: `[{status, description}]` | JARVIS's answer as text |
| `confirm` | | JARVIS waits for "tak" / "nie"; `listening` follows without the wake word |
| `error` | `code`, `detail` | the server refused the turn, or an unknown command (`ipc.unknown_command`) |
| `login` | `url` | the person has to sign in; the browser is opened as well |

Commands (stdin): `{"type":"talk"}` (like Enter: start without the wake word, or end the utterance now),
`{"type":"stop"}` (drop the utterance, or stop JARVIS mid-answer), `{"type":"mute"}` / `{"type":"unmute"}` (ignore the
microphone, the wake word too), `{"type":"quit"}`.

## Computer actions

`--actions <file>` on the voice bridge connects the computer as a device. The file is a JSON array (comments allowed);
each action has `name`, `description`, `run` (a command for `/bin/sh -c`), optionally `input` (a JSON schema),
`readOnly` and `confirm`. The input reaches the command only as environment variables, `JARVIS_<FIELD>` for each field
and `JARVIS_INPUT` for the whole JSON, so quote them (`"$JARVIS_TEXT"`); never paste them into the command text. A
command has 15 s, and the first 2000 characters of its output (or of its errors when it fails) are JARVIS's answer.
Example for a Mac: `src/Jarvis.VoiceBridge/actions.macos.example.json`.

## Speech without Speaches (the default)

The models are large and are NOT in git. Put them in `~/.homeapps/voice-bridge/models/` (or point `--stt-model` and
`--tts-model` at them); the bridge names the file that is missing at start.

| File | Where from |
|---|---|
| `ggml-base.bin` (or `ggml-tiny.bin`, then `--stt-model`) | `https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin` |
| `pl_PL-gosia-medium.onnx` and `pl_PL-gosia-medium.onnx.json` | `https://huggingface.co/rhasspy/piper-voices/resolve/main/pl/pl_PL/gosia/medium/` |

Download example (macOS / Linux):

```bash
mkdir -p ~/.homeapps/voice-bridge/models && cd ~/.homeapps/voice-bridge/models
curl -LO https://huggingface.co/ggerganov/whisper.cpp/resolve/main/ggml-base.bin
curl -LO https://huggingface.co/rhasspy/piper-voices/resolve/main/pl/pl_PL/gosia/medium/pl_PL-gosia-medium.onnx
curl -LO https://huggingface.co/rhasspy/piper-voices/resolve/main/pl/pl_PL/gosia/medium/pl_PL-gosia-medium.onnx.json
```

Piper needs phonemes from the `espeak-ng` program, which the bridge starts itself, so it must be installed and on the
`PATH`: macOS `brew install espeak-ng`, Linux `apt install espeak-ng`, Windows install the espeak-ng package and make
sure its folder is on `PATH` (then open a new terminal). Without it, speaking an answer fails with "espeak-ng is not installed".

Echo cancellation is a level gate (`EchoGate`) against the WAV being played, not an adaptive filter: a microphone frame
counts as the person's voice only when it is louder than both 500 and 1.5 times the playback level at that moment.
The thresholds are constants in `src/Jarvis.VoiceBridge.Core/EchoGate.cs` (`Margin`, `Floor`, `Latency`), not
config options: if JARVIS interrupts himself, or "stop" is not heard, change the constant and rebuild.

Playback of the answer uses the system's player: `afplay` on macOS, `aplay` on Linux, and PowerShell's `SoundPlayer` on
Windows (see [desktop.md](desktop.md)).
