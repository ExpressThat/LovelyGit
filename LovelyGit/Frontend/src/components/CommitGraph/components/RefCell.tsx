import { PreviewCard } from "@base-ui/react/preview-card";
import type { BranchIntegrationMode } from "@/components/TopNavBar/components/BranchIntegrationDialog";
import type { CommitGraphRow } from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";
import type { BranchAction } from "./BranchContextMenu";
import { buildRefCellGroupView } from "./RefCellGrouping";
import { groupRefs, normalizeRefs } from "./RefCellUtils";
import { RefGroupHoverList } from "./RefGroupHoverList";
import { RefGroupPill } from "./RefGroupPill";
import type { TagAction } from "./TagContextMenu";

export function RefCell({
	branchMutationBusy,
	branchRemoteName,
	currentBranchName,
	onBranchAction,
	onCreateBranchFromTag,
	onIntegrateBranch,
	onTagAction,
	remotePrefixes,
	row,
	tagMutationBusy,
	tagRemoteName,
}: {
	branchMutationBusy: boolean;
	branchRemoteName: string | null;
	currentBranchName: string | null;
	onBranchAction: (action: BranchAction, branchName: string) => void;
	onCreateBranchFromTag: (tagName: string, commitHash: string) => void;
	onIntegrateBranch: (mode: BranchIntegrationMode, branchName: string) => void;
	onTagAction: (action: TagAction, tagName: string) => void;
	remotePrefixes: string[];
	row: CommitGraphRow;
	tagMutationBusy: boolean;
	tagRemoteName: string | null;
}) {
	const reduceMotion = useReducedMotion();
	if (row.commit.refs.length === 0) {
		return <div className="h-[17px]" />;
	}
	const refs = normalizeRefs(row.commit.refs, remotePrefixes);
	const groups = groupRefs(refs, remotePrefixes, currentBranchName);
	const groupView = buildRefCellGroupView(groups);

	if (groups.length === 0) {
		return <div className="h-[17px]" />;
	}

	return (
		<div className="flex h-full min-w-0 items-center gap-1 leading-none">
			{groupView.hiddenCount > 0 ? (
				<>
					<PreviewCard.Root>
						<PreviewCard.Trigger
							aria-label={`Show ${groupView.hiddenCount} grouped references at ${row.commit.hash}`}
							closeDelay={180}
							delay={100}
							onClick={(event) => {
								event.preventDefault();
								event.stopPropagation();
							}}
							render={
								<motion.div
									className="min-w-0 cursor-pointer rounded-[3px] focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring"
									whileHover={{ scale: reduceMotion ? 1 : 1.02 }}
								/>
							}
							role="button"
							tabIndex={0}
						>
							<RefGroupPill
								{...pillProps()}
								group={groupView.visibleGroups[0]}
							/>
						</PreviewCard.Trigger>
						<PreviewCard.Portal>
							<PreviewCard.Positioner
								align="start"
								className="isolate z-50"
								side="bottom"
								sideOffset={4}
							>
								<PreviewCard.Popup
									aria-label={`References at ${row.commit.hash.slice(0, 7)}`}
									className="w-fit min-w-32 max-w-72 origin-(--transform-origin) rounded-md border bg-popover p-1.5 text-popover-foreground shadow-xl data-open:animate-in data-open:fade-in-0 data-open:zoom-in-95 data-closed:animate-out data-closed:fade-out-0 data-closed:zoom-out-95"
								>
									<RefGroupHoverList {...pillProps()} groups={groups} />
								</PreviewCard.Popup>
							</PreviewCard.Positioner>
						</PreviewCard.Portal>
					</PreviewCard.Root>
					<HiddenRefCount count={groupView.hiddenCount} />
				</>
			) : (
				<RefGroupPill {...pillProps()} group={groupView.visibleGroups[0]} />
			)}
		</div>
	);

	function pillProps() {
		return {
			branchMutationBusy,
			branchRemoteName,
			commitHash: row.commit.hash,
			currentBranchName,
			onBranchAction,
			onCreateBranchFromTag,
			onIntegrateBranch,
			onTagAction,
			remotePrefixes,
			tagMutationBusy,
			tagRemoteName,
		};
	}
}

function HiddenRefCount({ count }: { count: number }) {
	return (
		<span
			className="inline-flex h-[14px] shrink-0 items-center rounded-[3px] border bg-muted px-1 font-semibold text-[9px] text-muted-foreground shadow-sm"
			title={`${count} additional references`}
		>
			+{count}
		</span>
	);
}
