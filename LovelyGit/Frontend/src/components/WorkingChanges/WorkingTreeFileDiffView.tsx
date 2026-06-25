import { Columns2, FileText, ListCollapse, Rows3, WrapText, X } from "lucide-react";
import { useEffect, useState } from "react";
import type { CommitFileDiffLine, CommitFileDiffResponse } from "@/generated/types";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { ContextLinesControl, DiffContent } from "../CommitFileDiff/CommitFileDiffView";

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
	const [state, setState] = useState<DiffState>({ status: "loading" });
	const [isLineActionBusy, setIsLineActionBusy] = useState(false);

	const fetchDiff = () =>
		sendRequestWithResponse({
			commandType: "GetWorkingTreeFileDiff",
			arguments: {
				path: file.path,
				group: file.group,
				repositoryId,
				viewMode,
			},
		});

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

	const loadDiff = (showLoading = true) => {
		let isActive = true;
		if (showLoading) {
			setState({ status: "loading" });
		}

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
	};

	useEffect(loadDiff, [file.group, file.path, repositoryId, viewMode]);

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
					error instanceof Error
						? error.message
						: "Failed to stage line.",
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
					error instanceof Error
						? error.message
						: "Failed to unstage line.",
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
			<header className="shrink-0 border-b bg-popover text-popover-foreground">
				<div className="flex h-10 items-center gap-2 px-3">
					<div className="min-w-0 flex-1 truncate font-mono text-sm text-muted-foreground">
						<span>{folderPrefix(file.path)}</span>
						<span className="font-semibold text-foreground">{fileName(file.path)}</span>
					</div>
					<div className="hidden items-center gap-2 text-[10px] uppercase text-muted-foreground md:flex">
						<span>{file.group}</span>
						<span>{file.status}</span>
					</div>
					<button
						aria-label="Close diff"
						className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
						onClick={onClose}
						type="button"
					>
						<X aria-hidden="true" size={16} />
					</button>
				</div>
				<div className="flex h-10 items-center justify-center border-t bg-card/60 px-3">
					<div className="inline-flex rounded-md border bg-background p-0.5">
						<ModeButton icon={<Columns2 aria-hidden="true" size={14} />} isActive={viewMode === "SideBySide"} label="Side by side" onClick={() => void setSetting("CommitDiffViewMode", "SideBySide")} />
						<ModeButton icon={<Rows3 aria-hidden="true" size={14} />} isActive={viewMode === "Combined"} label="Combined" onClick={() => void setSetting("CommitDiffViewMode", "Combined")} />
					</div>
					<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
						<ModeButton icon={<ListCollapse aria-hidden="true" size={14} />} isActive={lineDisplayMode === "Changes"} label="Changes" onClick={() => void setSetting("CommitDiffLineDisplayMode", "Changes")} />
						<ModeButton icon={<FileText aria-hidden="true" size={14} />} isActive={lineDisplayMode === "FullFile"} label="Full file" onClick={() => void setSetting("CommitDiffLineDisplayMode", "FullFile")} />
					</div>
					{lineDisplayMode === "Changes" ? (
						<ContextLinesControl contextLines={contextLines} />
					) : null}
					<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
						<ModeButton icon={<WrapText aria-hidden="true" size={14} />} isActive={wrapLines} label="Wrap lines" onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)} />
					</div>
				</div>
			</header>
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
						onUnstageLine={canUnstageLines(file.group) ? unstageLine : undefined}
						wrapLines={wrapLines}
					/>
				) : null}
			</div>
		</section>
	);
}

function canStageLines(group: string) {
	return group === "Unstaged" || group === "Untracked";
}

function canUnstageLines(group: string) {
	return group === "Staged";
}

function workingOldText(line: CommitFileDiffLine) {
	if (line.oldText) {
		return line.oldText;
	}

	return line.changeType === "Deleted" ? line.text : "";
}

function workingNewText(line: CommitFileDiffLine) {
	if (line.newText) {
		return line.newText;
	}

	return line.changeType === "Inserted" ? line.text : "";
}

function isSameDiffLine(left: CommitFileDiffLine, right: CommitFileDiffLine) {
	return (
		left.changeType === right.changeType &&
		left.oldLineNumber === right.oldLineNumber &&
		left.newLineNumber === right.newLineNumber &&
		left.oldText === right.oldText &&
		left.newText === right.newText &&
		left.text === right.text
	);
}

function isChangedLine(line: CommitFileDiffLine) {
	return (
		line.changeType === "Inserted" ||
		line.changeType === "Deleted" ||
		line.changeType === "Modified"
	);
}

function ModeButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: React.ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<button
			aria-label={label}
			className={`inline-flex h-7 items-center gap-1 rounded px-2 text-xs text-muted-foreground hover:bg-accent hover:text-accent-foreground ${
				isActive ? "bg-accent font-semibold text-accent-foreground" : ""
			}`}
			onClick={onClick}
			title={label}
			type="button"
		>
			{icon}
			<span>{label}</span>
		</button>
	);
}

function LoadingDiff() {
	return (
		<div className="space-y-2 p-4">
			{Array.from({ length: 16 }, (_, index) => (
				<div
					className="h-5 animate-pulse rounded bg-muted"
					key={`working-loading-diff-row-${index}`}
					style={{ width: `${index % 3 === 0 ? 72 : 96}%` }}
				/>
			))}
		</div>
	);
}

function folderPrefix(path: string) {
	const lastSlash = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
	return lastSlash >= 0 ? path.slice(0, lastSlash + 1) : "";
}

function fileName(path: string) {
	const lastSlash = Math.max(path.lastIndexOf("/"), path.lastIndexOf("\\"));
	return lastSlash >= 0 ? path.slice(lastSlash + 1) : path;
}
