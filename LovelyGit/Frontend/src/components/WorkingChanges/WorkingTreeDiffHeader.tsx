import { X } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { DiffToolbarControls } from "../CommitFileDiff/DiffToolbarControls";
import { fileName, folderPrefix } from "./WorkingTreeFileDiffHelpers";

export function WorkingTreeDiffHeader({
	file,
	onClose,
}: {
	file: WorkingTreeChangedFile;
	onClose: () => void;
}) {
	return (
		<header className="shrink-0 border-b bg-popover text-popover-foreground">
			<div className="flex h-10 items-center gap-2 px-3">
				<div className="min-w-0 flex-1 truncate font-mono text-sm text-muted-foreground">
					<span>{folderPrefix(file.path)}</span>
					<span className="font-semibold text-foreground">
						{fileName(file.path)}
					</span>
				</div>
				<div className="hidden items-center gap-2 text-[10px] uppercase text-muted-foreground md:flex">
					<span>{file.group}</span>
					<span>{file.status}</span>
				</div>
				<Button
					aria-label="Close diff"
					onClick={onClose}
					size="icon-sm"
					type="button"
					variant="ghost"
				>
					<X aria-hidden="true" size={16} />
				</Button>
			</div>
			<DiffToolbarControls />
		</header>
	);
}
