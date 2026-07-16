# NativePrompt UI Toolkit Sample

Open `NativePromptSample.unity` and enter Play Mode. The centered 540 × 960
logical viewport exposes representative Alert, Bottom Sheet, Toast, and Loading scenarios
for manual Editor or device verification. The latest callback result appears in
the `LATEST RESULT` panel.

The compact Loading selector combines `S` / `M` / `L` with the five supported
positions (`TL`, `TR`, `C`, `BL`, `BR`). Selecting either dimension immediately
replaces the active spinner-only request while preserving the other selection.
`BG + block (3s)` remains as the combined background/blocking preset; background-only
and block-only presets are intentionally omitted from the sample. Select
`Dismiss loading` to end the active request. The blocking preset automatically
dismisses after three seconds so it cannot leave the sample controls inaccessible.
The manual toast remains visible until
`Dismiss manual` is selected. Native UI
appearance is intentionally verified on a device; the PlayMode tests cover scene
loading, UI wiring, viewport ratio, and result updates.
