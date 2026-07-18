import { useEffect, useMemo, useState } from "react";
import { copyToClipboard } from "@/components/CommitGraph/utils/clipboard";
import { Search } from "@/components/icons/lovelyIcons";
import {
	openRemoteWebResource,
	openRepositoryTerminal,
	revealKnownRepository,
} from "@/components/TopNavBar/components/RepositoryCommands";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { CommandPaletteList } from "./CommandPaletteList";
import {
	createPaletteItems,
	filterPaletteItems,
	nextEnabledItem,
} from "./commandPaletteItems";

export function CommandPalette({
	onOpenChange,
	onOpenCommitSearch,
	onCreateBranch,
	onManageRemotes,
	onManageStashes,
	onOpenSettings,
	onOpenWorkingChanges,
	onRefreshRepository,
	open,
}: {
	onOpenChange: (open: boolean) => void;
	onOpenCommitSearch: () => void;
	onCreateBranch: () => void;
	onManageRemotes: () => void;
	onManageStashes: () => void;
	onOpenSettings: () => void;
	onOpenWorkingChanges: () => void;
	onRefreshRepository: () => void | Promise<void>;
	open: boolean;
}) {
	const {
		currentRepository,
		currentRepositoryId,
		repositories,
		setCurrentRepositoryId,
	} = useRepositoryContext();
	const [query, setQuery] = useState("");
	const [activeIndex, setActiveIndex] = useState(0);
	const items = useMemo(
		() =>
			createPaletteItems({
				currentRepositoryId,
				currentRepositoryPath: currentRepository?.path ?? null,
				onClose: () => onOpenChange(false),
				onCopyRepositoryPath: () => {
					if (currentRepository?.path)
						void copyToClipboard(currentRepository.path, "Repository path");
				},
				onCreateBranch,
				onManageRemotes,
				onManageStashes,
				onOpenCommitSearch,
				onOpenSettings,
				onOpenRemote: () => {
					if (currentRepositoryId)
						void openRemoteWebResource(currentRepositoryId, "Repository");
				},
				onOpenTerminal: () => {
					if (currentRepositoryId)
						void openRepositoryTerminal(currentRepositoryId);
				},
				onOpenWorkingChanges,
				onRefreshRepository,
				onRevealRepository: () => {
					if (currentRepositoryId)
						void revealKnownRepository(currentRepositoryId);
				},
				repositories,
				setCurrentRepositoryId,
			}),
		[
			currentRepository,
			currentRepositoryId,
			onOpenChange,
			onCreateBranch,
			onManageRemotes,
			onManageStashes,
			onOpenCommitSearch,
			onOpenSettings,
			onOpenWorkingChanges,
			onRefreshRepository,
			repositories,
			setCurrentRepositoryId,
		],
	);
	const filtered = useMemo(
		() => filterPaletteItems(items, query),
		[items, query],
	);
	useEffect(() => {
		if (!open) return;
		setQuery("");
		setActiveIndex(0);
	}, [open]);
	const runActive = () => {
		const item = filtered[activeIndex];
		if (item && !item.disabled) item.run();
	};

	return (
		<Dialog onOpenChange={onOpenChange} open={open}>
			<DialogContent
				className="top-[18%] max-w-xl translate-y-0 gap-0 overflow-hidden p-0 sm:max-w-xl"
				showCloseButton={false}
			>
				<DialogHeader className="sr-only">
					<DialogTitle>Command palette</DialogTitle>
					<DialogDescription>Search actions and repositories</DialogDescription>
				</DialogHeader>
				<div className="flex items-center gap-2 border-b px-4">
					<Search aria-hidden="true" className="size-4 text-muted-foreground" />
					<Input
						aria-label="Search commands"
						autoFocus
						className="h-12 border-0 bg-transparent px-0 text-sm shadow-none focus-visible:ring-0 dark:bg-transparent"
						onInput={(event) => {
							setQuery(event.currentTarget.value);
							setActiveIndex(0);
						}}
						onKeyDown={(event) => {
							if (event.key === "ArrowDown") {
								event.preventDefault();
								setActiveIndex((index) => nextEnabledItem(filtered, index, 1));
							}
							if (event.key === "ArrowUp") {
								event.preventDefault();
								setActiveIndex((index) => nextEnabledItem(filtered, index, -1));
							}
							if (event.key === "Enter") {
								event.preventDefault();
								runActive();
							}
						}}
						placeholder="Type a command or repository…"
						value={query}
					/>
					<kbd className="rounded border bg-muted px-1.5 py-0.5 font-mono text-[10px] text-muted-foreground">
						Esc
					</kbd>
				</div>
				<div className="p-2">
					<CommandPaletteList
						activeIndex={activeIndex}
						items={filtered}
						onActiveIndexChange={setActiveIndex}
					/>
				</div>
				<footer className="flex items-center gap-3 border-t bg-card/60 px-4 py-2 text-[10px] text-muted-foreground">
					<span>↑↓ Navigate</span>
					<span>↵ Run</span>
					<span>Esc Close</span>
				</footer>
			</DialogContent>
		</Dialog>
	);
}
