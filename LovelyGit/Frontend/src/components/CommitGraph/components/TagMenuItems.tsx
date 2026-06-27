import { Trash2 } from "lucide-react";
import { ContextMenuItem } from "@/components/ui/context-menu";

export function TagMenuItems({
	canDeleteTag,
	onDelete,
}: {
	canDeleteTag: boolean;
	onDelete: () => void;
}) {
	if (!canDeleteTag) {
		return null;
	}

	return (
		<ContextMenuItem onClick={onDelete} variant="destructive">
			<Trash2 />
			Delete local tag
		</ContextMenuItem>
	);
}
