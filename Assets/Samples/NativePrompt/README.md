# NativePrompt UI Toolkit Sample

Open `NativePromptSample.unity` and enter Play Mode. The centered 540 × 960
logical viewport exposes representative Alert, Bottom Sheet, Toast, Loading, and
Store Review scenarios for manual Editor or device verification. The latest callback,
Loading lifecycle event, or Store Review request fact appears in the `LATEST RESULT`
panel. Loading events also write their full request ID, metadata, end reason, and
active-request count to the Unity Console.

The compact Loading selector shows `S` / `M` / `L` and the representative positions
`TL` / `C` / `BR` in one row. It starts with no selection and no Loading. Selecting
a size fills a missing position with `BR`; selecting a position fills a missing size
with `M`. Every selection immediately replaces the active spinner-only request.
`With message` displays `Now Loading...` with the selected size and position.
`BG + block (5s)` remains as the combined background/blocking preset;
background-only and block-only presets are intentionally omitted from the sample.
Select `Dismiss loading` to end the active request and clear all selections. The
blocking preset automatically dismisses after five seconds so it cannot leave the
sample controls inaccessible. The manual toast remains visible until `Dismiss
manual` is selected. `Request review (test)` calls `NP.RequestReview()` and reports
only that the request was made; it does not claim that store UI appeared. In the
Editor, prompt APIs use an iOS-inspired Game view preview with white surfaces and a
dark pill-shaped Toast. Store Review shows an image-free simulated review card and
writes a test log without claiming that platform UI appeared. Final native appearance
is verified on a device; the PlayMode tests cover scene loading, UI wiring, viewport
ratio, and result updates.
