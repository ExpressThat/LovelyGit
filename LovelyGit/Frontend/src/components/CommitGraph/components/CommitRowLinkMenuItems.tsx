import { Copy, ExternalLink, GitBranch, Tag } from "lucide-react";
import { toast } from "sonner";
import {
	ContextMenuItem,
	ContextMenuSeparator,
} from "@/components/ui/context-menu";
import type { CommitGraphRow } from "@/generated/types";
import { copyToClipboard } from "../utils/clipboard";
import { shortHash } from "../utils/format";

export function CommitRowLinkMenuItems({
	refs,
	row,
	subject,
}: {
	refs: string[];
	row: CommitGraphRow;
	subject: string;
}) {
	const remoteUrl = row.commit.remoteUrl;
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
