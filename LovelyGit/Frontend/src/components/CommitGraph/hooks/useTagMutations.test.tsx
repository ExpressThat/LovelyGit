// @vitest-environment jsdom

import { act, renderHook, waitFor } from "@testing-library/react";
import { toast } from "sonner";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { sendRequestWithResponse } from "@/lib/commands";
import { useTagMutations } from "./useTagMutations";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));
vi.mock("sonner", () => ({
	toast: { error: vi.fn(), loading: vi.fn(() => "toast"), success: vi.fn() },
}));

const send = vi.mocked(sendRequestWithResponse);

describe("useTagMutations", () => {
	beforeEach(() => vi.clearAllMocks());

	it("failed delete preserves confirmation and a successful retry refreshes", async () => {
		const onRepositoryChanged = vi.fn();
		send.mockRejectedValueOnce(new Error("Tag is protected"));
		const { result } = renderHook(() =>
			useTagMutations({
				onRepositoryChanged,
				remoteName: "origin",
				repositoryId: "repo",
			}),
		);
		act(() => result.current.manageTag("delete", "v1"));

		await act(() => result.current.deleteTag());

		expect(result.current.deleteTagName).toBe("v1");
		expect(result.current.busyTag).toBeNull();
		expect(onRepositoryChanged).not.toHaveBeenCalled();
		expect(toast.error).toHaveBeenCalledWith("Tag is protected", {
			id: "toast",
		});

		send.mockResolvedValueOnce(undefined);
		await act(() => result.current.deleteTag());
		expect(result.current.deleteTagName).toBeNull();
		expect(onRepositoryChanged).toHaveBeenCalledOnce();
	});

	it("failed push re-enables the tag and permits retry", async () => {
		send.mockRejectedValueOnce(new Error("Authentication failed"));
		const { result } = renderHook(() =>
			useTagMutations({
				onRepositoryChanged: vi.fn(),
				remoteName: "origin",
				repositoryId: "repo",
			}),
		);

		act(() => result.current.manageTag("push", "v1"));
		await waitFor(() => expect(result.current.busyTag).toBeNull());
		expect(toast.error).toHaveBeenCalledWith("Authentication failed", {
			id: "toast",
		});

		send.mockResolvedValueOnce(undefined);
		act(() => result.current.manageTag("push", "v1"));
		await waitFor(() => expect(toast.success).toHaveBeenCalled());
		expect(send).toHaveBeenCalledTimes(2);
	});

	it("does not push when repository or remote is missing", () => {
		const { result } = renderHook(() =>
			useTagMutations({
				onRepositoryChanged: vi.fn(),
				remoteName: null,
				repositoryId: null,
			}),
		);

		act(() => result.current.manageTag("push", "v1"));

		expect(send).not.toHaveBeenCalled();
		expect(result.current.busyTag).toBeNull();
	});
});
