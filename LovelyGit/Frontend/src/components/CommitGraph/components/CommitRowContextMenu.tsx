import {
	Copy,
	GitBranch,
	GitCommitHorizontal,
	GitPullRequestArrow,
	Tag,
} from "lucide-react";
import type { ReactElement } from "react";
import { useState } from "react";
import { toast } from "sonner";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";
import { shortHash } from "../utils/format";
import { CheckoutCommitDetachedDialog } from "./CheckoutCommitDetachedDialog";
import { CherryPickCommitDialog } from "./CherryPickCommitDialog";
import { CreateBranchFromCommitDialog } from "./CreateBranchFromCommitDialog";
import { CreateTagAtCommitDialog } from "./CreateTagAtCommitDialog";

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
					<ContextMenuSeparator />
					<ContextMenuItem
						onClick={() => void copyToClipboard(row.commit.hash, "Commit hash")}
					>
						<Copy />
						Copy full hash
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() =>
							void copyToClipboard(shortHash(row.commit.hash), "Short hash")
						}
					>
						<Copy />
						Copy short hash
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() => void copyToClipboard(subject, "Subject")}
					>
						<Copy />
						Copy subject
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() => void copyToClipboard(row.commit.message, "Message")}
					>
						<Copy />
						Copy message
					</ContextMenuItem>
					{refs.length > 0 ? (
						<>
							<ContextMenuSeparator />
							<ContextMenuItem
								onClick={() => void copyToClipboard(refs.join("\n"), "Refs")}
							>
								<GitBranch />
								Copy refs
							</ContextMenuItem>
							{row.commit.tags.length > 0 ? (
								<ContextMenuItem
									onClick={() =>
										void copyToClipboard(row.commit.tags.join("\n"), "Tags")
									}
								>
									<Tag />
									Copy tags
								</ContextMenuItem>
							) : null}
						</>
					) : null}
				</ContextMenuContent>
			</ContextMenu>
			<CherryPickCommitDialog
				commitHash={row.commit.hash}
				isOpen={isCherryPickOpen}
				onOpenChange={setIsCherryPickOpen}
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

async function copyToClipboard(value: string, label: string) {
	try {
		await navigator.clipboard.writeText(value);
		toast.success(`${label} copied`);
	} catch {
		toast.error(`Could not copy ${label.toLowerCase()}`);
	}
}
