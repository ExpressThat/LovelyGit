import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import { File, FilePlus2, FileX2 } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { CommitChangedFile } from "@/generated/types";
import { FileHistoryContextMenu } from "../FileHistory/FileHistoryContextMenu";
import { CommitChangedFileStats } from "./CommitChangedFileStats";

const ROW_HEIGHT = 42;
const MAX_VISIBLE_ROWS = 10;
const VIRTUALIZE_AFTER = MAX_VISIBLE_ROWS;

type ChangedFilesListProps = {
	files: CommitChangedFile[];
	hasLineStats: boolean;
	onOpenBlame: (file: CommitChangedFile) => void;
	onOpenHistory: (file: CommitChangedFile) => void;
	onSelectFile: (file: CommitChangedFile) => void;
};

export function getChangedFileListHeight(fileCount: number) {
	return Math.min(fileCount, MAX_VISIBLE_ROWS) * ROW_HEIGHT;
}

export function CommitDetailsChangedFilesList({
	files,
	hasLineStats,
	onOpenBlame,
	onOpenHistory,
	onSelectFile,
}: ChangedFilesListProps) {
	if (files.length <= VIRTUALIZE_AFTER) {
		return (
			<section
				aria-label="Changed files"
				className="custom-scrollbar overflow-y-auto"
				data-changed-files-list="ordinary"
				style={{ height: `${getChangedFileListHeight(files.length)}px` }}
			>
				{files.map((file) =>
					changedFileItem(
						file,
						hasLineStats,
						onOpenBlame,
						onOpenHistory,
						onSelectFile,
						`${file.status}:${file.path}`,
					),
				)}
			</section>
		);
	}

	return (
		<VirtualChangedFilesList
			files={files}
			hasLineStats={hasLineStats}
			onOpenBlame={onOpenBlame}
			onOpenHistory={onOpenHistory}
			onSelectFile={onSelectFile}
		/>
	);
}

function VirtualChangedFilesList({
	files,
	hasLineStats,
	onOpenBlame,
	onOpenHistory,
	onSelectFile,
}: ChangedFilesListProps) {
	const parentRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => parentRef.current,
		overscan: 4,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const virtualRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(MAX_VISIBLE_ROWS, files.length) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);
	const listHeight = getChangedFileListHeight(files.length);

	return (
		<section
			aria-label="Changed files"
			className="custom-scrollbar overflow-y-auto"
			data-changed-files-list="virtual"
			ref={parentRef}
			style={{ height: `${listHeight}px` }}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{virtualRows.map((virtualRow) => {
					const file = files[virtualRow.index];
					if (!file) {
						return null;
					}

					return (
						<div
							className="absolute left-0 right-0"
							key={`${file.status}:${file.path}`}
							style={{ transform: `translateY(${virtualRow.start}px)` }}
						>
							{changedFileItem(
								file,
								hasLineStats,
								onOpenBlame,
								onOpenHistory,
								onSelectFile,
							)}
						</div>
					);
				})}
			</div>
		</section>
	);
}

function changedFileItem(
	file: CommitChangedFile,
	hasLineStats: boolean,
	onOpenBlame: (file: CommitChangedFile) => void,
	onOpenHistory: (file: CommitChangedFile) => void,
	onSelectFile: (file: CommitChangedFile) => void,
	key?: string,
) {
	return (
		<FileHistoryContextMenu
			key={key}
			onOpenBlame={() => onOpenBlame(file)}
			onOpen={() => onOpenHistory(file)}
			path={file.path}
		>
			<ChangedFileRow
				file={file}
				hasLineStats={hasLineStats}
				onSelect={() => onSelectFile(file)}
			/>
		</FileHistoryContextMenu>
	);
}

function ChangedFileRow({
	file,
	hasLineStats,
	onSelect,
}: {
	file: CommitChangedFile;
	hasLineStats: boolean;
	onSelect: () => void;
}) {
	const Icon = statusIcon(file.status);

	return (
		<Button
			className="h-[42px] w-full justify-start gap-2 rounded-none border-b px-0 py-1.5 font-normal hover:bg-accent/60"
			onClick={onSelect}
			title={file.path}
			type="button"
			variant="ghost"
		>
			<Icon aria-hidden="true" className={statusColor(file.status)} size={15} />
			<div className="min-w-0 flex-1 text-left">
				<div className="truncate font-mono text-xs text-foreground">
					{file.path}
				</div>
				<div className="text-[10px] uppercase text-muted-foreground">
					{file.status}
					{file.isBinary ? " binary" : ""}
				</div>
			</div>
			<CommitChangedFileStats file={file} visible={hasLineStats} />
		</Button>
	);
}

function statusIcon(status: string) {
	switch (status) {
		case "Added":
			return FilePlus2;
		case "Deleted":
			return FileX2;
		default:
			return File;
	}
}

function statusColor(status: string) {
	switch (status) {
		case "Added":
			return "shrink-0 text-emerald-600 dark:text-emerald-400";
		case "Deleted":
			return "shrink-0 text-red-600 dark:text-red-400";
		case "Modified":
		case "TypeChanged":
			return "shrink-0 text-amber-600 dark:text-amber-400";
		default:
			return "shrink-0 text-muted-foreground";
	}
}
