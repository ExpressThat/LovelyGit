// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useSetting } from "@/lib/settings/settingsStore";
import { useWorkingChangesPanelActions } from "./useWorkingChangesPanelActions";
import { loadHeadCommitMessage } from "./WorkingChangesPanelCommands";

vi.mock("./WorkingChangesPanelCommands", () => ({
	commitStagedChanges: vi.fn(),
	discardWorkingChanges: vi.fn(),
	loadHeadCommitMessage: vi.fn(),
	runIndexCommand: vi.fn(),
}));
vi.mock("@/lib/settings/settingsStore", () => ({ useSetting: vi.fn() }));

describe("useWorkingChangesPanelActions amend state", () => {
	beforeEach(() => {
		vi.clearAllMocks();
		vi.mocked(useSetting).mockReturnValue(false);
	});

	it("starts with the persisted signing preference and permits an override", () => {
		vi.mocked(useSetting).mockReturnValue(true);
		const { result } = renderActions();

		expect(result.current.isSigningCommit).toBe(true);
		act(() => result.current.setIsSigningCommit(false));
		expect(result.current.isSigningCommit).toBe(false);
	});

	it("loads HEAD and restores the user's draft when amend is disabled", async () => {
		vi.mocked(loadHeadCommitMessage).mockResolvedValueOnce({
			body: "Existing body",
			firstParentHash: "b".repeat(40),
			hash: "a".repeat(40),
			parentCount: 1,
			title: "Existing title",
		});
		const { result } = renderActions();
		act(() => {
			result.current.setCommitTitle("Draft title");
			result.current.setCommitBody("Draft body");
		});

		await act(() => result.current.toggleAmend(true));

		expect(result.current.isAmending).toBe(true);
		expect(result.current.commitTitle).toBe("Existing title");
		expect(result.current.commitBody).toBe("Existing body");
		await act(() => result.current.toggleAmend(false));
		expect(result.current.isAmending).toBe(false);
		expect(result.current.commitTitle).toBe("Draft title");
		expect(result.current.commitBody).toBe("Draft body");
	});

	it("leaves amend disabled when the native read fails", async () => {
		vi.mocked(loadHeadCommitMessage).mockRejectedValueOnce(
			new Error("No commits yet"),
		);
		const { result } = renderActions();

		await act(() => result.current.toggleAmend(true));

		expect(result.current.isAmending).toBe(false);
		expect(result.current.actionError).toBe("No commits yet");
		expect(result.current.isLoadingAmendMessage).toBe(false);
	});
});

function renderActions() {
	return renderHook(() =>
		useWorkingChangesPanelActions({
			changes: null,
			onCommitSuccess: vi.fn(),
			onRefresh: vi.fn(),
			repositoryId: "repo",
			setOptimisticChanges: vi.fn(),
		}),
	);
}
