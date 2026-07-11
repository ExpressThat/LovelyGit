import type { ReactNode } from "react";
import {
	Columns2,
	FileText,
	ListCollapse,
	Minus,
	Pilcrow,
	Plus,
	Rows3,
	WrapText,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { cn } from "@/lib/utils";

export function DiffToolbarControls({
	className,
	disabled = false,
	showViewMode = true,
}: {
	className?: string;
	disabled?: boolean;
	showViewMode?: boolean;
} = {}) {
	const viewMode = useSetting("CommitDiffViewMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const wrapLines = useSetting("CommitDiffWrapLines");
	const ignoreWhitespace = useSetting("CommitDiffIgnoreWhitespace");

	return (
		<div
			className={cn(
				"diff-toolbar custom-scrollbar flex h-10 items-center justify-start overflow-x-auto overflow-y-hidden border-t bg-card/60 px-3 lg:justify-center",
				className,
			)}
			role="toolbar"
			aria-label="Diff display controls"
		>
			{showViewMode ? (
				<ToolbarGroup>
					<ToolbarButton
						disabled={disabled}
						icon={<Columns2 aria-hidden="true" />}
						isActive={viewMode === "SideBySide"}
						label="Side by side"
						onClick={() => void setSetting("CommitDiffViewMode", "SideBySide")}
					/>
					<ToolbarButton
						disabled={disabled}
						icon={<Rows3 aria-hidden="true" />}
						isActive={viewMode === "Combined"}
						label="Combined"
						onClick={() => void setSetting("CommitDiffViewMode", "Combined")}
					/>
				</ToolbarGroup>
			) : null}
			<ToolbarGroup>
				<ToolbarButton
					disabled={disabled}
					icon={<ListCollapse aria-hidden="true" />}
					isActive={lineDisplayMode === "Changes"}
					label="Changes"
					onClick={() =>
						void setSetting("CommitDiffLineDisplayMode", "Changes")
					}
				/>
				<ToolbarButton
					disabled={disabled}
					icon={<FileText aria-hidden="true" />}
					isActive={lineDisplayMode === "FullFile"}
					label="Full file"
					onClick={() =>
						void setSetting("CommitDiffLineDisplayMode", "FullFile")
					}
				/>
			</ToolbarGroup>
			{lineDisplayMode === "Changes" ? (
				<ContextLinesControl contextLines={contextLines} disabled={disabled} />
			) : null}
			<ToolbarGroup>
				<ToolbarButton
					disabled={disabled}
					icon={<WrapText aria-hidden="true" />}
					isActive={wrapLines}
					label="Wrap lines"
					onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
				/>
				<ToolbarButton
					disabled={disabled}
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

function ContextLinesControl({
	contextLines,
	disabled,
}: {
	contextLines: number;
	disabled: boolean;
}) {
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
					disabled={disabled || contextLines <= 0}
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
					disabled={disabled || contextLines >= 99}
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
	disabled = false,
	isActive,
	label,
	onClick,
}: {
	icon: ReactNode;
	disabled?: boolean;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button
			aria-label={label}
			aria-pressed={isActive}
			className="diff-toolbar-button h-7 rounded px-2 text-xs"
			disabled={disabled}
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
