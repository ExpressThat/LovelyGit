import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import type { CommitGraphRow } from "@/generated/types";
import { shortHash } from "../utils/format";

const INITIAL_WINDOW = 8;
const ROW_HEIGHT = 41;
const VIRTUAL_COMMIT_THRESHOLD = 30;

export function CommitOperationCommitList({
	commits,
}: {
	commits: CommitGraphRow[];
}) {
	if (commits.length <= VIRTUAL_COMMIT_THRESHOLD) {
		return (
			<div className="custom-scrollbar max-h-52 overflow-y-auto rounded-lg border bg-card">
				{commits.map((commit, index) => (
					<CommitRow commit={commit} index={index} key={commit.commit.hash} />
				))}
			</div>
		);
	}
	return <VirtualCommitList commits={commits} />;
}

function VirtualCommitList({ commits }: { commits: CommitGraphRow[] }) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: commits.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 3,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(INITIAL_WINDOW, commits.length) },
					(_, index) => ({ index, start: index * ROW_HEIGHT }),
				);
	return (
		<div
			className="custom-scrollbar h-52 overflow-y-auto rounded-lg border bg-card"
			data-commit-operation-list="virtual"
			ref={scrollRef}
		>
			<div className="relative" style={{ height: virtualizer.getTotalSize() }}>
				{visibleRows.map((row) => {
					const commit = commits[row.index];
					return commit ? (
						<div
							className="absolute inset-x-0"
							key={commit.commit.hash}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<CommitRow commit={commit} index={row.index} />
						</div>
					) : null;
				})}
			</div>
		</div>
	);
}

function CommitRow({
	commit,
	index,
}: {
	commit: CommitGraphRow;
	index: number;
}) {
	return (
		<div
			className="grid h-[41px] grid-cols-[2rem_1fr_auto] items-center gap-2 border-b px-3 py-2 last:border-b-0"
			data-commit-operation-row
		>
			<span className="text-center text-muted-foreground text-xs">
				{index + 1}
			</span>
			<span className="truncate font-medium text-sm">
				{commit.commit.message.split(/\r?\n/, 1)[0] || "(no commit message)"}
			</span>
			<span className="font-mono text-muted-foreground text-xs">
				{shortHash(commit.commit.hash)}
			</span>
		</div>
	);
}
