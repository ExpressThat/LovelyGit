import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import {
	ArchiveRestore,
	FileSearch,
	GitBranch,
	PackageOpen,
	Trash2,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { RepositoryStashItem, StashAction } from "@/generated/types";

const ROW_HEIGHT = 112;
const ROW_GAP = 8;
const INITIAL_WINDOW = 10;
const stashDateFormatter = new Intl.DateTimeFormat(undefined, {
	dateStyle: "medium",
	timeStyle: "short",
});

export type StashListActions = {
	busyAction: StashAction | null;
	onApply: (stash: RepositoryStashItem) => void;
	onBranch: (stash: RepositoryStashItem) => void;
	onDrop: (stash: RepositoryStashItem) => void;
	onInspect: (stash: RepositoryStashItem) => void;
	onPop: (stash: RepositoryStashItem) => void;
};

export function StashVirtualList({
	stashes,
	...actions
}: StashListActions & { stashes: RepositoryStashItem[] }) {
	const scrollRef = useRef<HTMLElement>(null);
	const virtualizer = useVirtualizer({
		count: stashes.length,
		estimateSize: () => ROW_HEIGHT,
		gap: ROW_GAP,
		getScrollElement: () => scrollRef.current,
		overscan: 4,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{ length: Math.min(stashes.length, INITIAL_WINDOW) },
					(_, index) => ({ index, start: index * (ROW_HEIGHT + ROW_GAP) }),
				);

	return (
		<section
			aria-label="Saved stashes"
			className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
			ref={scrollRef}
		>
			<div
				className="relative"
				style={{ height: `${virtualizer.getTotalSize()}px` }}
			>
				{visibleRows.map((row) => {
					const stash = stashes[row.index];
					return stash ? (
						<div
							className="absolute left-0 right-0 top-0"
							data-index={row.index}
							key={`${stash.selector}:${stash.commitHash}`}
							ref={virtualizer.measureElement}
							style={{ transform: `translateY(${row.start}px)` }}
						>
							<StashRow actions={actions} stash={stash} />
						</div>
					) : null;
				})}
			</div>
		</section>
	);
}

function StashRow({
	actions,
	stash,
}: {
	actions: StashListActions;
	stash: RepositoryStashItem;
}) {
	const isBusy = actions.busyAction !== null;
	return (
		<article className="grid gap-2 rounded-lg border bg-card p-3">
			<div className="flex min-w-0 items-start gap-3">
				<div className="mt-0.5 rounded-md bg-muted p-1.5 text-muted-foreground">
					<PackageOpen aria-hidden="true" className="size-4" />
				</div>
				<div className="min-w-0 flex-1">
					<div className="flex items-center gap-2">
						<span className="font-mono text-xs text-primary">
							{stash.selector}
						</span>
						<span className="font-mono text-[10px] text-muted-foreground">
							{stash.commitHash.slice(0, 7)}
						</span>
					</div>
					<p className="mt-1 break-words text-sm">
						{stash.message || "Stashed working changes"}
					</p>
					{stash.createdAtUnixSeconds ? (
						<time className="mt-1 block text-xs text-muted-foreground">
							{formatStashDate(stash.createdAtUnixSeconds)}
						</time>
					) : null}
				</div>
			</div>
			<div className="flex justify-end gap-1">
				<Button
					disabled={isBusy}
					onClick={() => actions.onInspect(stash)}
					size="sm"
					variant="ghost"
				>
					<FileSearch aria-hidden="true" /> Inspect
				</Button>
				<Button
					disabled={isBusy}
					onClick={() => actions.onBranch(stash)}
					size="sm"
					variant="ghost"
				>
					<GitBranch aria-hidden="true" /> Branch
				</Button>
				<Button
					disabled={isBusy}
					onClick={() => actions.onApply(stash)}
					size="sm"
					variant="ghost"
				>
					<ArchiveRestore aria-hidden="true" /> Apply
				</Button>
				<Button
					disabled={isBusy}
					onClick={() => actions.onPop(stash)}
					size="sm"
					variant="ghost"
				>
					<PackageOpen aria-hidden="true" /> Pop
				</Button>
				<Button
					aria-label={`Delete ${stash.selector}`}
					disabled={isBusy}
					onClick={() => actions.onDrop(stash)}
					size="icon-sm"
					title={`Delete ${stash.selector}`}
					variant="ghost"
				>
					<Trash2 aria-hidden="true" />
				</Button>
			</div>
		</article>
	);
}

function formatStashDate(value: number) {
	const date = new Date(value * 1000);
	return Number.isNaN(date.getTime())
		? "Unknown date"
		: stashDateFormatter.format(date);
}
