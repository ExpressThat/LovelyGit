import { useMemo, useState } from "react";
import {
	ChevronLeft,
	ChevronRight,
	Search,
	X,
} from "@/components/icons/lovelyIcons";
import { HorizontalPanelHandle } from "@/components/layout/HorizontalPanelHandle";
import { useHorizontalPanelResize } from "@/components/layout/useHorizontalPanelResize";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { CommitGraphRow, RepositoryRefsResponse } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import type { BranchAction } from "./BranchContextMenu";
import { RefsAccordion } from "./RefsAccordion";
import { buildRefPanelSections, filterRefPanelSections } from "./RefsPanelData";
import type { TagAction } from "./TagContextMenu";
import { filterWorktrees } from "./WorktreeSection";

export function RefsPanel({
	branchMutationBusy,
	branchRemoteName,
	currentBranchName,
	onIntegrateBranch,
	onBranchAction,
	onCreateBranchFromTag,
	onSelectCommit,
	onTagAction,
	remotePrefixes,
	refRowsByHash,
	repositoryRefs,
	rows,
	tagMutationBusy,
	tagRemoteName,
	worktreeController,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onCreateBranchFromTag: (tagName: string, commitHash: string) => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	remotePrefixes: string[];
	refRowsByHash: ReadonlyMap<string, CommitGraphRow>;
	repositoryRefs: RepositoryRefsResponse | null;
	rows: Array<CommitGraphRow | null>;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
	worktreeController: WorktreeMutationController;
}) {
	const isOpen = useSetting("CommitGraphRefsPanelOpen");
	const savedWidth = useSetting("CommitGraphRefsPanelWidth");
	const resize = useHorizontalPanelResize({
		direction: 1,
		max: 520,
		min: 208,
		onCommit: (width) => void setSetting("CommitGraphRefsPanelWidth", width),
		width: savedWidth,
	});
	const [query, setQuery] = useState("");
	const sections = useMemo(
		() =>
			buildRefPanelSections({
				currentBranchName:
					repositoryRefs?.currentBranchName ?? currentBranchName,
				refs: repositoryRefs?.refs,
				remotePrefixes: repositoryRefs?.remotePrefixes ?? remotePrefixes,
				refRowsByHash,
				rows,
			}),
		[currentBranchName, refRowsByHash, remotePrefixes, repositoryRefs, rows],
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
		<aside
			className="relative flex shrink-0 flex-col border-r bg-sidebar text-sidebar-foreground"
			style={{ width: resize.width }}
		>
			<HorizontalPanelHandle
				label="Resize refs panel"
				onPointerDown={resize.startResize}
				onResizeBy={resize.resizeBy}
				side="right"
			/>
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
				<RefsAccordion
					actions={{
						branchMutationBusy,
						branchRemoteName,
						currentBranchName,
						onBranchAction,
						onCreateBranchFromTag,
						onIntegrateBranch,
						onSelectCommit,
						onTagAction,
						tagMutationBusy,
						tagRemoteName,
					}}
					controller={worktreeController}
					sections={filteredSections}
					worktrees={filteredWorktrees}
				/>
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
