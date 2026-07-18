import { useEffect, useState } from "react";
import type {
	CommitChangedFile,
	CommitDetailsResponse,
	RepositoryStashItem,
} from "@/generated/types";
import { loadCommitDetails } from "@/lib/commitDetailsCache";

export type StashInspectionFile = {
	commitHash: string;
	file: CommitChangedFile;
	source: "Tracked" | "Untracked";
};

export type StashInspectionState =
	| { status: "idle" | "loading" }
	| { status: "error"; message: string }
	| {
			status: "loaded";
			files: StashInspectionFile[];
			tracked: CommitDetailsResponse;
			untracked: CommitDetailsResponse | null;
	  };

export function useStashInspection(
	repositoryId: string,
	stash: RepositoryStashItem | null,
) {
	const [retryToken, setRetryToken] = useState(0);
	const [state, setState] = useState<StashInspectionState>({ status: "idle" });

	useEffect(() => {
		void retryToken;
		if (!stash) {
			setState({ status: "idle" });
			return;
		}

		let active = true;
		setState({ status: "loading" });
		loadInspection(repositoryId, stash.commitHash).then(
			(result) => {
				if (active) setState({ status: "loaded", ...result });
			},
			(error: unknown) => {
				if (!active) return;
				setState({
					status: "error",
					message:
						error instanceof Error ? error.message : "Failed to inspect stash.",
				});
			},
		);

		return () => {
			active = false;
		};
	}, [repositoryId, retryToken, stash]);

	return { retry: () => setRetryToken((value) => value + 1), state };
}

async function loadInspection(repositoryId: string, stashHash: string) {
	const tracked = await loadCommitDetails(repositoryId, stashHash, 0);
	const untrackedHash = tracked.parents[2];
	const untracked = untrackedHash
		? await loadCommitDetails(repositoryId, untrackedHash, 0)
		: null;
	return {
		files: toInspectionFiles(tracked, untracked),
		tracked,
		untracked,
	};
}

function toInspectionFiles(
	tracked: CommitDetailsResponse,
	untracked: CommitDetailsResponse | null,
) {
	const trackedCount = tracked.changedFiles.length;
	const untrackedCount = untracked?.changedFiles.length ?? 0;
	const files = new Array<StashInspectionFile>(trackedCount + untrackedCount);
	for (let index = 0; index < trackedCount; index++) {
		files[index] = {
			commitHash: tracked.hash,
			file: tracked.changedFiles[index],
			source: "Tracked",
		};
	}
	if (!untracked) return files;
	for (let index = 0; index < untrackedCount; index++) {
		files[trackedCount + index] = {
			commitHash: untracked.hash,
			file: untracked.changedFiles[index],
			source: "Untracked",
		};
	}
	return files;
}
