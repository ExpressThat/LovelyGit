import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import { GitCommitHorizontal } from "@/components/icons/lovelyIcons";
import type { BranchComparisonCommit } from "@/generated/types";
import { shortHash } from "../utils/format";

const INITIAL_WINDOW = 12;
const ROW_HEIGHT = 46;
const VIRTUAL_COMMIT_THRESHOLD = 30;

export function BranchComparisonCommitList({
	commits,
}: {
	commits: BranchComparisonCommit[];
}) {
	if (commits.length <= VIRTUAL_COMMIT_THRESHOLD) {
		return (
			<ul className="grid gap-1.5 pr-1">
				{commits.map((commit) => (
					<CommitRow commit={commit} key={commit.hash} />
				))}
			</ul>
		);
	}
	return <VirtualCommitList commits={commits} />;
}

function VirtualCommitList({ commits }: { commits: BranchComparisonCommit[] }) {
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: commits.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => scrollRef.current,
		overscan: 4,
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
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			data-branch-comparison-commits="virtual"
			ref={scrollRef}
		>
			<ul
				className="relative pr-1"
				style={{ height: virtualizer.getTotalSize() }}
			>
				{visibleRows.map((row) => {
					const commit = commits[row.index];
					return commit ? (
						<li
							className="absolute inset-x-0 pb-1.5"
							key={commit.hash}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<CommitContent commit={commit} />
						</li>
					) : null;
				})}
			</ul>
		</div>
	);
}

function CommitRow({ commit }: { commit: BranchComparisonCommit }) {
	return (
		<li>
			<CommitContent commit={commit} />
		</li>
	);
}

function CommitContent({ commit }: { commit: BranchComparisonCommit }) {
	return (
		<div className="grid grid-cols-[auto_minmax(0,1fr)_auto] items-center gap-2 rounded-lg border bg-card px-2.5 py-2">
			<GitCommitHorizontal className="size-4 text-primary" />
			<div className="min-w-0">
				<div className="truncate font-medium text-xs">{commit.subject}</div>
				<div className="truncate text-[10px] text-muted-foreground">
					{commit.authorName}
				</div>
			</div>
			<code className="text-[10px] text-muted-foreground">
				{shortHash(commit.hash)}
			</code>
		</div>
	);
}
