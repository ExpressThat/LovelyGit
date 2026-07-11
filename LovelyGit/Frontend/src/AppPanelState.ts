import type {
	CommitChangedFile,
	WorkingTreeChangedFile,
} from "./generated/types";

export type DetailsPanelState =
	| {
			commitHash: string;
			kind: "commit";
			parentIndex?: number;
			selectedFile?: CommitChangedFile;
	  }
	| {
			kind: "workingChanges";
			selectedFile?: WorkingTreeChangedFile;
	  };

export type CommitDetailsPanelState = Extract<
	DetailsPanelState,
	{ kind: "commit" }
>;

export function commitDetailsPanel(
	commitHash: string,
	parentIndex?: number,
	selectedFile?: CommitChangedFile,
): CommitDetailsPanelState {
	return { commitHash, kind: "commit", parentIndex, selectedFile };
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
