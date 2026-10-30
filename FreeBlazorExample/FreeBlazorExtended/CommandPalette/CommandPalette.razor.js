/*
    Purpose: JavaScript hotkey bridge for the CommandPalette component.
    Fits: Registers a single global keydown handler and forwards the configured command hotkey to Blazor.
*/

let dotNetRef = null;
let hotkey = "k";
let listener = null;

export function init(dotNet, key) {
    dispose(); // idempotent — replace any prior binding

    dotNetRef = dotNet;
    hotkey = (key || "k").toLowerCase();

    listener = (e) => {
        // Ctrl on Windows/Linux, Meta (Cmd) on macOS — match either modifier.
        const modifier = e.ctrlKey || e.metaKey;
        if (modifier && !e.shiftKey && !e.altKey && e.key && e.key.toLowerCase() === hotkey) {
            e.preventDefault();
            if (dotNetRef) {
                dotNetRef.invokeMethodAsync("HandleHotkey");
            }
        }
    };

    document.addEventListener("keydown", listener);
}

export function dispose() {
    if (listener) {
        document.removeEventListener("keydown", listener);
        listener = null;
    }
    dotNetRef = null;
}
