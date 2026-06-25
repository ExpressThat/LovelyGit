import type { CommitChangedFile, WorkingTreeChangedFile } from "./generated/types";
import { sendRequestWithoutResponse } from "./lib/commands";

export type DetailsPanelState =
	| {
			commitHash: string;
			kind: "commit";
			selectedFile?: CommitChangedFile;
	  }
	| {
			kind: "workingChanges";
			selectedFile?: WorkingTreeChangedFile;
	  };

export function cancelCommitDiffPreparation(
	repositoryId: string,
	commitHash: string,
) {
	void sendRequestWithoutResponse({
		commandType: "CancelCommitDiffPreparation",
		arguments: {
			commitHash,
			repositoryId,
		},
	});
}

export function panelTitle(panel: DetailsPanelState | null) {
	if (panel?.kind === "commit") {
		return "Commit Details";
	}
	if (panel?.kind === "workingChanges") {
		return "Working Changes";
	}
	return "Details";
}
