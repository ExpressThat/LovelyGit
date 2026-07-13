import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import { File, FilePlus2, FileX2 } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { StashInspectionFile } from "./useStashInspection";

const ROW_HEIGHT = 48;

export function StashInspectionFileList({
	files,
	onSelect,
	selected,
}: {
	files: StashInspectionFile[];
	onSelect: (file: StashInspectionFile) => void;
	selected: StashInspectionFile | null;
}) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 8,
	});

	return (
		<section
			aria-label="Stashed files"
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			ref={scrollRef}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{virtualizer.getVirtualItems().map((row) => {
					const item = files[row.index];
					if (!item) return null;
					return (
						<Button
							aria-current={selected === item ? "true" : undefined}
							className="absolute left-0 right-0 h-12 justify-start gap-2 rounded-none border-b px-3 font-normal aria-[current=true]:bg-accent"
							key={`${item.source}:${item.file.status}:${item.file.path}`}
							onClick={() => onSelect(item)}
							style={{ transform: `translateY(${row.start}px)` }}
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
				})}
			</div>
		</section>
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
