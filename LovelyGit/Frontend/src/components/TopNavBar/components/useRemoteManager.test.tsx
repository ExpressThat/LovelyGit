// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { useRemoteManager } from "./useRemoteManager";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

const origin = {
	name: "origin",
	pushUrl: null,
	url: "https://example.invalid/origin.git",
};

describe("useRemoteManager", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads remotes with the native read when opened", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce([origin]);
		const { result } = renderManager();

		await waitFor(() => expect(result.current.isLoading).toBe(false));

		expect(result.current.remotes).toEqual([origin]);
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repo" },
			commandType: "GetRemotes",
		});
	});

	it("adds a remote then refreshes the native list", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce([])
			.mockResolvedValueOnce(undefined);
		const { result } = renderManager();
		await waitFor(() => expect(result.current.isLoading).toBe(false));

		await act(() =>
			result.current.save({
				name: "origin",
				originalName: null,
				pushUrl: "",
				url: origin.url,
			}),
		);

		expect(sendRequestWithResponse).toHaveBeenNthCalledWith(
			2,
			expect.objectContaining({
				arguments: expect.objectContaining({
					action: "Add",
					name: "origin",
					url: origin.url,
				}),
				commandType: "ManageRemote",
			}),
			expect.anything(),
		);
		expect(result.current.remotes).toEqual([origin]);
	});

	it("removes the selected remote and refreshes", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce([origin])
			.mockResolvedValueOnce(undefined);
		const { result } = renderManager();
		await waitFor(() => expect(result.current.remotes).toEqual([origin]));
		act(() => result.current.startRemove(origin));

		await act(() => result.current.confirmRemove());

		expect(sendRequestWithResponse).toHaveBeenNthCalledWith(
			2,
			expect.objectContaining({
				arguments: expect.objectContaining({
					action: "Remove",
					name: "origin",
				}),
			}),
			expect.anything(),
		);
		expect(result.current.removeTarget).toBeNull();
		expect(result.current.remotes).toEqual([]);
	});

	it("keeps the editor open and exposes mutation errors", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce([origin])
			.mockRejectedValueOnce(new Error("remote already exists"));
		const { result } = renderManager();
		await waitFor(() => expect(result.current.remotes).toEqual([origin]));
		act(() => result.current.startAdd());

		await act(() =>
			result.current.save({
				name: "origin",
				originalName: null,
				pushUrl: "",
				url: origin.url,
			}),
		);

		expect(result.current.error).toBe("remote already exists");
		expect(result.current.editor).not.toBeNull();
	});
});

function renderManager() {
	return renderHook(() => useRemoteManager("repo", true));
}
