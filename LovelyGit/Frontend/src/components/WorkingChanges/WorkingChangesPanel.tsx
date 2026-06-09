import { File, FilePlus2, FileQuestion, FileX2, GitPullRequestArrow, RefreshCw } from "lucide-react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/ExpressThat.LovelyGit.Services.Git.WorkingTree.Models";

export function WorkingChangesPanel({
	changes,
	error,
	isLoading,
	onRefresh,
	onSelectFile,
}: {
	changes: WorkingTreeChangesResponse | null;
	error: string | null;
	isLoading: boolean;
	onRefresh: () => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
}) {
	const workingFiles = changes ? [...changes.unstaged, ...changes.untracked] : [];

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

			{error ? (
				<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
					{error}
				</div>
			) : null}

			{changes && changes.totalCount === 0 ? (
				<div className="rounded-md border bg-card p-4 text-sm text-muted-foreground">
					No working changes.
				</div>
			) : null}

			{changes ? (
				<>
					<ChangeGroup title="Staged" files={changes.staged} onSelectFile={onSelectFile} />
					<ChangeGroup
						hideGroupLabel
						title="Changes"
						files={workingFiles}
						onSelectFile={onSelectFile}
					/>
					<ChangeGroup title="Unmerged" files={changes.unmerged} onSelectFile={onSelectFile} />
				</>
			) : null}
		</div>
	);
}

function ChangeGroup({
	files,
	hideGroupLabel = false,
	onSelectFile,
	title,
}: {
	files: WorkingTreeChangedFile[];
	hideGroupLabel?: boolean;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	title: string;
}) {
	if (files.length === 0) {
		return null;
	}

	return (
		<section className="space-y-1">
			<h3 className="text-[10px] font-semibold uppercase text-muted-foreground">
				{title} ({files.length})
			</h3>
			<div className="border-y">
				{files.map((file) => (
					<ChangedFileRow
						file={file}
						hideGroupLabel={hideGroupLabel}
						key={`${file.group}:${file.status}:${file.path}`}
						onSelect={() => onSelectFile(file)}
					/>
				))}
			</div>
		</section>
	);
}

function ChangedFileRow({
	file,
	hideGroupLabel = false,
	onSelect,
}: {
	file: WorkingTreeChangedFile;
	hideGroupLabel?: boolean;
	onSelect: () => void;
}) {
	const Icon = statusIcon(file.status, file.group);

	return (
		<button
			className="flex min-h-9 w-full items-center gap-2 border-b py-1.5 text-left hover:bg-accent/60 last:border-b-0"
			onClick={onSelect}
			type="button"
		>
			<Icon aria-hidden="true" className={statusColor(file.status, file.group)} size={15} />
			<div className="min-w-0 flex-1">
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
		</button>
	);
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
