import { Button } from "@/components/ui/button";
import type { CommitGraphRow } from "@/generated/types";
import { shortHash } from "../utils/format";
import { BranchRefContextMenu } from "./BranchRefContextMenu";
import { RefIcon } from "./RefCellUtils";
import type { RefPanelItem, RefPanelSection } from "./RefsPanelData";
import { refPanelItemToRefInfo } from "./RefsPanelData";
import { StashRefContextMenu } from "./StashRefContextMenu";

export function RefSection({
	currentBranchName,
	onRefsChanged,
	onSelectCommit,
	repositoryId,
	section,
}: {
	currentBranchName: string | null;
	onRefsChanged: () => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	repositoryId: string | null;
	section: RefPanelSection;
}) {
	return (
		<section className="mb-3 last:mb-0">
			<div className="mb-1 flex items-center justify-between px-1 text-[10px] font-semibold uppercase text-muted-foreground">
				<span>{section.label}</span>
				<span>{section.count}</span>
			</div>
			<div className="grid gap-1">
				{section.items.map((item) => (
					<RefPanelRow
						currentBranchName={currentBranchName}
						item={item}
						key={`${item.kind}:${item.name}:${item.commitHash}`}
						onRefsChanged={onRefsChanged}
						onSelectCommit={onSelectCommit}
						repositoryId={repositoryId}
					/>
				))}
			</div>
		</section>
	);
}

export function RefPanelRow({
	currentBranchName,
	item,
	onRefsChanged,
	onSelectCommit,
	repositoryId,
}: {
	currentBranchName: string | null;
	item: RefPanelItem;
	onRefsChanged: () => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	repositoryId: string | null;
}) {
	const row = item.row;
	const button = (
		<Button
			aria-disabled={!row}
			className="h-7 min-w-0 justify-start gap-2 px-2 font-normal"
			onClick={() => {
				if (row) onSelectCommit(row);
			}}
			title={
				row
					? `${item.name} at ${shortHash(item.commitHash)}`
					: `${item.name} at ${shortHash(item.commitHash)}. Load this commit in the graph to select it.`
			}
			variant={item.isCurrent ? "secondary" : "ghost"}
		>
			<RefIcon kind={item.kind} />
			<span className="min-w-0 flex-1 truncate text-left">{item.label}</span>
			<span className="font-mono text-[10px] text-muted-foreground">
				{shortHash(item.commitHash)}
			</span>
		</Button>
	);

	if (item.kind === "Stash") {
		return (
			<StashRefContextMenu
				onRefsChanged={onRefsChanged}
				refInfo={refPanelItemToRefInfo(item)}
				repositoryId={repositoryId}
			>
				{button}
			</StashRefContextMenu>
		);
	}

	return (
		<BranchRefContextMenu
			currentBranchName={currentBranchName}
			onRefsChanged={onRefsChanged}
			refInfo={refPanelItemToRefInfo(item)}
			repositoryId={repositoryId}
		>
			{button}
		</BranchRefContextMenu>
	);
}
