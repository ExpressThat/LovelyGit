import { MinusSquare, RefreshCw, SquareCheckBig } from "lucide-react";
import type { ReactNode } from "react";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { ChangedFileRow } from "./WorkingChangedFileRow";

export function WorkingChangesSkeleton() {
	return (
		<div className="space-y-3 p-4">
			<div className="h-4 w-36 animate-pulse rounded bg-muted" />
			<div className="h-24 animate-pulse rounded bg-muted" />
			<div className="h-32 animate-pulse rounded bg-muted" />
		</div>
	);
}

export function WorkingChangesHeader({
	isLoading,
	onRefresh,
	totalCount,
}: {
	isLoading: boolean;
	onRefresh: () => Promise<void> | void;
	totalCount: number;
}) {
	return (
		<div className="flex items-center justify-between gap-3">
			<div>
				<div className="font-semibold text-foreground">
					{totalCount} changed files
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
				<RefreshCw
					aria-hidden="true"
					className={isLoading ? "animate-spin" : ""}
					size={14}
				/>
			</button>
		</div>
	);
}

export function BulkIndexActions({
	canStage,
	canUnstage,
	isBusy,
	onStageAll,
	onUnstageAll,
}: {
	canStage: boolean;
	canUnstage: boolean;
	isBusy: boolean;
	onStageAll: () => void;
	onUnstageAll: () => void;
}) {
	return (
		<div className="flex flex-wrap gap-2">
			<ActionButton
				disabled={isBusy || !canStage}
				icon={<SquareCheckBig aria-hidden="true" size={14} />}
				label="Stage all"
				onClick={onStageAll}
			/>
			<ActionButton
				disabled={isBusy || !canUnstage}
				icon={<MinusSquare aria-hidden="true" size={14} />}
				label="Unstage all"
				onClick={onUnstageAll}
			/>
		</div>
	);
}

export function ChangeGroup({
	actionLabel,
	destructiveActionLabel,
	files,
	hideGroupLabel = false,
	isActionDisabled = true,
	isBusy = false,
	isDestructiveActionDisabled = true,
	onAction,
	onDestructiveAction,
	onFileDestructiveAction,
	onFileAction,
	onSelectFile,
	onToggleSelected,
	repositoryId,
	selectedKeys,
	title,
}: {
	actionLabel?: string;
	destructiveActionLabel?: string;
	files: WorkingTreeChangedFile[];
	hideGroupLabel?: boolean;
	isActionDisabled?: boolean;
	isBusy?: boolean;
	isDestructiveActionDisabled?: boolean;
	onAction?: () => void;
	onDestructiveAction?: () => void;
	onFileDestructiveAction?: (file: WorkingTreeChangedFile) => void;
	onFileAction?: (file: WorkingTreeChangedFile) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected?: (file: WorkingTreeChangedFile) => void;
	repositoryId: string;
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
				<div className="flex items-center gap-1">
					{onDestructiveAction && destructiveActionLabel ? (
						<button
							className="inline-flex h-6 items-center rounded px-2 text-[10px] font-semibold uppercase text-destructive hover:bg-destructive/10 disabled:pointer-events-none disabled:opacity-40"
							disabled={isDestructiveActionDisabled}
							onClick={onDestructiveAction}
							type="button"
						>
							{destructiveActionLabel}
						</button>
					) : null}
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
			</div>
			<div className="border-y">
				{files.map((file) => (
					<ChangedFileRow
						file={file}
						hideGroupLabel={hideGroupLabel}
						isBusy={isBusy}
						isSelected={selectedKeys?.has(fileKey(file)) ?? false}
						key={`${file.group}:${file.status}:${file.path}`}
						onAction={onFileAction ? () => onFileAction(file) : undefined}
						onDestructiveAction={
							onFileDestructiveAction
								? () => onFileDestructiveAction(file)
								: undefined
						}
						repositoryId={repositoryId}
						rowActionLabel={singleFileActionLabel(title)}
						onSelect={() => onSelectFile(file)}
						onToggleSelected={
							onToggleSelected ? () => onToggleSelected(file) : undefined
						}
					/>
				))}
			</div>
		</section>
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

export function selectedFiles(
	files: WorkingTreeChangedFile[],
	selectedKeys: Set<string>,
) {
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
