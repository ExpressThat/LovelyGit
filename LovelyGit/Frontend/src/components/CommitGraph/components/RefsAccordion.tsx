import { useRef } from "react";
import { useWindowPointerDrag } from "@/components/layout/useWindowPointerDrag";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow, RepositoryWorktreeItem } from "@/generated/types";
import type { WorktreeMutationController } from "../hooks/useWorktreeMutations";
import type { BranchAction } from "./BranchContextMenu";
import { RefsAccordionPane, RefsPaneSplitter } from "./RefsAccordionPane";
import type { RefPanelSection } from "./RefsPanelData";
import type { TagAction } from "./TagContextMenu";
import { useRefsAccordionLayout } from "./useRefsAccordionLayout";
import { VirtualRefSection } from "./VirtualRefSection";
import { VirtualWorktreeList } from "./VirtualWorktreeList";

export type RefsAccordionActions = {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onCreateBranchFromTag: (tagName: string, commitHash: string) => void;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
};

export function RefsAccordion({
	actions,
	controller,
	sections,
	worktrees,
}: {
	actions: RefsAccordionActions;
	controller: WorktreeMutationController;
	sections: RefPanelSection[];
	worktrees: RepositoryWorktreeItem[];
}) {
	const containerRef = useRef<HTMLDivElement>(null);
	const startPointerDrag = useWindowPointerDrag();
	const entries = [
		{ count: worktrees.length, id: "Worktrees", type: "worktrees" as const },
		...sections.map((section) => ({
			count: section.count,
			id: section.label,
			section,
			type: "refs" as const,
		})),
	];
	const layout = useRefsAccordionLayout(entries.map((entry) => entry.id));
	return (
		<div
			className="flex min-h-0 flex-1 flex-col overflow-hidden"
			ref={containerRef}
		>
			{entries.map((entry, index) => {
				const isOpen = !layout.closed.has(entry.id);
				const nextOpen = entries
					.slice(index + 1)
					.find((candidate) => layout.openIds.includes(candidate.id));
				return (
					<div className={isOpen ? "contents" : "shrink-0"} key={entry.id}>
						<RefsAccordionPane
							count={entry.count}
							id={entry.id}
							isOpen={isOpen}
							onCreateWorktree={
								entry.type === "worktrees"
									? () => controller.setCreateBranchName("")
									: undefined
							}
							onToggle={() => layout.toggle(entry.id)}
							weight={layout.weights[entry.id] ?? 1}
						>
							{entry.type === "worktrees" ? (
								<VirtualWorktreeList
									controller={controller}
									worktrees={worktrees}
								/>
							) : (
								<VirtualRefSection actions={actions} section={entry.section} />
							)}
						</RefsAccordionPane>
						{isOpen && nextOpen ? (
							<RefsPaneSplitter
								onPointerDown={(event) =>
									startVerticalResize(
										event,
										containerRef.current,
										entry.id,
										nextOpen.id,
										layout.resize,
										startPointerDrag,
									)
								}
								onResizeBy={(amount) =>
									layout.resize(entry.id, nextOpen.id, amount)
								}
							/>
						) : null}
					</div>
				);
			})}
		</div>
	);
}

function startVerticalResize(
	event: React.PointerEvent<HTMLButtonElement>,
	container: HTMLDivElement | null,
	before: string,
	after: string,
	resize: (before: string, after: string, delta: number) => void,
	startPointerDrag: ReturnType<typeof useWindowPointerDrag>,
) {
	if (!container) return;
	event.preventDefault();
	let lastY = event.clientY;
	const height = Math.max(1, container.getBoundingClientRect().height);
	const move = (moveEvent: PointerEvent) => {
		resize(before, after, ((moveEvent.clientY - lastY) / height) * 2);
		lastY = moveEvent.clientY;
	};
	startPointerDrag({ onMove: move });
}
