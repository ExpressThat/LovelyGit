import { FileClock } from "lucide-react";
import { motion } from "motion/react";
import { formatDate, shortHash } from "@/components/CommitGraph/utils/format";
import type { FileHistoryResult } from "@/generated/types";

export function FileHistoryResultRow({
	index,
	isSelected,
	onSelect,
	onSelectIndex,
	result,
}: {
	index: number;
	isSelected: boolean;
	onSelect: () => void;
	onSelectIndex: () => void;
	result: FileHistoryResult;
}) {
	return (
		<button
			aria-label={`Open commit ${shortHash(result.hash)}: ${result.subject}`}
			className="relative grid w-full grid-cols-[auto_minmax(0,1fr)_auto] gap-3 overflow-hidden rounded-lg border border-transparent px-3 py-2.5 text-left outline-none hover:border-border focus-visible:ring-2 focus-visible:ring-ring"
			id={`file-history-result-${index}`}
			onClick={onSelect}
			onFocus={onSelectIndex}
			onMouseEnter={onSelectIndex}
			type="button"
		>
			{isSelected ? (
				<motion.span
					className="absolute inset-0 bg-accent"
					layoutId="file-history-selection"
					transition={{ type: "spring", stiffness: 470, damping: 38 }}
				/>
			) : null}
			<span className="relative mt-0.5 rounded-md bg-primary/12 p-1.5 text-primary">
				<FileClock aria-hidden="true" className="size-4" />
			</span>
			<span className="relative min-w-0">
				<span className="flex items-center gap-2">
					<strong className="truncate text-sm">{result.subject}</strong>
					<span className="rounded bg-secondary px-1.5 py-0.5 text-[10px] uppercase text-secondary-foreground">
						{result.changeKind}
					</span>
				</span>
				<span className="mt-0.5 block truncate font-mono text-[11px] text-muted-foreground">
					{result.previousPath
						? `${result.previousPath} → ${result.path}`
						: result.path}
				</span>
				<span className="mt-1 block truncate text-[11px] text-muted-foreground">
					{result.author} · {formatDate(result.date)}
				</span>
			</span>
			<span className="relative pt-0.5 font-mono text-xs text-muted-foreground">
				{shortHash(result.hash)}
			</span>
		</button>
	);
}
