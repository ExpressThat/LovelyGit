import { beforeEach, describe, expect, it, vi } from "vitest";
import type { WorkingTreeChangesResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	commitStagedChanges,
	loadHeadCommitMessage,
} from "./WorkingChangesPanelCommands";

vi.mock("@/lib/commands", () => ({ sendRequestWithResponse: vi.fn() }));

describe("working changes commit commands", () => {
	beforeEach(() => vi.clearAllMocks());

	it("does not create a normal commit without staged changes", async () => {
		const harness = createCommitHarness({ amend: false });

		await commitStagedChanges(harness);

		expect(sendRequestWithResponse).not.toHaveBeenCalled();
	});

	it("allows a message-only amend and clears the form after success", async () => {
		const harness = createCommitHarness({ amend: true });

		await commitStagedChanges(harness);

		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: {
				amend: true,
				body: "Updated body",
				repositoryId: "repo",
				title: "Updated title",
			},
			commandType: "CommitStagedChanges",
		});
		expect(harness.setCommitTitle).toHaveBeenCalledWith("");
		expect(harness.setCommitBody).toHaveBeenCalledWith("");
		expect(harness.setIsAmending).toHaveBeenCalledWith(false);
		expect(harness.onCommitSuccess).toHaveBeenCalledOnce();
	});

	it("keeps the amend form intact and shows native failures", async () => {
		vi.mocked(sendRequestWithResponse).mockRejectedValueOnce(
			new Error("signing failed"),
		);
		const harness = createCommitHarness({ amend: true });

		await commitStagedChanges(harness);

		expect(harness.setActionError).toHaveBeenLastCalledWith("signing failed");
		expect(harness.setCommitTitle).not.toHaveBeenCalled();
		expect(harness.setIsCommitting).toHaveBeenLastCalledWith(false);
	});

	it("loads the HEAD message through the dedicated native read", async () => {
		vi.mocked(sendRequestWithResponse).mockResolvedValueOnce({
			body: "Body",
			hash: "a".repeat(40),
			title: "Title",
		});

		await expect(loadHeadCommitMessage("repo")).resolves.toMatchObject({
			title: "Title",
		});
		expect(sendRequestWithResponse).toHaveBeenCalledWith({
			arguments: { repositoryId: "repo" },
			commandType: "GetHeadCommitMessage",
		});
	});
});

function createCommitHarness({ amend }: { amend: boolean }) {
	return {
		amend,
		changes: emptyChanges(),
		commitBody: "Updated body",
		commitTitle: "Updated title",
		onCommitSuccess: vi.fn(),
		repositoryId: "repo",
		setActionError: vi.fn(),
		setCommitBody: vi.fn(),
		setCommitTitle: vi.fn(),
		setIsAmending: vi.fn(),
		setIsCommitting: vi.fn(),
		setSelectedKeys: vi.fn(),
	};
}

function emptyChanges(): WorkingTreeChangesResponse {
	return {
		staged: [],
		totalCount: 0,
		unmerged: [],
		unstaged: [],
		untracked: [],
	};
}
