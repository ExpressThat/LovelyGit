import { Columns2, Rows3, WrapText, X } from "lucide-react";
import { useEffect, useState } from "react";
import type { CommitFileDiffResponse } from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import type { WorkingTreeChangedFile } from "@/generated/ExpressThat.LovelyGit.Services.Git.WorkingTree.Models";
import { sendRequestWithResponse } from "@/lib/registerSignalR";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { DiffContent } from "../CommitFileDiff/CommitFileDiffView";

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
	const wrapLines = useSetting("CommitDiffWrapLines");
	const [state, setState] = useState<DiffState>({ status: "loading" });

	useEffect(() => {
		let isActive = true;
		setState({ status: "loading" });

		sendRequestWithResponse({
			commandType: "GetWorkingTreeFileDiff",
			arguments: {
				path: file.path,
				group: file.group,
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
							: "Failed to load working file diff.",
				});
			});

		return () => {
			isActive = false;
		};
	}, [file.group, file.path, repositoryId, viewMode]);

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
				{state.status === "loaded" ? <DiffContent diff={state.diff} wrapLines={wrapLines} /> : null}
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
