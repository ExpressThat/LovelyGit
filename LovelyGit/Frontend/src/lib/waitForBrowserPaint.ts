/**
 * Lets Chromium present pending UI state before the synchronous native host
 * bridge occupies the renderer thread. Two frames are intentional: promise
 * continuations from the first callback run before that frame paints.
 */
export async function waitForBrowserPaint() {
	if (typeof requestAnimationFrame !== "function") return;
	await new Promise<void>((resolve) => {
		requestAnimationFrame(() => requestAnimationFrame(() => resolve()));
	});
}
