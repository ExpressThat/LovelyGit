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
	workingNewText,
	workingOldText,
} from "./WorkingTreeFileDiffHelpers";

type DiffState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "loaded"; diff: CommitFileDiffResponse };

export function WorkingTreeFileDiffView({
	file,
	onClose,
	repositoryId,
}: {
	file: WorkingTreeChangedFile;
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

	const refreshDiffInPlace = async () => {
		try {
			const diff = await fetchDiff();
			if (!diff) {
				setState({ status: "error", message: "File diff was empty." });
				return;
			}

			setState({ status: "loaded", diff });
		} catch (error: unknown) {
			setState({
				status: "error",
				message:
					error instanceof Error
						? error.message
						: "Failed to load working file diff.",
			});
		}
	};

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

	const stageLine = async (line: CommitFileDiffLine) => {
		setIsLineActionBusy(true);
		try {
			await sendRequestWithResponse({
				commandType: "StageWorkingTreeLine",
				arguments: {
					changeType: line.changeType,
					group: file.group,
					newLineNumber: line.newLineNumber,
					newText: workingNewText(line),
					oldLineNumber: line.oldLineNumber,
					oldText: workingOldText(line),
					path: file.path,
					repositoryId,
				},
			});
			removeLineFromCurrentDiff(line);
			await refreshDiffInPlace();
		} catch (error) {
			setState({
				status: "error",
				message:
					error instanceof Error ? error.message : "Failed to stage line.",
			});
		} finally {
			setIsLineActionBusy(false);
		}
	};

	const unstageLine = async (line: CommitFileDiffLine) => {
		setIsLineActionBusy(true);
		try {
			await sendRequestWithResponse({
				commandType: "UnstageWorkingTreeLine",
				arguments: {
					changeType: line.changeType,
					group: file.group,
					newLineNumber: line.newLineNumber,
					newText: workingNewText(line),
					oldLineNumber: line.oldLineNumber,
					oldText: workingOldText(line),
					path: file.path,
					repositoryId,
				},
			});
			removeLineFromCurrentDiff(line);
			await refreshDiffInPlace();
		} catch (error) {
			setState({
				status: "error",
				message:
					error instanceof Error ? error.message : "Failed to unstage line.",
			});
		} finally {
			setIsLineActionBusy(false);
		}
	};

	const removeLineFromCurrentDiff = (line: CommitFileDiffLine) => {
		setState((current) => {
			if (current.status !== "loaded") {
				return current;
			}

			const nextLines = current.diff.lines.filter(
				(currentLine) => !isSameDiffLine(currentLine, line),
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
						onUnstageLine={
							canUnstageLines(file.group) ? unstageLine : undefined
						}
						wrapLines={wrapLines}
					/>
				) : null}
			</div>
		</section>
	);
}
