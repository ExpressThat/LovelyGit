import { EyeOff, FileSearch, HardDrive, History } from "lucide-react";
import type { ReactNode } from "react";
import {
	ContextMenu,
	ContextMenuContent,
	ContextMenuGroup,
	ContextMenuItem,
	ContextMenuLabel,
	ContextMenuSeparator,
	ContextMenuTrigger,
} from "@/components/ui/context-menu";

export function FileHistoryContextMenu({
	children,
	canBlame = true,
	onOpenBlame,
	onOpen,
	path,
	blameLabel = "View line blame…",
	onIgnoreLocal,
	onIgnoreShared,
}: {
	children: ReactNode;
	canBlame?: boolean;
	onOpenBlame: () => void;
	onOpen: () => void;
	path: string;
	blameLabel?: string;
	onIgnoreLocal?: () => void;
	onIgnoreShared?: () => void;
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
				{onIgnoreLocal && onIgnoreShared ? (
					<>
						<ContextMenuSeparator />
						<ContextMenuGroup>
							<ContextMenuLabel>Ignore untracked file</ContextMenuLabel>
							<ContextMenuItem
								aria-label="Add exact path to .gitignore"
								onClick={onIgnoreShared}
							>
								<EyeOff aria-hidden="true" />
								Add exact path to .gitignore
							</ContextMenuItem>
							<ContextMenuItem
								aria-label="Ignore exact path locally"
								onClick={onIgnoreLocal}
							>
								<HardDrive aria-hidden="true" />
								Ignore exact path locally
							</ContextMenuItem>
						</ContextMenuGroup>
					</>
				) : null}
			</ContextMenuContent>
		</ContextMenu>
	);
}
