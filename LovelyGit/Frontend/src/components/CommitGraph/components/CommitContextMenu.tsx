import type { ReactNode } from "react";
import { ContextMenu, ContextMenuTrigger } from "@/components/ui/context-menu";
import type { CommitContextMenuPopupProps } from "./CommitContextMenuPopup";
import { CommitContextMenuPopup } from "./CommitContextMenuPopupShell";

export function CommitContextMenu({
	children,
	...popupProps
}: CommitContextMenuPopupProps & { children: ReactNode }) {
	return (
		<ContextMenu>
			<ContextMenuTrigger className="block w-full">
				{children}
			</ContextMenuTrigger>
			<CommitContextMenuPopup {...popupProps} />
		</ContextMenu>
	);
}
