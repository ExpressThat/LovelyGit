import { useCallback, useEffect, useState } from "react";
import type {
	CommitFileDiffLine,
	CommitFileDiffResponse,
	WorkingTreeChangedFile,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { useSetting } from "@/lib/settings/settingsStore";
import { DiffContent } from "../CommitFileDiff/DiffContent";
import { WorkingTreeDiffHeader } from "./WorkingTreeDiffHeader";
import {
	canStageLines,
	canUnstageLines,
	isChangedLine,
	isSameDiffLine,
	LoadingDiff,
} from "./WorkingTreeFileDiffHelpers";
import {
	moveWorkingTreeHunk,
	moveWorkingTreeLine,
} from "./WorkingTreePartialStageCommands";

type DiffState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "loaded"; diff: CommitFileDiffResponse };

export function WorkingTreeFileDiffView({
	file,
	onChange,
	onClose,
	repositoryId,
}: {
	file: WorkingTreeChangedFile;
	onChange?: () => Promise<void> | void;
	onClose: () => void;
	repositoryId: string;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");
	const [state, setState] = useState<DiffState>({ status: "loading" });
	const [isLineActionBusy, setIsLineActionBusy] = useState(false);

	const fetchDiff = useCallback(
		() =>
			sendRequestWithResponse({
				commandType: "GetWorkingTreeFileDiff",
				arguments: {
					path: file.path,
					group: file.group,
					ignoreWhitespace,
					repositoryId,
					viewMode,
				},
			}),
		[file.group, file.path, ignoreWhitespace, repositoryId, viewMode],
	);

	useEffect(() => {
		let isActive = true;
		setState({ status: "loading" });

		fetchDiff()
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
							: "Failed to load working file diff.",
				});
			});

		return () => {
			isActive = false;
		};
	}, [fetchDiff]);

	const moveLines = async (
		kind: "stage" | "unstage",
		lines: CommitFileDiffLine[],
	) => {
		if (state.status !== "loaded") return;
		const diff = state.diff;
		setIsLineActionBusy(true);
		try {
			if (lines.length === 1) {
				await moveWorkingTreeLine(kind, repositoryId, file, diff, lines[0]);
			} else {
				await moveWorkingTreeHunk(kind, repositoryId, file, diff, lines);
			}
			removeLinesFromCurrentDiff(lines);
			refreshWorkingChangesList(onChange);
		} catch (error) {
			setState({
				status: "error",
				message:
					error instanceof Error
						? error.message
						: `Failed to ${kind} ${lines.length === 1 ? "line" : "hunk"}.`,
			});
		} finally {
			setIsLineActionBusy(false);
		}
	};

	const stageLine = (line: CommitFileDiffLine) => moveLines("stage", [line]);
	const unstageLine = (line: CommitFileDiffLine) =>
		moveLines("unstage", [line]);
	const stageHunk = (lines: CommitFileDiffLine[]) => moveLines("stage", lines);
	const unstageHunk = (lines: CommitFileDiffLine[]) =>
		moveLines("unstage", lines);

	const removeLinesFromCurrentDiff = (lines: CommitFileDiffLine[]) => {
		setState((current) => {
			if (current.status !== "loaded") {
				return current;
			}

			if (current.diff.compactLinesGzipBase64) {
				return current;
			}

			const nextLines = current.diff.lines.filter((currentLine) =>
				lines.every((line) => !isSameDiffLine(currentLine, line)),
			);

			return {
				status: "loaded",
				diff: {
					...current.diff,
					hasDifferences: nextLines.some(isChangedLine),
					lines: nextLines,
				},
			};
		});
	};

	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<WorkingTreeDiffHeader file={file} onClose={onClose} />
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
						isLineActionBusy={isLineActionBusy}
						lineDisplayMode={lineDisplayMode}
						onStageLine={canStageLines(file.group) ? stageLine : undefined}
						onStageHunk={canStageLines(file.group) ? stageHunk : undefined}
						onUnstageLine={
							canUnstageLines(file.group) ? unstageLine : undefined
						}
						onUnstageHunk={
							canUnstageLines(file.group) ? unstageHunk : undefined
						}
						wrapLines={wrapLines}
					/>
				) : null}
			</div>
		</section>
	);
}

function refreshWorkingChangesList(onChange?: () => Promise<void> | void) {
	Promise.resolve(onChange?.()).catch((error) => {
		console.error(
			"Failed to refresh working changes after line action.",
			error,
		);
	});
}
