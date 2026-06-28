import {
	Columns2,
	FileText,
	ListCollapse,
	Minus,
	Pilcrow,
	Plus,
	Rows3,
	WrapText,
} from "lucide-react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";

export function DiffToolbarControls() {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");

	return (
		<div className="diff-toolbar flex h-10 items-center justify-center border-t bg-card/60 px-3">
			<ToolbarGroup>
				<ToolbarButton
					icon={<Columns2 aria-hidden="true" />}
					isActive={viewMode === "SideBySide"}
					label="Side by side"
					onClick={() => void setSetting("CommitDiffViewMode", "SideBySide")}
				/>
				<ToolbarButton
					icon={<Rows3 aria-hidden="true" />}
					isActive={viewMode === "Combined"}
					label="Combined"
					onClick={() => void setSetting("CommitDiffViewMode", "Combined")}
				/>
			</ToolbarGroup>
			<ToolbarGroup>
				<ToolbarButton
					icon={<ListCollapse aria-hidden="true" />}
					isActive={lineDisplayMode === "Changes"}
					label="Changes"
					onClick={() =>
						void setSetting("CommitDiffLineDisplayMode", "Changes")
					}
				/>
				<ToolbarButton
					icon={<FileText aria-hidden="true" />}
					isActive={lineDisplayMode === "FullFile"}
					label="Full file"
					onClick={() =>
						void setSetting("CommitDiffLineDisplayMode", "FullFile")
					}
				/>
			</ToolbarGroup>
			{lineDisplayMode === "Changes" ? (
				<ContextLinesControl contextLines={contextLines} />
			) : null}
			<ToolbarGroup>
				<ToolbarButton
					icon={<WrapText aria-hidden="true" />}
					isActive={wrapLines}
					label="Wrap lines"
					onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
				/>
				<ToolbarButton
					icon={<Pilcrow aria-hidden="true" />}
					isActive={ignoreWhitespace}
					label="Ignore whitespace"
					onClick={() =>
						void setSetting("CommitDiffIgnoreWhitespace", !ignoreWhitespace)
					}
				/>
			</ToolbarGroup>
		</div>
	);
}

export function clampContextLines(value: number) {
	return Math.max(0, Math.min(99, Math.trunc(value)));
}

function ContextLinesControl({ contextLines }: { contextLines: number }) {
	const updateContextLines = (value: number) => {
		void setSetting("CommitDiffContextLines", clampContextLines(value));
	};

	return (
		<div className="diff-toolbar-control ml-2 inline-flex h-8 shrink-0 items-center gap-1 rounded-md border bg-background px-2 text-xs text-muted-foreground">
			<span className="diff-toolbar-context-label">Context</span>
			<div className="ml-1 inline-flex h-6 overflow-hidden rounded border bg-card text-foreground">
				<Button
					aria-label="Decrease context lines"
					className="h-full w-6 rounded-none border-0"
					disabled={contextLines <= 0}
					onClick={() => updateContextLines(contextLines - 1)}
					size="icon-xs"
					variant="ghost"
				>
					<Minus aria-hidden="true" className="size-3" />
				</Button>
				<div className="flex min-w-7 items-center justify-center border-x px-1 font-mono text-xs">
					{contextLines}
				</div>
				<Button
					aria-label="Increase context lines"
					className="h-full w-6 rounded-none border-0"
					disabled={contextLines >= 99}
					onClick={() => updateContextLines(contextLines + 1)}
					size="icon-xs"
					variant="ghost"
				>
					<Plus aria-hidden="true" className="size-3" />
				</Button>
			</div>
		</div>
	);
}

function ToolbarGroup({ children }: { children: ReactNode }) {
	return (
		<div className="diff-toolbar-control ml-2 first:ml-0 inline-flex shrink-0 rounded-md border bg-background p-0.5">
			{children}
		</div>
	);
}

function ToolbarButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button
			aria-label={label}
			aria-pressed={isActive}
			className="diff-toolbar-button h-7 rounded px-2 text-xs"
			onClick={onClick}
			size="xs"
			title={label}
			type="button"
			variant={isActive ? "secondary" : "ghost"}
		>
			{icon}
			<span className="diff-toolbar-label">{label}</span>
		</Button>
	);
}
