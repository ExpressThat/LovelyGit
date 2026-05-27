import type { CommitGraphRow } from "../types/graph";

export function shortHash(hash: string) {
	return hash.slice(0, 7);
}

export function refLabel(ref: string) {
	return ref.replace(/^origin\//, "").replace(/^refs\/heads\//, "");
}

export function formatDate(seconds: number) {
	return new Date(seconds * 1000).toISOString().slice(0, 10);
}

export function messagePrefix(row: CommitGraphRow) {
	if (row.isMergeCommit && row.commit.branches.length > 0) {
		return `Merge branch '${refLabel(row.commit.branches[0])}' into seen`;
	}

	if (row.isMergeCommit) {
		return "Merge branch into seen";
	}

	return row.commit.message || "(no commit message)";
}
