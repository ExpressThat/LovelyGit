import { FileSearch, History } from "lucide-react";
import type { ReactNode } from "react";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";

export function FileHistoryContextMenu({
	children,
	canBlame = true,
	onOpenBlame,
	onOpen,
	path,
	blameLabel = "View line blame…",
}: {
	children: ReactNode;
	canBlame?: boolean;
	onOpenBlame: () => void;
	onOpen: () => void;
	path: string;
	blameLabel?: string;
}) {
	return (
		<ContextMenu>
			<ContextMenuTrigger className="block w-full">
				{children}
			</ContextMenuTrigger>
			<ContextMenuContent>
				<ContextMenuGroup>
					<ContextMenuLabel className="max-w-64 truncate" title={path}>
						{path}
					</ContextMenuLabel>
				</ContextMenuGroup>
				<ContextMenuItem onClick={onOpen}>
					<History aria-hidden="true" />
					View file history…
				</ContextMenuItem>
				<ContextMenuItem disabled={!canBlame} onClick={onOpenBlame}>
					<FileSearch aria-hidden="true" />
					{blameLabel}
				</ContextMenuItem>
			</ContextMenuContent>
		</ContextMenu>
	);
}
