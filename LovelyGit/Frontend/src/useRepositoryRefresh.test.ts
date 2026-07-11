import { toast } from "sonner";
import { describe, expect, it, vi } from "vitest";
import { createRepositoryRefreshAction } from "./useRepositoryRefresh";

vi.mock("sonner", () => ({ toast: { error: vi.fn(), success: vi.fn() } }));

describe("createRepositoryRefreshAction", () => {
	it("reloads both repository surfaces and reports success", async () => {
		const reload = vi.fn().mockResolvedValue(undefined);
		const setToken = vi.fn();
		await createRepositoryRefreshAction(reload, setToken)();
		expect(reload).toHaveBeenCalledOnce();
		expect(setToken).toHaveBeenCalledOnce();
		expect(toast.success).toHaveBeenCalledWith("Repository refreshed");
	});

	it("preserves the graph and surfaces a working-tree failure", async () => {
		const setToken = vi.fn();
		await createRepositoryRefreshAction(
			vi.fn().mockRejectedValue(new Error("scan failed")),
			setToken,
		)();
		expect(setToken).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith("scan failed");
	});
});
