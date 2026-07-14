import type { ReactNode } from "react";
import {
	MinusSquare,
	RefreshCw,
	SquareCheckBig,
} from "@/components/icons/lovelyIcons";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";

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
	actions,
	isLoading,
	onRefresh,
	totalCount,
}: {
	actions?: ReactNode;
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
			<div className="flex items-center gap-2">
				{actions}
				<button
					aria-label="Refresh working changes"
					className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-50"
					disabled={isLoading}
					onClick={() => {
						void Promise.resolve(onRefresh()).catch(() => undefined);
					}}
					type="button"
				>
					<RefreshCw
						aria-hidden="true"
						className={isLoading ? "animate-spin" : ""}
						size={14}
					/>
				</button>
			</div>
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

export function selectedStashPaths(
	changes: WorkingTreeChangesResponse,
	selectedKeys: Set<string>,
) {
	return uniquePaths(
		selectedFiles(
			[...changes.staged, ...changes.unstaged, ...changes.untracked],
			selectedKeys,
		),
	);
}

export function fileKey(file: WorkingTreeChangedFile) {
	return `${file.group}:${file.status}:${file.path}`;
}
