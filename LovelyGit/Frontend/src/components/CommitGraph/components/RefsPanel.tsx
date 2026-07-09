import { ChevronLeft, ChevronRight, Search, X } from "lucide-react";
import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { CommitGraphRow, RepositoryRefsResponse } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import {
	buildRefPanelSections,
	buildRefPanelSummary,
	filterRefPanelSections,
	type RefPanelSummary,
} from "./RefsPanelData";
import { RefsPanelList } from "./RefsPanelList";
import { filterWorktrees, WorktreeSection } from "./WorktreeSection";

export function RefsPanel({
	currentBranchName,
	onSelectCommit,
	remotePrefixes,
	repositoryRefs,
	rows,
}: {
	currentBranchName: string | null;
	onSelectCommit: (row: CommitGraphRow) => void;
	remotePrefixes: string[];
	repositoryRefs: RepositoryRefsResponse | null;
	rows: Array<CommitGraphRow | null>;
}) {
	const isOpen = useSetting("CommitGraphRefsPanelOpen");
	const [query, setQuery] = useState("");
	const sections = useMemo(
		() =>
			buildRefPanelSections({
				currentBranchName:
					repositoryRefs?.currentBranchName ?? currentBranchName,
				refs: repositoryRefs?.refs,
				remotePrefixes: repositoryRefs?.remotePrefixes ?? remotePrefixes,
				rows,
			}),
		[currentBranchName, remotePrefixes, repositoryRefs, rows],
	);
	const filteredSections = useMemo(
		() => filterRefPanelSections(sections, query),
		[sections, query],
	);
	const summary = useMemo(
		() =>
			buildRefPanelSummary({
				currentBranchName:
					repositoryRefs?.currentBranchName ?? currentBranchName,
				remotePrefixes: repositoryRefs?.remotePrefixes ?? remotePrefixes,
				sections,
			}),
		[currentBranchName, remotePrefixes, repositoryRefs, sections],
	);
	const filteredWorktrees = useMemo(
		() => filterWorktrees(repositoryRefs?.worktrees ?? [], query),
		[query, repositoryRefs?.worktrees],
	);

	if (!isOpen) {
		return (
			<aside className="flex w-9 shrink-0 justify-center border-r bg-sidebar py-2">
				<Button
					aria-label="Show refs panel"
					onClick={() => void setSetting("CommitGraphRefsPanelOpen", true)}
					size="icon-sm"
					title="Show refs panel"
					variant="ghost"
				>
					<ChevronRight aria-hidden="true" />
				</Button>
			</aside>
		);
	}

	return (
		<aside className="flex w-56 shrink-0 flex-col border-r bg-sidebar text-sidebar-foreground">
			<header className="flex h-[34px] items-center justify-between border-b px-2">
				<h2 className="text-xs font-semibold uppercase text-muted-foreground">
					Refs
				</h2>
				<Button
					aria-label="Hide refs panel"
					onClick={() => void setSetting("CommitGraphRefsPanelOpen", false)}
					size="icon-xs"
					title="Hide refs panel"
					variant="ghost"
				>
					<ChevronLeft aria-hidden="true" />
				</Button>
			</header>
			<div className="border-b p-2">
				<RefsSummary
					summary={summary}
					worktreeCount={repositoryRefs?.worktrees.length ?? 0}
				/>
				<div className="relative">
					<Search
						aria-hidden="true"
						className="pointer-events-none absolute left-2 top-1/2 -translate-y-1/2 text-muted-foreground"
						size={14}
					/>
					<Input
						aria-label="Filter refs"
						className="h-7 rounded-md pl-7 pr-7 text-xs"
						onChange={(event) => setQuery(event.currentTarget.value)}
						onInput={(event) => setQuery(event.currentTarget.value)}
						placeholder="Filter refs"
						value={query}
					/>
					{query ? (
						<Button
							aria-label="Clear ref filter"
							className="absolute right-1 top-1/2 size-5 -translate-y-1/2"
							onClick={() => setQuery("")}
							size="icon-xs"
							title="Clear ref filter"
							type="button"
							variant="ghost"
						>
							<X aria-hidden="true" size={12} />
						</Button>
					) : null}
				</div>
			</div>
			{filteredSections.length > 0 || filteredWorktrees.length > 0 ? (
				<>
					<div className="custom-scrollbar max-h-36 overflow-y-auto p-2 pb-0">
						<WorktreeSection
							query={query}
							worktrees={repositoryRefs?.worktrees ?? []}
						/>
					</div>
					<RefsPanelList
						onSelectCommit={onSelectCommit}
						sections={filteredSections}
					/>
				</>
			) : (
				<div className="p-2">
					<RefsEmptyState hasQuery={query.trim().length > 0} />
				</div>
			)}
		</aside>
	);
}

function RefsSummary({
	summary,
	worktreeCount,
}: {
	summary: RefPanelSummary;
	worktreeCount: number;
}) {
	return (
		<div className="mb-2 space-y-1.5">
			<div className="flex min-w-0 items-center justify-between gap-2">
				<div className="min-w-0 text-[10px] font-semibold uppercase text-muted-foreground">
					Current
				</div>
				<div
					className="min-w-0 truncate text-right text-xs font-medium text-sidebar-foreground"
					title={summary.currentBranchLabel ?? "Detached HEAD"}
				>
					{summary.currentBranchLabel ?? "Detached HEAD"}
				</div>
			</div>
			<div className="grid grid-cols-3 gap-1 text-center">
				<SummaryMetric label="Local" value={summary.localBranchCount} />
				<SummaryMetric label="Tags" value={summary.tagCount} />
				<SummaryMetric label="Worktrees" value={worktreeCount} />
			</div>
			<div className="grid grid-cols-3 gap-1 text-center">
				<SummaryMetric label="Remote" value={summary.remoteBranchCount} />
				<SummaryMetric label="Stashes" value={summary.stashCount} />
				<SummaryMetric label="Refs" value={summary.totalRefCount} />
			</div>
		</div>
	);
}

function SummaryMetric({ label, value }: { label: string; value: number }) {
	return (
		<div className="rounded border bg-sidebar px-1 py-1">
			<div className="font-mono text-xs leading-none text-sidebar-foreground">
				{value.toLocaleString()}
			</div>
			<div className="mt-0.5 truncate text-[9px] leading-none text-muted-foreground">
				{label}
			</div>
		</div>
	);
}

function RefsEmptyState({ hasQuery }: { hasQuery: boolean }) {
	return (
		<p className="px-1 py-2 text-xs text-muted-foreground">
			{hasQuery
				? "No refs match this filter."
				: "Branches, tags, stashes, and worktrees appear here."}
		</p>
	);
}
