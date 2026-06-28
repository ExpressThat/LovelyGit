import {
	FileText,
	GitBranch,
	GitPullRequestArrow,
	Settings,
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
import { FileDiffViewSettings } from "./FileDiffViewSettings";
import { GraphViewSettings } from "./GraphViewSettings";
import { RemoteOperationSettings } from "./RemoteOperationSettings";

type SettingsCategory = "fileDiffView" | "graphView" | "remoteOperations";

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
	{
		description: "Commit graph layout and ref navigation",
		icon: GitBranch,
		id: "graphView",
		label: "Graph View",
	},
	{
		description: "Fetch, pull, rebase, and push defaults",
		icon: GitPullRequestArrow,
		id: "remoteOperations",
		label: "Remote Operations",
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
						{activeCategory === "fileDiffView" ? (
							<FileDiffViewSettings />
						) : null}
						{activeCategory === "graphView" ? <GraphViewSettings /> : null}
						{activeCategory === "remoteOperations" ? (
							<RemoteOperationSettings />
						) : null}
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
