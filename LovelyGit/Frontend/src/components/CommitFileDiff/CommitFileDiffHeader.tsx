import {
	Columns2,
	FileText,
	ListCollapse,
	Minus,
	Plus,
	Rows3,
	WrapText,
	X,
} from "lucide-react";
import type { CommitChangedFile } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";

export function CommitFileDiffHeader({
	file,
	onClose,
}: {
	file: CommitChangedFile;
	onClose: () => void;
}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");

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
				<button
					aria-label="Close diff"
					className="inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
					onClick={onClose}
					type="button"
				>
					<X aria-hidden="true" size={16} />
				</button>
			</div>
			<div className="flex h-10 items-center justify-center border-t bg-card/60 px-3">
				<div className="inline-flex rounded-md border bg-background p-0.5">
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
				<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
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
				<div className="ml-2 inline-flex rounded-md border bg-background p-0.5">
					<ModeButton
						icon={<WrapText aria-hidden="true" size={14} />}
						isActive={wrapLines}
						label="Wrap lines"
						onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
					/>
				</div>
			</div>
		</header>
	);
}

export function ContextLinesControl({
	contextLines,
}: {
	contextLines: number;
}) {
	const updateContextLines = (value: number) => {
		const nextValue = Math.max(0, Math.min(99, Math.trunc(value)));
		void setSetting("CommitDiffContextLines", nextValue);
	};

	return (
		<div className="ml-2 inline-flex h-8 items-center gap-1 rounded-md border bg-background px-2 text-xs text-muted-foreground">
			<span>Context</span>
			<div className="ml-1 inline-flex h-6 overflow-hidden rounded border bg-card text-foreground">
				<button
					aria-label="Decrease context lines"
					className="inline-flex w-6 items-center justify-center hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-35"
					disabled={contextLines <= 0}
					onClick={() => updateContextLines(contextLines - 1)}
					type="button"
				>
					<Minus aria-hidden="true" size={12} />
				</button>
				<div className="flex min-w-7 items-center justify-center border-x px-1 font-mono text-xs">
					{contextLines}
				</div>
				<button
					aria-label="Increase context lines"
					className="inline-flex w-6 items-center justify-center hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-35"
					disabled={contextLines >= 99}
					onClick={() => updateContextLines(contextLines + 1)}
					type="button"
				>
					<Plus aria-hidden="true" size={12} />
				</button>
			</div>
		</div>
	);
}

function ModeButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: React.ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<button
			aria-label={label}
			className={`inline-flex h-7 items-center gap-1 rounded px-2 text-xs text-muted-foreground hover:bg-accent hover:text-accent-foreground ${
				isActive ? "bg-accent font-semibold text-accent-foreground" : ""
			}`}
			onClick={onClick}
			title={label}
			type="button"
		>
			{icon}
			<span>{label}</span>
		</button>
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
