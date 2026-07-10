// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { useSubmoduleManager } from "./useSubmoduleManager";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("useSubmoduleManager", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads submodules through the native reader", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValue([submodule]);
		const { result } = renderHook(() => useSubmoduleManager("repository-id"));

		await act(() => result.current.load());

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repository-id" },
			commandType: "GetSubmodules",
		});
		expect(result.current.submodules).toEqual([submodule]);
	});

	it("runs a mutation and refreshes native state", async () => {
		vi.mocked(sendRequestWithResponse)
			.mockResolvedValueOnce(undefined)
			.mockResolvedValueOnce([{ ...submodule, state: "Current" }]);
		const { result } = renderHook(() => useSubmoduleManager("repository-id"));

		await act(() => result.current.run("deps/library", "Initialize"));

		expect(sendRequestWithResponse).toHaveBeenNthCalledWith(
			1,
			{
				arguments: {
					action: "Initialize",
					path: "deps/library",
					repositoryId: "repository-id",
				},
				commandType: "ManageSubmodule",
			},
			{ timeoutMs: 120_000 },
		);
		expect(result.current.submodules[0]?.state).toBe("Current");
	});
});

const submodule = {
	branch: null,
	currentCommit: null,
	expectedCommit: "1".repeat(40),
	name: "library",
	path: "deps/library",
	state: "Uninitialized" as const,
	url: "../library.git",
};
