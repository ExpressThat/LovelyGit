import { Check, File, FilePlus2, FileQuestion, FileX2, GitCommitHorizontal, GitPullRequestArrow, MinusSquare, RefreshCw, SquareCheckBig } from "lucide-react";
import { useEffect, useMemo, useState } from "react";
import type { ReactNode } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/ExpressThat.LovelyGit.Services.Git.WorkingTree.Models";
import { sendRequestWithResponse } from "@/lib/registerSignalR";

const COMMIT_TITLE_LIMIT = 72;

export function WorkingChangesPanel({
	changes,
	error,
	isLoading,
	onRefresh,
	onCommitSuccess,
	onSelectFile,
	repositoryId,
}: {
	changes: WorkingTreeChangesResponse | null;
	error: string | null;
	isLoading: boolean;
	onCommitSuccess: () => Promise<void> | void;
	onRefresh: () => Promise<void> | void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	repositoryId: string;
}) {
	const workingFiles = changes ? [...changes.unstaged, ...changes.untracked] : [];
	const [selectedKeys, setSelectedKeys] = useState<Set<string>>(() => new Set());
	const [actionError, setActionError] = useState<string | null>(null);
	const [isMutating, setIsMutating] = useState(false);
	const [commitTitle, setCommitTitle] = useState("");
	const [commitBody, setCommitBody] = useState("");
	const [isCommitting, setIsCommitting] = useState(false);
	const fileKeys = useMemo(
		() =>
			new Set(
				[
					...(changes?.staged ?? []),
					...(changes?.unstaged ?? []),
					...(changes?.untracked ?? []),
					...(changes?.unmerged ?? []),
				].map(fileKey),
			),
		[changes],
	);
	const selectedStagedFiles = selectedFiles(changes?.staged ?? [], selectedKeys);
	const selectedWorkingFiles = selectedFiles(workingFiles, selectedKeys);
	const isBusy = isMutating || isCommitting;

	useEffect(() => {
		setSelectedKeys((current) => {
			const next = new Set([...current].filter((key) => fileKeys.has(key)));
			return next.size === current.size ? current : next;
		});
	}, [fileKeys]);

	const runIndexCommand = async (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => {
		if (!includeAll && files.length === 0) {
			return;
		}

			setIsMutating(true);
			setActionError(null);
		try {
			await sendRequestWithResponse({
				commandType,
				arguments: {
					includeAll,
					paths: includeAll ? [] : uniquePaths(files),
					repositoryId,
				},
			});
			setSelectedKeys(new Set());
			await onRefresh();
		} catch (mutationError) {
			setActionError(
				mutationError instanceof Error
					? mutationError.message
					: "Failed to update the index.",
			);
		} finally {
			setIsMutating(false);
		}
	};

	const commitStagedChanges = async () => {
		if (!changes || changes.staged.length === 0 || commitTitle.trim().length === 0) {
			return;
		}

		setIsCommitting(true);
		setActionError(null);
		try {
			await sendRequestWithResponse({
				commandType: "CommitStagedChanges",
				arguments: {
					body: commitBody,
					repositoryId,
					title: commitTitle,
				},
			});
			setCommitTitle("");
			setCommitBody("");
			setSelectedKeys(new Set());
			await onCommitSuccess();
		} catch (commitError) {
			setActionError(
				commitError instanceof Error
					? commitError.message
					: "Failed to commit staged changes.",
			);
		} finally {
			setIsCommitting(false);
		}
	};

	const toggleSelected = (file: WorkingTreeChangedFile) => {
		const key = fileKey(file);
		setSelectedKeys((current) => {
			const next = new Set(current);
			if (next.has(key)) {
				next.delete(key);
			} else {
				next.add(key);
			}

			return next;
		});
	};

	if (!changes && isLoading) {
		return (
			<div className="space-y-3 p-4">
				<div className="h-4 w-36 animate-pulse rounded bg-muted" />
				<div className="h-24 animate-pulse rounded bg-muted" />
				<div className="h-32 animate-pulse rounded bg-muted" />
			</div>
		);
	}

	return (
		<div className="space-y-4 p-4 text-left text-sm">
			<div className="flex items-center justify-between gap-3">
				<div>
					<div className="font-semibold text-foreground">
						{changes?.totalCount ?? 0} changed files
					</div>
					<div className="text-xs text-muted-foreground">
						Staged, working tree, and unmerged
					</div>
				</div>
				<button
					aria-label="Refresh working changes"
					className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
					onClick={onRefresh}
					type="button"
				>
					<RefreshCw aria-hidden="true" className={isLoading ? "animate-spin" : ""} size={14} />
				</button>
			</div>

			{changes && changes.totalCount > 0 ? (
				<div className="flex flex-wrap gap-2">
					<ActionButton
						disabled={isBusy || workingFiles.length === 0}
						icon={<SquareCheckBig aria-hidden="true" size={14} />}
						label="Stage all"
						onClick={() => void runIndexCommand("StageWorkingTreeFiles", [], true)}
					/>
					<ActionButton
						disabled={isBusy || changes.staged.length === 0}
						icon={<MinusSquare aria-hidden="true" size={14} />}
						label="Unstage all"
						onClick={() => void runIndexCommand("UnstageWorkingTreeFiles", [], true)}
					/>
				</div>
			) : null}

			{error || actionError ? (
				<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
					{actionError ?? error}
				</div>
			) : null}

			{changes && changes.totalCount === 0 ? (
				<div className="rounded-md border bg-card p-4 text-sm text-muted-foreground">
					No working changes.
				</div>
			) : null}

			{changes && changes.staged.length > 0 ? (
				<section className="space-y-2 rounded-md border bg-card p-3">
					<div className="flex items-center gap-2 text-xs font-semibold text-foreground">
						<GitCommitHorizontal aria-hidden="true" size={15} />
						<span>Commit staged changes</span>
					</div>
					<div className="relative min-h-36 rounded-md border bg-background px-3 py-2 focus-within:border-sky-500">
						<div
							className={`absolute right-3 top-2 font-mono text-xs ${
								commitTitle.length > COMMIT_TITLE_LIMIT
									? "text-destructive"
									: "text-muted-foreground"
							}`}
						>
							{COMMIT_TITLE_LIMIT - commitTitle.length}
						</div>
						<label className="block pr-12">
							<span className="sr-only">Commit title</span>
							<input
								className="h-8 w-full border-0 bg-transparent p-0 text-lg text-foreground outline-none placeholder:text-muted-foreground"
								disabled={isBusy}
								onChange={(event) => setCommitTitle(event.target.value)}
								placeholder="Title"
								type="text"
								value={commitTitle}
							/>
						</label>
						<label className="block">
							<span className="sr-only">Commit body</span>
							<textarea
								className="min-h-24 w-full resize-none border-0 bg-transparent p-0 text-sm text-muted-foreground outline-none placeholder:text-muted-foreground"
								disabled={isBusy}
								onChange={(event) => setCommitBody(event.target.value)}
								placeholder="Body"
								value={commitBody}
							/>
						</label>
					</div>
					<div className="flex justify-end">
						<ActionButton
							disabled={isBusy || commitTitle.trim().length === 0}
							icon={
								<GitCommitHorizontal
									aria-hidden="true"
									className={isCommitting ? "animate-pulse" : undefined}
									size={14}
								/>
							}
							label={isCommitting ? "Committing" : "Commit"}
							onClick={() => void commitStagedChanges()}
						/>
					</div>
				</section>
			) : null}

			{changes ? (
				<>
					<ChangeGroup
						actionLabel={
							selectedStagedFiles.length > 0
								? `Unstage selected (${selectedStagedFiles.length})`
								: "Unstage selected"
						}
						files={changes.staged}
						isActionDisabled={isBusy || selectedStagedFiles.length === 0}
						onAction={() =>
							void runIndexCommand(
								"UnstageWorkingTreeFiles",
								selectedStagedFiles,
								false,
							)
						}
						onFileAction={(file) =>
							void runIndexCommand("UnstageWorkingTreeFiles", [file], false)
						}
						onSelectFile={onSelectFile}
						onToggleSelected={toggleSelected}
						selectedKeys={selectedKeys}
						title="Staged"
					/>
					<ChangeGroup
						actionLabel={
							selectedWorkingFiles.length > 0
								? `Stage selected (${selectedWorkingFiles.length})`
								: "Stage selected"
						}
						hideGroupLabel
						files={workingFiles}
						isActionDisabled={isBusy || selectedWorkingFiles.length === 0}
						onAction={() =>
							void runIndexCommand(
								"StageWorkingTreeFiles",
								selectedWorkingFiles,
								false,
							)
						}
						onFileAction={(file) =>
							void runIndexCommand("StageWorkingTreeFiles", [file], false)
						}
						onSelectFile={onSelectFile}
						onToggleSelected={toggleSelected}
						selectedKeys={selectedKeys}
						title="Changes"
					/>
					<ChangeGroup title="Unmerged" files={changes.unmerged} onSelectFile={onSelectFile} />
				</>
			) : null}
		</div>
	);
}

function ChangeGroup({
	actionLabel,
	files,
	hideGroupLabel = false,
	isActionDisabled = true,
	onAction,
	onFileAction,
	onSelectFile,
	onToggleSelected,
	selectedKeys,
	title,
}: {
	actionLabel?: string;
	files: WorkingTreeChangedFile[];
	hideGroupLabel?: boolean;
	isActionDisabled?: boolean;
	onAction?: () => void;
	onFileAction?: (file: WorkingTreeChangedFile) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected?: (file: WorkingTreeChangedFile) => void;
	selectedKeys?: Set<string>;
	title: string;
}) {
	if (files.length === 0) {
		return null;
	}

	return (
		<section className="space-y-1">
			<div className="flex items-center justify-between gap-2">
				<h3 className="text-[10px] font-semibold uppercase text-muted-foreground">
					{title} ({files.length})
				</h3>
				{onAction && actionLabel ? (
					<button
						className="inline-flex h-6 items-center rounded px-2 text-[10px] font-semibold uppercase text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						disabled={isActionDisabled}
						onClick={onAction}
						type="button"
					>
						{actionLabel}
					</button>
				) : null}
			</div>
			<div className="border-y">
				{files.map((file) => (
					<ChangedFileRow
						file={file}
						hideGroupLabel={hideGroupLabel}
						isSelected={selectedKeys?.has(fileKey(file)) ?? false}
						key={`${file.group}:${file.status}:${file.path}`}
						onAction={onFileAction ? () => onFileAction(file) : undefined}
						rowActionLabel={singleFileActionLabel(title)}
						onSelect={() => onSelectFile(file)}
						onToggleSelected={onToggleSelected ? () => onToggleSelected(file) : undefined}
					/>
				))}
			</div>
		</section>
	);
}

function ChangedFileRow({
	file,
	hideGroupLabel = false,
	isSelected = false,
	onAction,
	onSelect,
	onToggleSelected,
	rowActionLabel,
}: {
	file: WorkingTreeChangedFile;
	hideGroupLabel?: boolean;
	isSelected?: boolean;
	onAction?: () => void;
	onSelect: () => void;
	onToggleSelected?: () => void;
	rowActionLabel?: string;
}) {
	const Icon = statusIcon(file.status, file.group);
	const handleRowClick = () => {
		onToggleSelected?.();
		onSelect();
	};

	return (
		<button
			className="group/file-row flex min-h-9 w-full items-center gap-2 border-b py-1.5 text-left hover:bg-accent/60 last:border-b-0"
			onClick={handleRowClick}
			type="button"
		>
			{onToggleSelected ? (
				<span
					aria-checked={isSelected}
					aria-label={`Select ${file.path}`}
					className={`inline-flex size-4 shrink-0 items-center justify-center rounded border transition-colors ${
						isSelected
							? "border-sky-400 bg-sky-500 text-white shadow-[0_0_0_1px_rgba(56,189,248,0.35)]"
							: "border-muted-foreground/60 bg-background text-transparent"
					}`}
					onClick={(event) => {
						event.stopPropagation();
						onToggleSelected();
					}}
					role="checkbox"
				>
					<Check aria-hidden="true" size={12} strokeWidth={3} />
				</span>
			) : null}
			<Icon aria-hidden="true" className={statusColor(file.status, file.group)} size={15} />
			<div className="min-w-0 flex-1 text-left">
				<div className="truncate font-mono text-xs text-foreground" title={file.path}>
					{file.path}
				</div>
				<div className="text-[10px] uppercase text-muted-foreground">
					{hideGroupLabel ? file.status : `${file.group} ${file.status}`}
					{file.isBinary ? " binary" : ""}
				</div>
			</div>
			<div className="shrink-0 font-mono text-xs">
				<span className="text-emerald-600 dark:text-emerald-400">+{file.additions}</span>{" "}
				<span className="text-red-600 dark:text-red-400">-{file.deletions}</span>
			</div>
			{onAction && rowActionLabel ? (
				<span
					className="mr-1 hidden h-6 shrink-0 items-center rounded border bg-background px-2 text-[10px] font-semibold uppercase text-muted-foreground hover:bg-accent hover:text-accent-foreground group-hover/file-row:inline-flex"
					onClick={(event) => {
						event.stopPropagation();
						onAction();
					}}
					onKeyDown={(event) => {
						if (event.key !== "Enter" && event.key !== " ") {
							return;
						}

						event.preventDefault();
						event.stopPropagation();
						onAction();
					}}
					role="button"
					tabIndex={0}
				>
					{rowActionLabel}
				</span>
			) : null}
		</button>
	);
}

function ActionButton({
	disabled,
	icon,
	label,
	onClick,
}: {
	disabled: boolean;
	icon: ReactNode;
	label: string;
	onClick: () => void;
}) {
	return (
		<button
			className="inline-flex h-7 items-center gap-1.5 rounded-md border bg-background px-2 text-xs font-medium text-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
			disabled={disabled}
			onClick={onClick}
			type="button"
		>
			{icon}
			<span>{label}</span>
		</button>
	);
}

function selectedFiles(files: WorkingTreeChangedFile[], selectedKeys: Set<string>) {
	return files.filter((file) => selectedKeys.has(fileKey(file)));
}

function uniquePaths(files: WorkingTreeChangedFile[]) {
	return [...new Set(files.map((file) => file.path))];
}

function fileKey(file: WorkingTreeChangedFile) {
	return `${file.group}:${file.status}:${file.path}`;
}

function singleFileActionLabel(groupTitle: string) {
	if (groupTitle === "Staged") {
		return "Unstage";
	}

	if (groupTitle === "Changes") {
		return "Stage";
	}

	return undefined;
}


function statusIcon(status: string, group: string) {
	if (group === "Unmerged") {
		return GitPullRequestArrow;
	}
	switch (status) {
		case "Added":
			return FilePlus2;
		case "Deleted":
			return FileX2;
		case "Unmerged":
			return FileQuestion;
		default:
			return File;
	}
}

function statusColor(status: string, group: string) {
	if (group === "Unmerged") {
		return "shrink-0 text-violet-600 dark:text-violet-400";
	}
	switch (status) {
		case "Added":
			return "shrink-0 text-emerald-600 dark:text-emerald-400";
		case "Deleted":
			return "shrink-0 text-red-600 dark:text-red-400";
		default:
			return "shrink-0 text-amber-600 dark:text-amber-400";
	}
}
