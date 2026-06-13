import {
	Columns2,
	FileText,
	ListCollapse,
	Minus,
	Plus,
	Rows3,
	Settings,
	WrapText,
} from "lucide-react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import type { CommitDiffViewMode } from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import type { CommitDiffLineDisplayMode } from "@/generated/ExpressThat.LovelyGit.Services.Settings";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";

type SettingsCategory = "fileDiffView";

const categories: Array<{
	description: string;
	icon: typeof FileText;
	id: SettingsCategory;
	label: string;
}> = [
	{
		description: "Diff layout, context, and line wrapping",
		icon: FileText,
		id: "fileDiffView",
		label: "File Diff View",
	},
];

export function SettingsDialog() {
	const [open, setOpen] = useState(false);
	const [activeCategory, setActiveCategory] =
		useState<SettingsCategory>("fileDiffView");
	const active = categories.find((category) => category.id === activeCategory);

	return (
		<Dialog open={open} onOpenChange={setOpen}>
			<DialogTrigger
				render={
					<Button
						aria-label="Open settings"
						size="icon-sm"
						title="Settings"
						variant="ghost"
					/>
				}
			>
				<Settings aria-hidden="true" className="size-4" />
			</DialogTrigger>
			<DialogContent className="grid h-[min(560px,calc(100vh-2rem))] max-w-[min(920px,calc(100vw-2rem))] grid-rows-[auto_minmax(0,1fr)] gap-0 overflow-hidden p-0 sm:max-w-[min(920px,calc(100vw-2rem))]">
				<DialogHeader className="border-b px-5 py-4">
					<DialogTitle>Settings</DialogTitle>
					<DialogDescription>
						{active?.description ?? "Application preferences"}
					</DialogDescription>
				</DialogHeader>
				<div className="grid min-h-0 grid-cols-[220px_minmax(0,1fr)]">
					<nav className="border-r bg-card/50 p-2">
						{categories.map((category) => (
							<CategoryButton
								category={category}
								isActive={activeCategory === category.id}
								key={category.id}
								onClick={() => setActiveCategory(category.id)}
							/>
						))}
					</nav>
					<section className="custom-scrollbar min-h-0 overflow-y-auto p-5">
						{activeCategory === "fileDiffView" ? <FileDiffViewSettings /> : null}
					</section>
				</div>
			</DialogContent>
		</Dialog>
	);
}

function CategoryButton({
	category,
	isActive,
	onClick,
}: {
	category: (typeof categories)[number];
	isActive: boolean;
	onClick: () => void;
}) {
	return (
		<Button
			className="mb-1 h-auto w-full justify-start gap-2 px-2 py-2 text-left"
			onClick={onClick}
			variant={isActive ? "secondary" : "ghost"}
		>
			<category.icon aria-hidden="true" className="size-4" />
			<span className="min-w-0">
				<span className="block truncate text-sm font-medium">
					{category.label}
				</span>
				<span className="block truncate text-xs text-muted-foreground">
					{category.description}
				</span>
			</span>
		</Button>
	);
}

function FileDiffViewSettings() {
	const viewMode = useSetting("CommitDiffViewMode");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const wrapLines = useSetting("CommitDiffWrapLines");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose how file changes are arranged."
				title="Layout"
			>
				<SegmentedControl>
					<SegmentedButton
						icon={<Columns2 aria-hidden="true" className="size-4" />}
						isActive={viewMode === "SideBySide"}
						label="Side by side"
						onClick={() =>
							void setSetting(
								"CommitDiffViewMode",
								"SideBySide" satisfies CommitDiffViewMode,
							)
						}
					/>
					<SegmentedButton
						icon={<Rows3 aria-hidden="true" className="size-4" />}
						isActive={viewMode === "Combined"}
						label="Combined"
						onClick={() =>
							void setSetting(
								"CommitDiffViewMode",
								"Combined" satisfies CommitDiffViewMode,
							)
						}
					/>
				</SegmentedControl>
			</SettingGroup>

			<SettingGroup
				description="Switch between changed hunks and the whole file."
				title="Line Display"
			>
				<SegmentedControl>
					<SegmentedButton
						icon={<ListCollapse aria-hidden="true" className="size-4" />}
						isActive={lineDisplayMode === "Changes"}
						label="Changes"
						onClick={() =>
							void setSetting(
								"CommitDiffLineDisplayMode",
								"Changes" satisfies CommitDiffLineDisplayMode,
							)
						}
					/>
					<SegmentedButton
						icon={<FileText aria-hidden="true" className="size-4" />}
						isActive={lineDisplayMode === "FullFile"}
						label="Full file"
						onClick={() =>
							void setSetting(
								"CommitDiffLineDisplayMode",
								"FullFile" satisfies CommitDiffLineDisplayMode,
							)
						}
					/>
				</SegmentedControl>
			</SettingGroup>

			<SettingGroup
				description="Set how many unchanged lines surround each change."
				title="Context Lines"
			>
				<div className="inline-flex h-9 overflow-hidden rounded-lg border bg-background">
					<Button
						aria-label="Decrease context lines"
						className="h-full rounded-none border-0"
						disabled={contextLines <= 0}
						onClick={() => updateContextLines(contextLines - 1)}
						size="icon-sm"
						variant="ghost"
					>
						<Minus aria-hidden="true" className="size-4" />
					</Button>
					<div className="flex min-w-12 items-center justify-center border-x px-3 font-mono text-sm">
						{contextLines}
					</div>
					<Button
						aria-label="Increase context lines"
						className="h-full rounded-none border-0"
						disabled={contextLines >= 99}
						onClick={() => updateContextLines(contextLines + 1)}
						size="icon-sm"
						variant="ghost"
					>
						<Plus aria-hidden="true" className="size-4" />
					</Button>
				</div>
			</SettingGroup>

			<SettingGroup
				description="Wrap long diff lines inside the viewport."
				title="Line Wrapping"
			>
				<Button
					onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
					variant={wrapLines ? "secondary" : "outline"}
				>
					<WrapText aria-hidden="true" className="size-4" />
					{wrapLines ? "Wrapping on" : "Wrapping off"}
				</Button>
			</SettingGroup>
		</div>
	);
}

function SettingGroup({
	children,
	description,
	title,
}: {
	children: React.ReactNode;
	description: string;
	title: string;
}) {
	return (
		<section className="grid gap-3 border-b pb-5 last:border-b-0 last:pb-0">
			<div>
				<h3 className="text-sm font-semibold">{title}</h3>
				<p className="text-xs text-muted-foreground">{description}</p>
			</div>
			{children}
		</section>
	);
}

function SegmentedControl({ children }: { children: React.ReactNode }) {
	return (
		<div className="inline-flex rounded-lg border bg-background p-0.5">
			{children}
		</div>
	);
}

function SegmentedButton({
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
		<Button
			className="rounded-md"
			onClick={onClick}
			variant={isActive ? "secondary" : "ghost"}
		>
			{icon}
			{label}
		</Button>
	);
}

function updateContextLines(value: number) {
	const nextValue = Math.max(0, Math.min(99, Math.trunc(value)));
	void setSetting("CommitDiffContextLines", nextValue);
}
