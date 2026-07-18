import { useVirtualizer } from "@tanstack/react-virtual";
import { type CSSProperties, useRef } from "react";
import type { PatchFilePreview } from "@/generated/types";

const INITIAL_WINDOW = 9;
const ROW_HEIGHT = 32;
const VIRTUAL_FILE_THRESHOLD = 30;

export function PatchFileList({ files }: { files: PatchFilePreview[] }) {
	if (files.length <= VIRTUAL_FILE_THRESHOLD) {
		return (
			<ul
				aria-label="Patch files"
				className="custom-scrollbar max-h-64 overflow-y-auto rounded-lg border bg-background"
			>
				{files.map((file) => (
					<PatchFileRow file={file} key={file.path} />
				))}
			</ul>
		);
	}

	return <VirtualPatchFileList files={files} />;
}

function VirtualPatchFileList({ files }: { files: PatchFilePreview[] }) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 4,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(INITIAL_WINDOW, files.length) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);

	return (
		<div
			className="custom-scrollbar h-64 overflow-y-auto rounded-lg border bg-background"
			ref={scrollRef}
		>
			<ul
				aria-label="Patch files"
				className="relative"
				data-patch-file-list="virtual"
				style={{ height: virtualizer.getTotalSize() }}
			>
				{visibleRows.map((row) => {
					const file = files[row.index];
					return file ? (
						<PatchFileRow
							className="absolute inset-x-0"
							file={file}
							key={`${row.index}:${file.path}`}
							style={{ transform: `translateY(${row.start}px)` }}
						/>
					) : null;
				})}
			</ul>
		</div>
	);
}

function PatchFileRow({
	className = "",
	file,
	style,
}: {
	className?: string;
	file: PatchFilePreview;
	style?: CSSProperties;
}) {
	return (
		<li
			className={`flex h-8 items-center gap-2 border-b px-3 last:border-b-0 ${className}`}
			data-patch-file
			style={style}
		>
			<span
				className="min-w-0 flex-1 truncate font-mono text-xs"
				title={file.path}
			>
				{file.path}
			</span>
			<span className="text-emerald-500 text-xs">+{file.additions}</span>
			<span className="text-rose-500 text-xs">−{file.deletions}</span>
		</li>
	);
}
