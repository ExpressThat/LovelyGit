// @vitest-environment jsdom

import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { useWorkingChangesPanelActions } from "./useWorkingChangesPanelActions";
import { loadHeadCommitMessage } from "./WorkingChangesPanelCommands";

vi.mock("./WorkingChangesPanelCommands", () => ({
	commitStagedChanges: vi.fn(),
	discardWorkingChanges: vi.fn(),
	loadHeadCommitMessage: vi.fn(),
	runIndexCommand: vi.fn(),
}));

describe("useWorkingChangesPanelActions amend state", () => {
	beforeEach(() => vi.clearAllMocks());

	it("loads HEAD and restores the user's draft when amend is disabled", async () => {
		vi.mocked(loadHeadCommitMessage).mockResolvedValueOnce({
			body: "Existing body",
			hash: "a".repeat(40),
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
		}),
	);
}
