import { X } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { CommitChangedFile } from "@/generated/types";
import { DiffToolbarControls } from "./DiffToolbarControls";

export function CommitFileDiffHeader({
	file,
	onClose,
}: {
	file: CommitChangedFile;
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
					<span>{file.status}</span>
					<span>
						+{file.additions} -{file.deletions}
					</span>
				</div>
				<Button
					aria-label="Close diff"
					size="icon-sm"
					onClick={onClose}
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

function folderPrefix(path: string) {
	const slashIndex = path.lastIndexOf("/");
	return slashIndex >= 0 ? path.slice(0, slashIndex + 1) : "";
}

function fileName(path: string) {
	const slashIndex = path.lastIndexOf("/");
	return slashIndex >= 0 ? path.slice(slashIndex + 1) : path;
}
