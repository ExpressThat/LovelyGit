import { Check, File, FilePlus2, FileQuestion, FileX2, GitPullRequestArrow } from "lucide-react";
import type { ReactNode } from "react";
import type { WorkingTreeChangedFile } from "@/generated/types";

export function ChangeGroup({
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

export function ActionButton({
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

export function selectedFiles(files: WorkingTreeChangedFile[], selectedKeys: Set<string>) {
	return files.filter((file) => selectedKeys.has(fileKey(file)));
}

export function uniquePaths(files: WorkingTreeChangedFile[]) {
	return [...new Set(files.map((file) => file.path))];
}

export function fileKey(file: WorkingTreeChangedFile) {
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
