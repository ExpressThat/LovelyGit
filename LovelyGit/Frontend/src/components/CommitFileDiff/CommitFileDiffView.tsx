import { useEffect, useState } from "react";
import type {
	CommitChangedFile,
	CommitFileDiffResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { useSetting } from "@/lib/settings/settingsStore";
import { CommitFileDiffHeader } from "./CommitFileDiffHeader";
import { DiffContent, LoadingDiff } from "./DiffContent";

type DiffState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "loaded"; diff: CommitFileDiffResponse };

export function CommitFileDiffView({
	commitHash,
	file,
	onClose,
	parentIndex,
	repositoryId,
}: {
	commitHash: string;
	file: CommitChangedFile;
	onClose: () => void;
	parentIndex: number;
	repositoryId: string;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");
	const [state, setState] = useState<DiffState>({ status: "loading" });

	useEffect(() => {
		let isActive = true;
		setState({ status: "loading" });

		sendRequestWithResponse({
			commandType: "GetCommitFileDiff",
			arguments: {
				commitHash,
				path: file.path,
				ignoreWhitespace,
				parentIndex,
				repositoryId,
				viewMode,
			},
		})
			.then((diff) => {
				if (!isActive) {
					return;
				}

				if (!diff) {
					setState({ status: "error", message: "File diff was empty." });
					return;
				}

				setState({ status: "loaded", diff });
			})
			.catch((error: unknown) => {
				if (!isActive) {
					return;
				}

				setState({
					status: "error",
					message:
						error instanceof Error
							? error.message
							: "Failed to load file diff.",
				});
			});

		return () => {
			isActive = false;
		};
	}, [
		commitHash,
		file.path,
		ignoreWhitespace,
		parentIndex,
		repositoryId,
		viewMode,
	]);

	const handleClose = () => {
		setState({ status: "loading" });
		onClose();
	};

	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<CommitFileDiffHeader file={file} onClose={handleClose} />
			<div className="min-h-0 flex-1 overflow-hidden bg-background">
				{state.status === "loading" ? <LoadingDiff /> : null}
				{state.status === "error" ? (
					<div className="m-4 rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
						{state.message}
					</div>
				) : null}
				{state.status === "loaded" ? (
					<DiffContent
						contextLines={contextLines}
						diff={state.diff}
						lineDisplayMode={lineDisplayMode}
						wrapLines={wrapLines}
					/>
				) : null}
			</div>
		</section>
	);
}
