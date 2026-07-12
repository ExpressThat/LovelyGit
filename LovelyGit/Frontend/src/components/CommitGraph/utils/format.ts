import type { CommitGraphRow } from "@/generated/types";

export function shortHash(hash: string) {
	return hash.slice(0, 7);
}

export function refLabel(ref: string) {
	return ref.replace(/^origin\//, "").replace(/^refs\/heads\//, "");
}

export function formatDate(seconds: number) {
	return new Date(seconds * 1000).toLocaleString();
}

export function messagePrefix(row: CommitGraphRow) {
	const branch = row.commit.refs.find(
		(reference) => reference.kind === "Local",
	);
	if (row.isMergeCommit && branch) {
		return `Merge branch '${refLabel(branch.name)}' into seen`;
	}

	if (row.isMergeCommit) {
		return "Merge branch into seen";
	}

	return row.commit.message || "(no commit message)";
}
