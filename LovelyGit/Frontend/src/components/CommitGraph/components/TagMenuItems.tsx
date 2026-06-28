import {
	Copy,
	ExternalLink,
	GitBranch,
	GitCommitHorizontal,
	Trash2,
} from "lucide-react";
import { toast } from "sonner";
import { ContextMenuItem } from "@/components/ui/context-menu";
import { copyToClipboard } from "../utils/clipboard";

export function TagMenuItems({
	canCheckoutTag,
	canCreateBranch,
	canDeleteTag,
	onCheckout,
	onCreateBranch,
	onDelete,
	tagName,
	tagRemoteUrl,
}: {
	canCheckoutTag: boolean;
	canCreateBranch: boolean;
	canDeleteTag: boolean;
	onCheckout: () => void;
	onCreateBranch: () => void;
	onDelete: () => void;
	tagName: string;
	tagRemoteUrl?: string | null;
}) {
	return (
		<>
			{canCheckoutTag ? (
				<ContextMenuItem onClick={onCheckout}>
					<GitCommitHorizontal />
					Checkout tag
				</ContextMenuItem>
			) : null}
			{canCreateBranch ? (
				<ContextMenuItem onClick={onCreateBranch}>
					<GitBranch />
					Create branch from tag
				</ContextMenuItem>
			) : null}
			<ContextMenuItem
				onClick={() => void copyToClipboard(tagName, "Tag name")}
			>
				<Copy />
				Copy tag name
			</ContextMenuItem>
			{tagRemoteUrl ? (
				<>
					<ContextMenuItem
						onClick={() => void copyToClipboard(tagRemoteUrl, "Remote tag URL")}
					>
						<Copy />
						Copy remote tag URL
					</ContextMenuItem>
					<ContextMenuItem onClick={() => openRemoteTag(tagRemoteUrl)}>
						<ExternalLink />
						Open tag on remote
					</ContextMenuItem>
				</>
			) : null}
			{canDeleteTag ? (
				<ContextMenuItem onClick={onDelete} variant="destructive">
					<Trash2 />
					Delete local tag
				</ContextMenuItem>
			) : null}
		</>
	);
}

function openRemoteTag(remoteUrl: string) {
	window.open(remoteUrl, "_blank", "noopener,noreferrer");
	toast.success("Opened tag on remote");
}
