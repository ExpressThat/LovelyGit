import { useVirtualizer } from "@tanstack/react-virtual";
import { useRef } from "react";
import { toast } from "sonner";
import {
	AlertTriangle,
	History,
	Search,
	X,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type { GitReflogEntry } from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";
import { useReflog } from "../hooks/useReflog";
import { ReflogEntryRow } from "./ReflogEntryRow";

const REFLOG_ROW_HEIGHT = 56;
const INITIAL_REFLOG_WINDOW = 10;

export function ReflogDialog({
	branchName,
	onClose,
	onCreateBranch,
	onReset,
	repositoryId,
}: {
	branchName: string;
	onClose: () => void;
	onCreateBranch: (entry: GitReflogEntry) => void;
	onReset: (entry: GitReflogEntry) => void;
	repositoryId: string | null;
}) {
	const { entries, error, filteredEntries, isLoading, query, setQuery } =
		useReflog(repositoryId, branchName);
	const scrollRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: filteredEntries.length,
		estimateSize: () => REFLOG_ROW_HEIGHT,
		gap: 6,
		getScrollElement: () => scrollRef.current,
		overscan: 5,
	});
	const measuredRows = virtualizer.getVirtualItems();
	const visibleRows =
		measuredRows.length > 0
			? measuredRows
			: Array.from(
					{
						length: Math.min(filteredEntries.length, INITIAL_REFLOG_WINDOW),
					},
					(_, index) => ({ index, start: index * (REFLOG_ROW_HEIGHT + 6) }),
				);
	const reduceMotion = useReducedMotion();
	const copyHash = async (hash: string) => {
		try {
			await navigator.clipboard.writeText(hash);
			toast.success("Copied commit hash");
		} catch {
			toast.error("Could not copy the commit hash.");
		}
	};
	return (
		<Dialog open onOpenChange={(open) => !open && onClose()}>
			<DialogContent className="sm:max-w-3xl">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<History aria-hidden="true" className="size-5 text-primary" />
						Reflog for {branchName}
					</DialogTitle>
					<DialogDescription>
						Recover commits after resets, rebases, amendments, and branch moves.
						Use the entry actions or right-click any row.
					</DialogDescription>
				</DialogHeader>
				<div className="grid min-h-0 gap-3 py-3">
					<div className="flex items-center gap-2">
						<div className="relative min-w-0 flex-1">
							<Search
								aria-hidden="true"
								className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
							/>
							<Input
								aria-label="Filter reflog"
								className="pl-8 pr-8"
								onChange={(event) => setQuery(event.currentTarget.value)}
								onInput={(event) => setQuery(event.currentTarget.value)}
								placeholder="Filter by action, actor, selector, or hash"
								value={query}
							/>
							{query ? (
								<Button
									aria-label="Clear reflog filter"
									className="absolute right-1.5 top-1/2 -translate-y-1/2"
									onClick={() => setQuery("")}
									size="icon-xs"
									variant="ghost"
								>
									<X aria-hidden="true" />
								</Button>
							) : null}
						</div>
						<span className="shrink-0 rounded-md bg-muted px-2 py-1 text-muted-foreground text-xs">
							{filteredEntries.length} of {entries.length}
						</span>
					</div>
					<div
						className="custom-scrollbar h-[min(58vh,520px)] overflow-y-auto pr-1"
						ref={scrollRef}
					>
						{isLoading ? (
							<ReflogLoading />
						) : error ? (
							<div className="flex items-start gap-2 rounded-lg border border-destructive/30 bg-destructive/8 p-3 text-destructive text-sm">
								<AlertTriangle
									aria-hidden="true"
									className="mt-0.5 size-4 shrink-0"
								/>
								{error}
							</div>
						) : filteredEntries.length === 0 ? (
							<p className="rounded-lg border border-dashed bg-muted/20 p-6 text-center text-muted-foreground text-sm">
								{entries.length === 0
									? "No reflog entries are available for this branch."
									: "No reflog entries match this filter."}
							</p>
						) : (
							<motion.div
								animate={{ opacity: 1, y: 0 }}
								className="relative"
								initial={{ opacity: 0, y: reduceMotion ? 0 : 4 }}
								style={{ height: `${virtualizer.getTotalSize()}px` }}
							>
								{visibleRows.map((virtualRow) => {
									const entry = filteredEntries[virtualRow.index];
									return entry ? (
										<div
											className="absolute left-0 right-0"
											key={`${entry.selector}:${entry.newHash}`}
											style={{
												transform: `translateY(${virtualRow.start}px)`,
											}}
										>
											<ReflogEntryRow
												entry={entry}
												onCopy={(hash) => void copyHash(hash)}
												onCreateBranch={onCreateBranch}
												onReset={onReset}
											/>
										</div>
									) : null;
								})}
							</motion.div>
						)}
					</div>
				</div>
			</DialogContent>
		</Dialog>
	);
}

function ReflogLoading() {
	return (
		<div aria-label="Loading reflog" className="grid gap-1.5" role="status">
			{["first", "second", "third", "fourth", "fifth", "sixth"].map((key) => (
				<div
					className="h-14 animate-pulse rounded-lg border bg-card"
					key={key}
				/>
			))}
		</div>
	);
}
