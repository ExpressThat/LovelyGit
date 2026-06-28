import {
	GitBranch,
	GitCommitHorizontal,
	GitPullRequestArrow,
	RotateCcw,
	Tag,
	Undo2,
} from "lucide-react";
import type { ReactElement } from "react";
import { useState } from "react";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuSub,
	ContextMenuSubContent,
	ContextMenuSubTrigger,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import {
	type CommitGraphRow,
	GitResetMode,
	type GitResetMode as GitResetModeValue,
} from "@/generated/types";
import { shortHash } from "../utils/format";
import { CheckoutCommitDetachedDialog } from "./CheckoutCommitDetachedDialog";
import { CherryPickCommitDialog } from "./CherryPickCommitDialog";
import { CommitRowLinkMenuItems } from "./CommitRowLinkMenuItems";
import { CreateBranchFromCommitDialog } from "./CreateBranchFromCommitDialog";
import { CreateTagAtCommitDialog } from "./CreateTagAtCommitDialog";
import { ResetCurrentBranchDialog } from "./ResetCurrentBranchDialog";
import { RevertCommitDialog } from "./RevertCommitDialog";

export function CommitRowContextMenu({
	children,
	onRefsChanged,
	repositoryId,
	row,
}: {
	children: ReactElement;
	onRefsChanged: () => void;
	repositoryId: string | null;
	row: CommitGraphRow;
}) {
	const [isCherryPickOpen, setIsCherryPickOpen] = useState(false);
	const [isCheckoutOpen, setIsCheckoutOpen] = useState(false);
	const [isCreateBranchOpen, setIsCreateBranchOpen] = useState(false);
	const [isCreateTagOpen, setIsCreateTagOpen] = useState(false);
	const [isRevertOpen, setIsRevertOpen] = useState(false);
	const [resetMode, setResetMode] = useState<GitResetModeValue | null>(null);
	const refs = commitRefs(row);
	const subject = commitSubject(row);

	return (
		<>
			<ContextMenu>
				<ContextMenuTrigger render={children} />
				<ContextMenuContent className="w-56">
					<ContextMenuGroup>
						<ContextMenuLabel className="truncate font-mono">
							{shortHash(row.commit.hash)}
						</ContextMenuLabel>
					</ContextMenuGroup>
					<ContextMenuSeparator />
					<ContextMenuItem
						disabled={repositoryId === null}
						onClick={() => setIsCheckoutOpen(true)}
					>
						<GitCommitHorizontal />
						Checkout commit
					</ContextMenuItem>
					<ContextMenuItem
						disabled={repositoryId === null}
						onClick={() => setIsCherryPickOpen(true)}
					>
						<GitPullRequestArrow />
						Cherry-pick commit
					</ContextMenuItem>
					<ContextMenuItem
						className="text-destructive focus:text-destructive"
						disabled={repositoryId === null}
						onClick={() => setIsRevertOpen(true)}
					>
						<RotateCcw />
						Revert commit
					</ContextMenuItem>
					<ContextMenuSub>
						<ContextMenuSubTrigger disabled={repositoryId === null}>
							<Undo2 />
							Reset current branch
						</ContextMenuSubTrigger>
						<ContextMenuSubContent className="w-44">
							<ContextMenuItem onClick={() => setResetMode(GitResetMode.Soft)}>
								Soft reset
							</ContextMenuItem>
							<ContextMenuItem onClick={() => setResetMode(GitResetMode.Mixed)}>
								Mixed reset
							</ContextMenuItem>
							<ContextMenuItem
								variant="destructive"
								onClick={() => setResetMode(GitResetMode.Hard)}
							>
								Hard reset
							</ContextMenuItem>
						</ContextMenuSubContent>
					</ContextMenuSub>
					<ContextMenuItem
						disabled={repositoryId === null}
						onClick={() => setIsCreateBranchOpen(true)}
					>
						<GitBranch />
						Create branch
					</ContextMenuItem>
					<ContextMenuItem
						disabled={repositoryId === null}
						onClick={() => setIsCreateTagOpen(true)}
					>
						<Tag />
						Create tag
					</ContextMenuItem>
					<CommitRowLinkMenuItems
						refs={refs}
						repositoryId={repositoryId}
						row={row}
						subject={subject}
					/>
				</ContextMenuContent>
			</ContextMenu>
			<CherryPickCommitDialog
				commitHash={row.commit.hash}
				isOpen={isCherryPickOpen}
				onOpenChange={setIsCherryPickOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<RevertCommitDialog
				commitHash={row.commit.hash}
				isOpen={isRevertOpen}
				onOpenChange={setIsRevertOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<CheckoutCommitDetachedDialog
				commitHash={row.commit.hash}
				isOpen={isCheckoutOpen}
				onOpenChange={setIsCheckoutOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<ResetCurrentBranchDialog
				commitHash={row.commit.hash}
				mode={resetMode}
				onOpenChange={(isOpen) => {
					if (!isOpen) {
						setResetMode(null);
					}
				}}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<CreateBranchFromCommitDialog
				commitHash={row.commit.hash}
				isOpen={isCreateBranchOpen}
				onOpenChange={setIsCreateBranchOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
			<CreateTagAtCommitDialog
				commitHash={row.commit.hash}
				isOpen={isCreateTagOpen}
				onOpenChange={setIsCreateTagOpen}
				onSuccess={onRefsChanged}
				repositoryId={repositoryId}
			/>
		</>
	);
}

function commitSubject(row: CommitGraphRow) {
	return row.commit.message.split(/\r?\n/, 1)[0] || "(no commit message)";
}

function commitRefs(row: CommitGraphRow) {
	const refs =
		row.commit.refs.length > 0
			? row.commit.refs.map((ref) => ref.name)
			: [...row.commit.branches, ...row.commit.tags];
	return [...new Set(refs)].sort((left, right) => left.localeCompare(right));
}
