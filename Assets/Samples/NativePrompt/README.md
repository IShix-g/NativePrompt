# NativePrompt UI Toolkit Sample

Open `NativePromptSample.unity` and enter Play Mode. The centered 540 × 960
logical viewport exposes representative Alert, Bottom Sheet, Toast, and Loading scenarios
for manual Editor or device verification. The latest callback result appears in
the `LATEST RESULT` panel.

The Loading controls cover all four background/input-block combinations, multiple
positions and sizes, messages, and immediate/delayed presentation. Select
`Dismiss loading` to end the active request. Blocking Loading samples automatically
dismiss after three seconds so they cannot leave the sample controls inaccessible.
The manual toast remains visible until
`Dismiss manual` is selected. Native UI
appearance is intentionally verified on a device; the PlayMode tests cover scene
loading, UI wiring, viewport ratio, and result updates.
