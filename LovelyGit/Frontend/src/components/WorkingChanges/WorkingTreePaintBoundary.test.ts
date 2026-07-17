import { afterEach, describe, expect, it, vi } from "vitest";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

describe("waitForWorkingTreePaint", () => {
	afterEach(() => vi.unstubAllGlobals());

	it("waits until a frame can be presented before continuing", async () => {
		const callbacks: FrameRequestCallback[] = [];
		vi.stubGlobal(
			"requestAnimationFrame",
			vi.fn((callback: FrameRequestCallback) => {
				callbacks.push(callback);
				return callbacks.length;
			}),
		);
		let completed = false;
		const pending = waitForWorkingTreePaint().then(() => {
			completed = true;
		});

		callbacks.shift()?.(0);
		await Promise.resolve();
		expect(completed).toBe(false);
		callbacks.shift()?.(16);
		await pending;
		expect(completed).toBe(true);
	});

	it("does not delay non-browser tests", async () => {
		vi.stubGlobal("requestAnimationFrame", undefined);
		await expect(waitForWorkingTreePaint()).resolves.toBeUndefined();
	});
});
