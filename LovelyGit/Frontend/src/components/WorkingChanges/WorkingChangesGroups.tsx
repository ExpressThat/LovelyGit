import { useVirtualizer } from "@tanstack/react-virtual";
import { useMemo, useRef } from "react";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { ChangedFileRow } from "./WorkingChangedFileRow";
import {
	buildWorkingChangesGroups,
	buildWorkingChangesVirtualRows,
	type WorkingChangesGroupConfig,
} from "./WorkingChangesGroupRows";
import { fileKey } from "./WorkingChangesPanelParts";

const HEADER_HEIGHT = 32;
const ROW_HEIGHT = 38;
const MAX_VISIBLE_ROWS = 12;

export function getWorkingChangesVirtualListHeight(rowCount: number) {
	return Math.min(rowCount, MAX_VISIBLE_ROWS) * ROW_HEIGHT + HEADER_HEIGHT;
}

export function WorkingChangesGroups({
	isBusy,
	onDiscardSelected,
	onIndexCommand,
	onSelectFile,
	onToggleSelected,
	selectedKeys,
	stagedFiles,
	workingFiles,
	unmergedFiles,
}: {
	isBusy: boolean;
	onDiscardSelected: (files: WorkingTreeChangedFile[]) => void;
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected: (file: WorkingTreeChangedFile) => void;
	selectedKeys: Set<string>;
	stagedFiles: WorkingTreeChangedFile[];
	workingFiles: WorkingTreeChangedFile[];
	unmergedFiles: WorkingTreeChangedFile[];
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const groups = useMemo(
		() =>
			buildWorkingChangesGroups({
				isBusy,
				onDiscardSelected,
				onIndexCommand,
				onToggleSelected,
				selectedKeys,
				stagedFiles,
				unmergedFiles,
				workingFiles,
			}),
		[
			isBusy,
			onDiscardSelected,
			onIndexCommand,
			onToggleSelected,
			selectedKeys,
			stagedFiles,
			unmergedFiles,
			workingFiles,
		],
	);
	const rows = useMemo(() => buildWorkingChangesVirtualRows(groups), [groups]);
	const virtualizer = useVirtualizer({
		count: rows.length,
		estimateSize: (index) =>
			rows[index]?.type === "header" ? HEADER_HEIGHT : ROW_HEIGHT,
		getScrollElement: () => parentRef.current,
		overscan: 8,
	});
	const virtualRows = virtualizer.getVirtualItems();

	if (rows.length === 0) {
		return null;
	}

	return (
		<div
			className="custom-scrollbar overflow-y-auto"
			ref={parentRef}
			style={{ height: `${getWorkingChangesVirtualListHeight(rows.length)}px` }}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{virtualRows.map((virtualRow) => {
					const row = rows[virtualRow.index];
					if (!row) {
						return null;
					}

					return (
						<div
							className="absolute left-0 right-0"
							data-index={virtualRow.index}
							key={row.id}
							ref={virtualizer.measureElement}
							style={{ transform: `translateY(${virtualRow.start}px)` }}
						>
							{row.type === "header" ? (
								<GroupHeader group={row.group} />
							) : (
								<ChangedFileRow
									file={row.file}
									hideGroupLabel={row.group.hideGroupLabel}
									isBusy={isBusy}
									isSelected={selectedKeys.has(fileKey(row.file))}
									onAction={
										row.group.onFileAction
											? () => row.group.onFileAction?.(row.file)
											: undefined
									}
									onSelect={() => onSelectFile(row.file)}
									onToggleSelected={
										row.group.onToggleSelected
											? () => row.group.onToggleSelected?.(row.file)
											: undefined
									}
									rowActionLabel={singleFileActionLabel(row.group.title)}
								/>
							)}
						</div>
					);
				})}
			</div>
		</div>
	);
}

function GroupHeader({ group }: { group: WorkingChangesGroupConfig }) {
	return (
		<div className="flex h-8 items-center justify-between gap-2">
			<h3 className="text-[10px] font-semibold uppercase text-muted-foreground">
				{group.title} ({group.files.length})
			</h3>
			<div className="flex items-center gap-1">
				{group.onDestructiveAction && group.destructiveActionLabel ? (
					<button
						className="inline-flex h-6 items-center rounded px-2 text-[10px] font-semibold uppercase text-destructive hover:bg-destructive/10 disabled:pointer-events-none disabled:opacity-40"
						disabled={group.isDestructiveActionDisabled}
						onClick={group.onDestructiveAction}
						type="button"
					>
						{group.destructiveActionLabel}
					</button>
				) : null}
				{group.onAction && group.actionLabel ? (
					<button
						className="inline-flex h-6 items-center rounded px-2 text-[10px] font-semibold uppercase text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						disabled={group.isActionDisabled}
						onClick={group.onAction}
						type="button"
					>
						{group.actionLabel}
					</button>
				) : null}
			</div>
		</div>
	);
}

function singleFileActionLabel(groupTitle: string) {
	if (groupTitle === "Staged") {
		return "Unstage";
	}

	return groupTitle === "Changes" ? "Stage" : undefined;
}
