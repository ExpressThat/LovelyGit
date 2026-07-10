import {
	File,
	FilePlus2,
	FileQuestion,
	FileX2,
	GitPullRequestArrow,
} from "lucide-react";
import { Checkbox } from "@/components/ui/checkbox";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { FileHistoryContextMenu } from "../FileHistory/FileHistoryContextMenu";

export function ChangedFileRow({
	file,
	hideGroupLabel = false,
	isBusy = false,
	isSelected = false,
	onAction,
	onOpenHistory,
	onSelect,
	onToggleSelected,
	rowActionLabel,
}: {
	file: WorkingTreeChangedFile;
	hideGroupLabel?: boolean;
	isBusy?: boolean;
	isSelected?: boolean;
	onAction?: () => void;
	onOpenHistory: () => void;
	onSelect: () => void;
	onToggleSelected?: () => void;
	rowActionLabel?: string;
}) {
	const Icon = statusIcon(file.status, file.group);
	const handleFileClick = () => {
		onSelect();
	};

	return (
		<div className="group/file-row flex min-h-9 w-full items-center gap-2 border-b text-left hover:bg-accent/60 last:border-b-0">
			{onToggleSelected ? (
				<div className="ml-1 inline-flex size-5 shrink-0 items-center justify-center">
					<Checkbox
						aria-label={`Select ${file.path}`}
						checked={isSelected}
						onClick={(event) => event.stopPropagation()}
						onCheckedChange={onToggleSelected}
					/>
				</div>
			) : null}
			<FileHistoryContextMenu onOpen={onOpenHistory} path={file.path}>
				<button
					className="flex min-w-0 flex-1 items-center gap-2 py-1.5 text-left"
					onClick={handleFileClick}
					type="button"
				>
					<Icon
						aria-hidden="true"
						className={statusColor(file.status, file.group)}
						size={15}
					/>
					<div className="min-w-0 flex-1 text-left">
						<div
							className="truncate font-mono text-xs text-foreground"
							title={file.path}
						>
							{file.path}
						</div>
						<div className="text-[10px] uppercase text-muted-foreground">
							{hideGroupLabel ? file.status : `${file.group} ${file.status}`}
							{file.isBinary ? " binary" : ""}
						</div>
					</div>
					<div className="shrink-0 font-mono text-xs">
						<span className="text-emerald-600 dark:text-emerald-400">
							+{file.additions}
						</span>{" "}
						<span className="text-red-600 dark:text-red-400">
							-{file.deletions}
						</span>
					</div>
				</button>
			</FileHistoryContextMenu>
			{onAction && rowActionLabel ? (
				<button
					className="mr-1 hidden h-6 shrink-0 items-center rounded border bg-background px-2 text-[10px] font-semibold uppercase text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40 group-hover/file-row:inline-flex"
					disabled={isBusy}
					onClick={(event) => {
						event.stopPropagation();
						onAction();
					}}
					type="button"
				>
					{rowActionLabel}
				</button>
			) : null}
		</div>
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
