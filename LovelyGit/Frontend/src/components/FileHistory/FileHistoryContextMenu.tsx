import { History } from "lucide-react";
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
	onOpen,
	path,
}: {
	children: ReactNode;
	onOpen: () => void;
	path: string;
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
			</ContextMenuContent>
		</ContextMenu>
	);
}
