import { useLayoutEffect, useRef, useState } from "react";
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

const MAX_CACHED_VARIANTS = 4;

export function CommitFileDiffView({
	commitHash,
	comparisonCommitHash,
	file,
	onClose,
	parentIndex,
	repositoryId,
	showFileStats = true,
}: {
	commitHash: string;
	comparisonCommitHash?: string | null;
	file: CommitChangedFile;
	onClose: () => void;
	parentIndex: number;
	repositoryId: string;
	showFileStats?: boolean;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");
	const [state, setState] = useState<DiffState>({ status: "loading" });
	const responseCache = useRef(new Map<string, CommitFileDiffResponse>());
	const requestKey = diffRequestKey({
		commitHash,
		comparisonCommitHash,
		filePath: file.path,
		ignoreWhitespace,
		parentIndex,
		repositoryId,
		viewMode,
	});

	useLayoutEffect(() => {
		let isActive = true;
		const cached = responseCache.current.get(requestKey);
		if (cached) {
			setState({ status: "loaded", diff: cached });
			return () => {
				isActive = false;
			};
		}
		setState({ status: "loading" });

		sendRequestWithResponse({
			commandType: "GetCommitFileDiff",
			arguments: {
				commitHash,
				comparisonCommitHash: comparisonCommitHash ?? null,
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

				cacheResponse(responseCache.current, requestKey, diff);
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
		comparisonCommitHash,
		file.path,
		ignoreWhitespace,
		parentIndex,
		repositoryId,
		requestKey,
		viewMode,
	]);

	const handleClose = () => {
		setState({ status: "loading" });
		onClose();
	};

	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<CommitFileDiffHeader
				file={file}
				onClose={handleClose}
				showStats={showFileStats}
			/>
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

function cacheResponse(
	cache: Map<string, CommitFileDiffResponse>,
	key: string,
	diff: CommitFileDiffResponse,
) {
	cache.set(key, diff);
	if (cache.size <= MAX_CACHED_VARIANTS) return;
	const oldest = cache.keys().next().value;
	if (oldest) cache.delete(oldest);
}

function diffRequestKey(input: {
	commitHash: string;
	comparisonCommitHash?: string | null;
	filePath: string;
	ignoreWhitespace: boolean;
	parentIndex: number;
	repositoryId: string;
	viewMode: string;
}) {
	return [
		input.repositoryId,
		input.commitHash,
		input.comparisonCommitHash ?? "",
		input.parentIndex,
		input.filePath,
		input.viewMode,
		input.ignoreWhitespace ? "ignore" : "exact",
	].join("\0");
}
