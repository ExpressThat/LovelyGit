import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import { File, FilePlus2, FileX2 } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { CommitChangedFile } from "@/generated/types";
import { FileHistoryContextMenu } from "../FileHistory/FileHistoryContextMenu";

const ROW_HEIGHT = 42;
const MAX_VISIBLE_ROWS = 10;

export function getChangedFileListHeight(fileCount: number) {
	return Math.min(fileCount, MAX_VISIBLE_ROWS) * ROW_HEIGHT;
}

export function CommitDetailsChangedFilesList({
	files,
	onOpenBlame,
	onOpenHistory,
	onSelectFile,
}: {
	files: CommitChangedFile[];
	onOpenBlame: (file: CommitChangedFile) => void;
	onOpenHistory: (file: CommitChangedFile) => void;
	onSelectFile: (file: CommitChangedFile) => void;
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => parentRef.current,
		overscan: 6,
	});
	const virtualRows = virtualizer.getVirtualItems();
	const listHeight = getChangedFileListHeight(files.length);

	return (
		<section
			aria-label="Changed files"
			className="custom-scrollbar overflow-y-auto"
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
							<FileHistoryContextMenu
								onOpenBlame={() => onOpenBlame(file)}
								onOpen={() => onOpenHistory(file)}
								path={file.path}
							>
								<ChangedFileRow
									file={file}
									onSelect={() => onSelectFile(file)}
								/>
							</FileHistoryContextMenu>
						</div>
					);
				})}
			</div>
		</section>
	);
}

function ChangedFileRow({
	file,
	onSelect,
}: {
	file: CommitChangedFile;
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
			<div className="shrink-0 font-mono text-xs">
				<span className="text-emerald-600 dark:text-emerald-400">
					+{file.additions}
				</span>{" "}
				<span className="text-red-600 dark:text-red-400">
					-{file.deletions}
				</span>
			</div>
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
