import { useVirtualizer } from "@tanstack/react-virtual";
import { type CSSProperties, useRef } from "react";
import { File, FilePlus2, FileX2 } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { StashInspectionFile } from "./useStashInspection";

const ROW_HEIGHT = 48;
const MAX_BOOTSTRAP_ROWS = 10;
const VIRTUALIZE_AFTER = MAX_BOOTSTRAP_ROWS;

type StashInspectionFileListProps = {
	files: StashInspectionFile[];
	onSelect: (file: StashInspectionFile) => void;
	selected: StashInspectionFile | null;
};

export function StashInspectionFileList({
	files,
	onSelect,
	selected,
}: StashInspectionFileListProps) {
	if (files.length <= VIRTUALIZE_AFTER) {
		return (
			<section
				aria-label="Stashed files"
				className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
				data-stashed-files-list="ordinary"
			>
				{files.map((item) =>
					stashFileButton(
						item,
						onSelect,
						selected,
						`${item.source}:${item.file.status}:${item.file.path}`,
					),
				)}
			</section>
		);
	}

	return (
		<VirtualStashInspectionFileList
			files={files}
			onSelect={onSelect}
			selected={selected}
		/>
	);
}

function VirtualStashInspectionFileList({
	files,
	onSelect,
	selected,
}: StashInspectionFileListProps) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 4,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const virtualRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(MAX_BOOTSTRAP_ROWS, files.length) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	return (
		<section
			aria-label="Stashed files"
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			data-stashed-files-list="virtual"
			ref={scrollRef}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{virtualRows.map((row) => {
					const item = files[row.index];
					if (!item) return null;
					return stashFileButton(item, onSelect, selected, undefined, {
						transform: `translateY(${row.start}px)`,
					});
				})}
			</div>
		</section>
	);
}

function stashFileButton(
	item: StashInspectionFile,
	onSelect: (file: StashInspectionFile) => void,
	selected: StashInspectionFile | null,
	key?: string,
	style?: CSSProperties,
) {
	return (
		<Button
			aria-current={selected === item ? "true" : undefined}
			className={`${style ? "absolute left-0 right-0 " : ""}h-12 w-full justify-start gap-2 rounded-none border-b px-3 font-normal aria-[current=true]:bg-accent`}
			key={key ?? `${item.source}:${item.file.status}:${item.file.path}`}
			onClick={() => onSelect(item)}
			style={style}
			title={item.file.path}
			variant="ghost"
		>
			<FileStatus status={item.file.status} />
			<span className="min-w-0 flex-1 text-left">
				<span className="block truncate font-mono text-xs">
					{item.file.path}
				</span>
				<span className="block text-[10px] uppercase text-muted-foreground">
					{item.source} · {item.file.status}
				</span>
			</span>
			<span className="shrink-0 font-mono text-[10px]">
				<span className="text-emerald-600 dark:text-emerald-400">
					+{item.file.additions}
				</span>{" "}
				<span className="text-red-600 dark:text-red-400">
					-{item.file.deletions}
				</span>
			</span>
		</Button>
	);
}

function FileStatus({ status }: { status: string }) {
	const Icon =
		status === "Added" ? FilePlus2 : status === "Deleted" ? FileX2 : File;
	const color =
		status === "Added"
			? "text-emerald-600 dark:text-emerald-400"
			: status === "Deleted"
				? "text-red-600 dark:text-red-400"
				: "text-amber-600 dark:text-amber-400";
	return <Icon aria-hidden="true" className={`shrink-0 ${color}`} size={15} />;
}
