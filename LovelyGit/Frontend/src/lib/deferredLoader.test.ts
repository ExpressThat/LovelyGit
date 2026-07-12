import { describe, expect, it, vi } from "vitest";
import { createDeferredLoader } from "./deferredLoader";

describe("createDeferredLoader", () => {
	it("shares pending and completed loads", async () => {
		const value = { name: "conflict" };
		const load = vi.fn(() => Promise.resolve(value));
		const loader = createDeferredLoader(load);

		const first = loader.load();
		expect(loader.load()).toBe(first);
		await expect(first).resolves.toBe(value);
		expect(loader.get()).toBe(value);
		await expect(loader.load()).resolves.toBe(value);
		expect(load).toHaveBeenCalledOnce();
	});

	it("does not retain a failed speculative load", async () => {
		const load = vi
			.fn<() => Promise<string>>()
			.mockRejectedValueOnce(new Error("chunk unavailable"))
			.mockResolvedValueOnce("conflict");
		const loader = createDeferredLoader(load);

		await expect(loader.load()).rejects.toThrow("chunk unavailable");
		expect(loader.get()).toBeNull();
		await expect(loader.load()).resolves.toBe("conflict");
	});
});
