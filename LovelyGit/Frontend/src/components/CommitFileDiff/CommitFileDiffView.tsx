import { Columns2, FileText, ListCollapse, Minus, Plus, Rows3, WrapText, X } from "lucide-react";
import { useEffect, useState } from "react";
import type {
	CommitChangedFile,
	CommitFileDiffResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { DiffContent, LoadingDiff } from "./DiffContent";

type DiffState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "loaded"; diff: CommitFileDiffResponse };

export function CommitFileDiffView({
	commitHash,
	file,
	onClose,
	repositoryId,
}: {
	commitHash: string;
	file: CommitChangedFile;
	onClose: () => void;
	repositoryId: string;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const [state, setState] = useState<DiffState>({ status: "loading" });

	useEffect(() => {
		let isActive = true;
		setState({ status: "loading" });

		sendRequestWithResponse({
			commandType: "GetCommitFileDiff",
			arguments: {
				commitHash,
				path: file.path,
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
	}, [commitHash, file.path, repositoryId, viewMode]);

	const updateViewMode = (nextViewMode: typeof viewMode) => {
		void setSetting("CommitDiffViewMode", nextViewMode);
	};

	const updateWrapLines = (nextWrapLines: boolean) => {
		void setSetting("CommitDiffWrapLines", nextWrapLines);
	};

	const updateLineDisplayMode = (nextLineDisplayMode: typeof lineDisplayMode) => {
		void setSetting("CommitDiffLineDisplayMode", nextLineDisplayMode);
	};

	const handleClose = () => {
		setState({ status: "loading" });
		onClose();
	};

	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<header className="shrink-0 border-b bg-popover text-popover-foreground">
				<div className="flex h-10 items-center gap-2 px-3">
					<div className="min-w-0 flex-1 truncate font-mono text-sm text-muted-foreground">
						<span>{folderPrefix(file.path)}</span>
						<span className="font-semibold text-foreground">
							{fileName(file.path)}
						</span>
					</div>
					<div className="hidden items-center gap-2 text-[10px] uppercase text-muted-foreground md:flex">
						<span>{file.status}</span>
						<span>
							+{file.additions} -{file.deletions}
						</span>
					</div>
					<button
						aria-label="Close diff"
						className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
						onClick={handleClose}
						type="button"
					>
						<X aria-hidden="true" size={16} />
					</button>
				</div>
				<div className="flex h-10 items-center justify-center border-t bg-card/60 px-3">
					<div className="inline-flex rounded-md border bg-background p-0.5">
						<ModeButton
							icon={<Columns2 aria-hidden="true" size={14} />}
							isActive={viewMode === "SideBySide"}
							label="Side by side"
							onClick={() => updateViewMode("SideBySide")}
						/>
						<ModeButton
							icon={<Rows3 aria-hidden="true" size={14} />}
							isActive={viewMode === "Combined"}
							label="Combined"
							onClick={() => updateViewMode("Combined")}
						/>
					</div>
					<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
						<ModeButton
							icon={<ListCollapse aria-hidden="true" size={14} />}
							isActive={lineDisplayMode === "Changes"}
							label="Changes"
							onClick={() => updateLineDisplayMode("Changes")}
						/>
						<ModeButton
							icon={<FileText aria-hidden="true" size={14} />}
							isActive={lineDisplayMode === "FullFile"}
							label="Full file"
							onClick={() => updateLineDisplayMode("FullFile")}
						/>
					</div>
					{lineDisplayMode === "Changes" ? (
						<ContextLinesControl contextLines={contextLines} />
					) : null}
					<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
						<ModeButton
							icon={<WrapText aria-hidden="true" size={14} />}
							isActive={wrapLines}
							label="Wrap lines"
							onClick={() => updateWrapLines(!wrapLines)}
						/>
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
					<DiffContent contextLines={contextLines} diff={state.diff} lineDisplayMode={lineDisplayMode} wrapLines={wrapLines} />
				) : null}
			</div>
		</section>
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

export function ContextLinesControl({ contextLines }: { contextLines: number }) {
	const updateContextLines = (value: number) => {
		const nextValue = Math.max(0, Math.min(99, Math.trunc(value)));
		void setSetting("CommitDiffContextLines", nextValue);
	};

	return (
		<div className="ml-2 inline-flex h-8 items-center gap-1 rounded-md border bg-background px-2 text-xs text-muted-foreground">
			<span>Context</span>
			<div className="ml-1 inline-flex h-6 overflow-hidden rounded border bg-card text-foreground">
				<button
					aria-label="Decrease context lines"
					className="inline-flex w-6 items-center justify-center hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-35"
					disabled={contextLines <= 0}
					onClick={() => updateContextLines(contextLines - 1)}
					type="button"
				>
					<Minus aria-hidden="true" size={12} />
				</button>
				<div className="flex min-w-7 items-center justify-center border-x px-1 font-mono text-xs">
					{contextLines}
				</div>
				<button
					aria-label="Increase context lines"
					className="inline-flex w-6 items-center justify-center hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-35"
					disabled={contextLines >= 99}
					onClick={() => updateContextLines(contextLines + 1)}
					type="button"
				>
					<Plus aria-hidden="true" size={12} />
				</button>
			</div>
		</div>
	);
}


function folderPrefix(path: string) {
	const slashIndex = path.lastIndexOf('/');
	return slashIndex >= 0 ? path.slice(0, slashIndex + 1) : '';
}

function fileName(path: string) {
	const slashIndex = path.lastIndexOf('/');
	return slashIndex >= 0 ? path.slice(slashIndex + 1) : path;
}
