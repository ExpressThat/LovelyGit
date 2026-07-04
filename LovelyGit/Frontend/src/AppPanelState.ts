import type {
	CommitChangedFile,
	WorkingTreeChangedFile,
} from "./generated/types";

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

export function panelTitle(panel: DetailsPanelState | null) {
	if (panel?.kind === "commit") {
		return "Commit Details";
	}
	if (panel?.kind === "workingChanges") {
		return "Working Changes";
	}
	return "Details";
}
