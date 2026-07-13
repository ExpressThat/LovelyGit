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
		files: [
			...toInspectionFiles(tracked, "Tracked"),
			...(untracked ? toInspectionFiles(untracked, "Untracked") : []),
		],
		tracked,
		untracked,
	};
}

function toInspectionFiles(
	details: CommitDetailsResponse,
	source: StashInspectionFile["source"],
) {
	return details.changedFiles.map((file) => ({
		commitHash: details.hash,
		file,
		source,
	}));
}
