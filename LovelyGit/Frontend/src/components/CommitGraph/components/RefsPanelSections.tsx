import { MoreHorizontal } from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import type { CommitGraphRow } from "@/generated/types";
import { shortHash } from "../utils/format";
import {
	CreateBranchDialog,
	DeleteBranchDialog,
	RenameBranchDialog,
	useBranchMutation,
} from "./BranchActionDialogs";
import { RefIcon } from "./RefCellUtils";
import type { RefPanelItem, RefPanelSection } from "./RefsPanelData";

export function RefSection({
	onRepositoryMutation,
	onSelectCommit,
	repositoryId,
	section,
}: {
	onRepositoryMutation: () => void;
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
						item={item}
						key={`${item.kind}:${item.name}:${item.commitHash}`}
						onRepositoryMutation={onRepositoryMutation}
						onSelectCommit={onSelectCommit}
						repositoryId={repositoryId}
					/>
				))}
			</div>
		</section>
	);
}

export function RefPanelRow({
	item,
	onRepositoryMutation,
	onSelectCommit,
	repositoryId,
}: {
	item: RefPanelItem;
	onRepositoryMutation: () => void;
	onSelectCommit: (row: CommitGraphRow) => void;
	repositoryId: string | null;
}) {
	const row = item.row;
	const [createOpen, setCreateOpen] = useState(false);
	const [deleteOpen, setDeleteOpen] = useState(false);
	const [renameOpen, setRenameOpen] = useState(false);
	const { isBusy, runCheckout } = useBranchMutation({
		onSuccess: onRepositoryMutation,
		repositoryId,
	});
	const isLocalBranch = item.kind === "Local";
	const isRemoteBranch = item.kind === "Remote";
	const canMutate = Boolean(repositoryId) && (isLocalBranch || isRemoteBranch);
	const canDelete = isLocalBranch && !item.isCurrent;
	const localBranchName = isRemoteBranch
		? deriveLocalBranchName(item.name)
		: item.name;
	return (
		<div className="group/ref-row flex h-7 min-w-0 items-center rounded-md hover:bg-accent">
			<Button
				aria-disabled={!row}
				className="h-7 min-w-0 flex-1 justify-start gap-2 rounded-r-none px-2 font-normal"
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
			{canMutate ? (
				<DropdownMenu>
					<DropdownMenuTrigger
						aria-label={`Branch actions for ${item.label}`}
						className="inline-flex h-7 w-7 shrink-0 items-center justify-center rounded-r-md text-muted-foreground opacity-0 hover:bg-accent hover:text-accent-foreground focus-visible:opacity-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring group-hover/ref-row:opacity-100"
						title={`Branch actions for ${item.label}`}
					>
						<MoreHorizontal aria-hidden="true" className="size-4" />
					</DropdownMenuTrigger>
					<DropdownMenuContent align="end" className="min-w-48">
						<DropdownMenuItem
							disabled={isBusy || item.isCurrent}
							onClick={() =>
								void runCheckout({
									branchName: item.name,
									isRemote: isRemoteBranch,
									label: localBranchName,
									localBranchName,
								})
							}
						>
							Checkout
						</DropdownMenuItem>
						<DropdownMenuItem onClick={() => setCreateOpen(true)}>
							Create branch here
						</DropdownMenuItem>
						{isLocalBranch ? (
							<>
								<DropdownMenuSeparator />
								<DropdownMenuItem
									disabled={isBusy}
									onClick={() => setRenameOpen(true)}
								>
									Rename branch
								</DropdownMenuItem>
								<DropdownMenuItem
									disabled={!canDelete || isBusy}
									onClick={() => setDeleteOpen(true)}
									variant="destructive"
								>
									Delete branch
								</DropdownMenuItem>
							</>
						) : null}
					</DropdownMenuContent>
				</DropdownMenu>
			) : null}
			<CreateBranchDialog
				defaultCheckout={false}
				onOpenChange={setCreateOpen}
				onSuccess={onRepositoryMutation}
				open={createOpen}
				repositoryId={repositoryId}
				startPoint={item.commitHash}
			/>
			<RenameBranchDialog
				branchName={item.name}
				onOpenChange={setRenameOpen}
				onSuccess={onRepositoryMutation}
				open={renameOpen}
				repositoryId={repositoryId}
			/>
			<DeleteBranchDialog
				branchName={item.name}
				onOpenChange={setDeleteOpen}
				onSuccess={onRepositoryMutation}
				open={deleteOpen}
				repositoryId={repositoryId}
			/>
		</div>
	);
}

function deriveLocalBranchName(remoteBranchName: string) {
	const slashIndex = remoteBranchName.indexOf("/");
	return slashIndex >= 0 && slashIndex < remoteBranchName.length - 1
		? remoteBranchName.slice(slashIndex + 1)
		: remoteBranchName;
}
