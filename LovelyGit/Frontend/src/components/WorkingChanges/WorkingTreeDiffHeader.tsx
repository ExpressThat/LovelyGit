import {
	Columns2,
	FileText,
	ListCollapse,
	Pilcrow,
	Rows3,
	WrapText,
	X,
} from "lucide-react";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { ContextLinesControl } from "../CommitFileDiff/CommitFileDiffHeader";
import {
	fileName,
	folderPrefix,
	ModeButton,
} from "./WorkingTreeFileDiffHelpers";

export function WorkingTreeDiffHeader({
	file,
	onClose,
}: {
	file: WorkingTreeChangedFile;
	onClose: () => void;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");

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
				<button
					aria-label="Close diff"
					className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
					onClick={onClose}
					type="button"
				>
					<X aria-hidden="true" size={16} />
				</button>
			</div>
			<div className="diff-toolbar flex h-10 items-center justify-center border-t bg-card/60 px-3">
				<div className="inline-flex shrink-0 rounded-md border bg-background p-0.5">
					<ModeButton
						icon={<Columns2 aria-hidden="true" size={14} />}
						isActive={viewMode === "SideBySide"}
						label="Side by side"
						onClick={() => void setSetting("CommitDiffViewMode", "SideBySide")}
					/>
					<ModeButton
						icon={<Rows3 aria-hidden="true" size={14} />}
						isActive={viewMode === "Combined"}
						label="Combined"
						onClick={() => void setSetting("CommitDiffViewMode", "Combined")}
					/>
				</div>
				<div className="diff-toolbar-control ml-2 inline-flex shrink-0 rounded-md border bg-background p-0.5">
					<ModeButton
						icon={<ListCollapse aria-hidden="true" size={14} />}
						isActive={lineDisplayMode === "Changes"}
						label="Changes"
						onClick={() =>
							void setSetting("CommitDiffLineDisplayMode", "Changes")
						}
					/>
					<ModeButton
						icon={<FileText aria-hidden="true" size={14} />}
						isActive={lineDisplayMode === "FullFile"}
						label="Full file"
						onClick={() =>
							void setSetting("CommitDiffLineDisplayMode", "FullFile")
						}
					/>
				</div>
				{lineDisplayMode === "Changes" ? (
					<ContextLinesControl contextLines={contextLines} />
				) : null}
				<div className="diff-toolbar-control ml-2 inline-flex shrink-0 rounded-md border bg-background p-0.5">
					<ModeButton
						icon={<WrapText aria-hidden="true" size={14} />}
						isActive={wrapLines}
						label="Wrap lines"
						onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
					/>
					<ModeButton
						icon={<Pilcrow aria-hidden="true" size={14} />}
						isActive={ignoreWhitespace}
						label="Ignore whitespace"
						onClick={() =>
							void setSetting("CommitDiffIgnoreWhitespace", !ignoreWhitespace)
						}
					/>
				</div>
			</div>
		</header>
	);
}
