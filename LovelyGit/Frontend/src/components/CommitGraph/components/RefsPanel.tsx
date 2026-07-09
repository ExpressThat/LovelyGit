import {
	ChevronLeft,
	ChevronRight,
	GitBranchPlus,
	Search,
	X,
} from "lucide-react";
import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { CommitGraphRow, RepositoryRefsResponse } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { CreateBranchDialog } from "./BranchActionDialogs";
import { buildRefPanelSections, filterRefPanelSections } from "./RefsPanelData";
import { RefsPanelList } from "./RefsPanelList";
import { filterWorktrees, WorktreeSection } from "./WorktreeSection";

export function RefsPanel({
	currentBranchName,
	onRepositoryMutation,
	onSelectCommit,
	remotePrefixes,
	repositoryRefs,
	repositoryId,
	rows,
}: {
	currentBranchName: string | null;
	onRepositoryMutation: () => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	remotePrefixes: string[];
	repositoryRefs: RepositoryRefsResponse | null;
	repositoryId: string | null;
	rows: Array<CommitGraphRow | null>;
}) {
	const isOpen = useSetting("CommitGraphRefsPanelOpen");
	const [query, setQuery] = useState("");
	const [createAtHeadOpen, setCreateAtHeadOpen] = useState(false);
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
				<div className="flex items-center gap-1">
					<Button
						aria-label="Create branch at HEAD"
						disabled={!repositoryId}
						onClick={() => setCreateAtHeadOpen(true)}
						size="icon-xs"
						title="Create branch at HEAD"
						variant="ghost"
					>
						<GitBranchPlus aria-hidden="true" />
					</Button>
					<Button
						aria-label="Hide refs panel"
						onClick={() => void setSetting("CommitGraphRefsPanelOpen", false)}
						size="icon-xs"
						title="Hide refs panel"
						variant="ghost"
					>
						<ChevronLeft aria-hidden="true" />
					</Button>
				</div>
			</header>
			<CreateBranchDialog
				onOpenChange={setCreateAtHeadOpen}
				onSuccess={onRepositoryMutation}
				open={createAtHeadOpen}
				repositoryId={repositoryId}
				startPoint="HEAD"
			/>
			<div className="border-b p-2">
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
						onRepositoryMutation={onRepositoryMutation}
						onSelectCommit={onSelectCommit}
						repositoryId={repositoryId}
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

function RefsEmptyState({ hasQuery }: { hasQuery: boolean }) {
	return (
		<p className="px-1 py-2 text-xs text-muted-foreground">
			{hasQuery
				? "No refs match this filter."
				: "Branches, tags, stashes, and worktrees appear here."}
		</p>
	);
}
