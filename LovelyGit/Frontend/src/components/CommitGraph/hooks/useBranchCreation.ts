import { useState } from "react";
import type { BranchCreationSource } from "@/components/TopNavBar/components/CreateBranchDialog";
import type { CommitGraphRow, GitReflogEntry } from "@/generated/types";
import { shortHash } from "../utils/format";

export function useBranchCreation() {
	const [source, setSource] = useState<BranchCreationSource | null>(null);

	const createAtCommit = (row: CommitGraphRow) => {
		const subject =
			row.commit.message.split(/\r?\n/, 1)[0] || "(no commit message)";
		setSource({
			description: subject,
			label: `commit ${shortHash(row.commit.hash)}`,
			startPoint: row.commit.hash,
		});
	};

	const createFromTag = (tagName: string, commitHash: string) => {
		setSource({
			description: `Tag ${tagName}`,
			label: `tag ${tagName}`,
			startPoint: commitHash,
		});
	};
	const createFromReflog = (entry: GitReflogEntry) => {
		setSource({
			description: entry.message || `Recovery point ${entry.selector}`,
			label: entry.selector,
			startPoint: entry.newHash,
		});
	};

	return {
		close: () => setSource(null),
		createAtCommit,
		createFromReflog,
		createFromTag,
		source,
	};
}
