import { ContextMenuContent } from "@/components/ui/context-menu";
import {
	CommitContextMenuItems,
	type CommitContextMenuPopupProps,
} from "./CommitContextMenuPopup";

export function CommitContextMenuPopup(props: CommitContextMenuPopupProps) {
	return (
		<ContextMenuContent className="max-w-96">
			<CommitContextMenuItems {...props} />
		</ContextMenuContent>
	);
}
