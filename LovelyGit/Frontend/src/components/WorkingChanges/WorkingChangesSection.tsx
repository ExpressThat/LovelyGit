import { useVirtualizer } from "@tanstack/react-virtual";
import type { ReactNode } from "react";
import { useRef } from "react";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { ChangedFileRow } from "./WorkingChangedFileRow";
import { fileKey } from "./WorkingChangesPanelParts";

const ROW_HEIGHT = 38;

export function WorkingChangesSection({
	actions,
	emptyMessage,
	files,
	isBusy,
	onIndexCommand,
	onOpenFileBlame,
	onOpenFileHistory,
	onIgnorePath,
	onSelectFile,
	onToggleSelected,
	selectedKeys,
	title,
}: {
	actions: ReactNode;
	emptyMessage: string;
	files: WorkingTreeChangedFile[];
	isBusy: boolean;
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void;
	onOpenFileBlame: (file: WorkingTreeChangedFile) => void;
	onOpenFileHistory: (file: WorkingTreeChangedFile) => void;
	onIgnorePath: (
		file: WorkingTreeChangedFile,
		target: "Local" | "Shared",
	) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected: (file: WorkingTreeChangedFile) => void;
	selectedKeys: Set<string>;
	title: string;
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => parentRef.current,
		overscan: 8,
	});
	const virtualRows = virtualizer.getVirtualItems();

	return (
		<section className="flex min-h-0 flex-1 flex-col rounded-md border bg-card">
			<header className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b px-3 py-2">
				<h3 className="text-sm font-semibold text-foreground">
					{title} ({files.length})
				</h3>
				<div className="flex flex-wrap justify-end gap-2">{actions}</div>
			</header>
			<div
				className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
				ref={parentRef}
			>
				{files.length === 0 ? (
					<div className="p-4 text-sm text-muted-foreground">
						{emptyMessage}
					</div>
				) : (
					<div
						className="relative"
						style={{ height: `${virtualizer.getTotalSize()}px` }}
					>
						{virtualRows.map((virtualRow) => {
							const file = files[virtualRow.index];
							if (!file) return null;
							const selectable = canSelect(file);
							const actionLabel = singleFileActionLabel(file);
							return (
								<div
									className="absolute left-0 right-0"
									data-index={virtualRow.index}
									key={fileKey(file)}
									ref={virtualizer.measureElement}
									style={{ transform: `translateY(${virtualRow.start}px)` }}
								>
									<ChangedFileRow
										file={file}
										isBusy={isBusy}
										isSelected={selectable && selectedKeys.has(fileKey(file))}
										onAction={
											actionLabel
												? () => runSingleFileAction(file, onIndexCommand)
												: undefined
										}
										onSelect={() => onSelectFile(file)}
										onOpenHistory={() => onOpenFileHistory(file)}
										onIgnore={(target) => onIgnorePath(file, target)}
										onOpenBlame={() => onOpenFileBlame(file)}
										onToggleSelected={
											selectable ? () => onToggleSelected(file) : undefined
										}
										rowActionLabel={actionLabel}
									/>
								</div>
							);
						})}
					</div>
				)}
			</div>
		</section>
	);
}

function canSelect(file: WorkingTreeChangedFile) {
	return (
		file.group === "Staged" ||
		file.group === "Unstaged" ||
		file.group === "Untracked"
	);
}

function runSingleFileAction(
	file: WorkingTreeChangedFile,
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void,
) {
	if (file.group === "Staged") {
		onIndexCommand("UnstageWorkingTreeFiles", [file], false);
	} else if (file.group === "Unstaged" || file.group === "Untracked") {
		onIndexCommand("StageWorkingTreeFiles", [file], false);
	}
}

function singleFileActionLabel(file: WorkingTreeChangedFile) {
	if (file.group === "Staged") return "Unstage";
	if (file.group === "Unstaged" || file.group === "Untracked") return "Stage";
	return undefined;
}
