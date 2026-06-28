import { Copy, ExternalLink, FileText, GitBranch, Tag } from "lucide-react";
import { toast } from "sonner";
import {
	ContextMenuItem,
	ContextMenuSeparator,
} from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { copyToClipboard } from "../utils/clipboard";
import { shortHash } from "../utils/format";

export function CommitRowLinkMenuItems({
	refs,
	repositoryId,
	row,
	subject,
}: {
	refs: string[];
	repositoryId: string | null;
	row: CommitGraphRow;
	subject: string;
}) {
	const remoteUrl = row.commit.remoteUrl;
	const remoteRepositoryUrl = row.commit.remoteRepositoryUrl;
	return (
		<>
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
			<ContextMenuItem onClick={() => void copyToClipboard(subject, "Subject")}>
				<Copy />
				Copy subject
			</ContextMenuItem>
			<ContextMenuItem
				onClick={() => void copyToClipboard(row.commit.message, "Message")}
			>
				<Copy />
				Copy message
			</ContextMenuItem>
			{repositoryId ? (
				<ContextMenuItem
					onClick={() => void copyCommitPatch(repositoryId, row.commit.hash)}
				>
					<FileText />
					Copy patch
				</ContextMenuItem>
			) : null}
			{remoteUrl ? (
				<>
					<ContextMenuItem
						onClick={() => void copyToClipboard(remoteUrl, "Remote commit URL")}
					>
						<Copy />
						Copy remote commit URL
					</ContextMenuItem>
					<ContextMenuItem onClick={() => openRemoteCommit(remoteUrl)}>
						<ExternalLink />
						Open commit on remote
					</ContextMenuItem>
				</>
			) : null}
			{remoteRepositoryUrl ? (
				<>
					<ContextMenuItem
						onClick={() =>
							void copyToClipboard(remoteRepositoryUrl, "Remote repository URL")
						}
					>
						<Copy />
						Copy remote repository URL
					</ContextMenuItem>
					<ContextMenuItem
						onClick={() => openRemoteRepository(remoteRepositoryUrl)}
					>
						<ExternalLink />
						Open repository on remote
					</ContextMenuItem>
				</>
			) : null}
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
		</>
	);
}

function openRemoteCommit(remoteUrl: string) {
	window.open(remoteUrl, "_blank", "noopener,noreferrer");
	toast.success("Opened commit on remote");
}

function openRemoteRepository(remoteUrl: string) {
	window.open(remoteUrl, "_blank", "noopener,noreferrer");
	toast.success("Opened repository on remote");
}

async function copyCommitPatch(repositoryId: string, commitHash: string) {
	try {
		const response = await sendRequestWithResponse({
			commandType: NativeMessageType.GetCommitPatch,
			arguments: {
				repositoryId,
				commitHash,
			},
		});
		await copyToClipboard(response.patch, "Patch");
		if (response.isTruncated) {
			toast.warning("Patch copied, but it was truncated");
		}
	} catch (error) {
		toast.error(
			error instanceof Error ? error.message : "Failed to copy commit patch.",
		);
	}
}
