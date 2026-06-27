import { GitCommitHorizontal, Trash2 } from "lucide-react";
import { ContextMenuItem } from "@/components/ui/context-menu";

export function TagMenuItems({
	canCheckoutTag,
	canDeleteTag,
	onCheckout,
	onDelete,
}: {
	canCheckoutTag: boolean;
	canDeleteTag: boolean;
	onCheckout: () => void;
	onDelete: () => void;
}) {
	if (!canCheckoutTag && !canDeleteTag) {
		return null;
	}

	return (
		<>
			{canCheckoutTag ? (
				<ContextMenuItem onClick={onCheckout}>
					<GitCommitHorizontal />
					Checkout tag
				</ContextMenuItem>
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
