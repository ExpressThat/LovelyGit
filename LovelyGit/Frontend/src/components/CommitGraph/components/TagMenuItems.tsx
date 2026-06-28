import { Copy, GitCommitHorizontal, Trash2 } from "lucide-react";
import { ContextMenuItem } from "@/components/ui/context-menu";
import { copyToClipboard } from "../utils/clipboard";

export function TagMenuItems({
	canCheckoutTag,
	canDeleteTag,
	onCheckout,
	onDelete,
	tagName,
}: {
	canCheckoutTag: boolean;
	canDeleteTag: boolean;
	onCheckout: () => void;
	onDelete: () => void;
	tagName: string;
}) {
	return (
		<>
			{canCheckoutTag ? (
				<ContextMenuItem onClick={onCheckout}>
					<GitCommitHorizontal />
					Checkout tag
				</ContextMenuItem>
			) : null}
			<ContextMenuItem
				onClick={() => void copyToClipboard(tagName, "Tag name")}
			>
				<Copy />
				Copy tag name
			</ContextMenuItem>
			{canDeleteTag ? (
				<ContextMenuItem onClick={onDelete} variant="destructive">
					<Trash2 />
					Delete local tag
				</ContextMenuItem>
			) : null}
		</>
	);
}
