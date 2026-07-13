import { describe, expect, it, vi } from "vitest";
import type { ConflictResolutionResponse } from "@/generated/types";
import { ConflictResolutionVariantCache } from "./conflictResolutionVariantCache";

describe("ConflictResolutionVariantCache", () => {
	it("reuses each prepared whitespace variant", async () => {
		const cache = new ConflictResolutionVariantCache();
		const response = {} as ConflictResolutionResponse;
		const exact = vi.fn().mockResolvedValue(response);
		const ignored = vi.fn().mockResolvedValue(response);

		await cache.load("repo\0file", false, exact);
		await cache.load("repo\0file", false, exact);
		await cache.load("repo\0file", true, ignored);
		await cache.load("repo\0file", true, ignored);

		expect(exact).toHaveBeenCalledOnce();
		expect(ignored).toHaveBeenCalledOnce();
	});

	it("drops variants when the repository or path changes", async () => {
		const cache = new ConflictResolutionVariantCache();
		const loader = vi.fn().mockResolvedValue({} as ConflictResolutionResponse);

		await cache.load("first", false, loader);
		await cache.load("second", false, loader);

		expect(loader).toHaveBeenCalledTimes(2);
	});

	it("passes the prepared opposite variant to a new loader", async () => {
		const cache = new ConflictResolutionVariantCache();
		const exact = {} as ConflictResolutionResponse;
		await cache.load("owner", false, async () => exact);
		const ignored = vi.fn().mockResolvedValue({} as ConflictResolutionResponse);

		await cache.load("owner", true, ignored);

		expect(ignored).toHaveBeenCalledWith(exact);
	});

	it("allows retry after a failed request", async () => {
		const cache = new ConflictResolutionVariantCache();
		const loader = vi
			.fn<() => Promise<ConflictResolutionResponse | null>>()
			.mockRejectedValueOnce(new Error("failed"))
			.mockResolvedValueOnce({} as ConflictResolutionResponse);

		await expect(cache.load("owner", false, loader)).rejects.toThrow("failed");
		await expect(cache.load("owner", false, loader)).resolves.toBeTruthy();
		expect(loader).toHaveBeenCalledTimes(2);
	});
});
