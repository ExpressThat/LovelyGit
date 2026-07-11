import { Search } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { useEffect, useMemo, useState } from "react";
import {
	openRemoteWebResource,
	openRepositoryTerminal,
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
import { cn } from "@/lib/utils";
import {
	createPaletteItems,
	filterPaletteItems,
	nextEnabledItem,
	type PaletteItem,
} from "./commandPaletteItems";

export function CommandPalette({
	onOpenChange,
	onOpenCommitSearch,
	onCreateBranch,
	onManageRemotes,
	onOpenSettings,
	onOpenWorkingChanges,
	onRefreshRepository,
	open,
}: {
	onOpenChange: (open: boolean) => void;
	onOpenCommitSearch: () => void;
	onCreateBranch: () => void;
	onManageRemotes: () => void;
	onOpenSettings: () => void;
	onOpenWorkingChanges: () => void;
	onRefreshRepository: () => void | Promise<void>;
	open: boolean;
}) {
	const { currentRepositoryId, repositories, setCurrentRepositoryId } =
		useRepositoryContext();
	const [query, setQuery] = useState("");
	const [activeIndex, setActiveIndex] = useState(0);
	const items = useMemo(
		() =>
			createPaletteItems({
				currentRepositoryId,
				onClose: () => onOpenChange(false),
				onCreateBranch,
				onManageRemotes,
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
				repositories,
				setCurrentRepositoryId,
			}),
		[
			currentRepositoryId,
			onOpenChange,
			onCreateBranch,
			onManageRemotes,
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
				<div className="custom-scrollbar max-h-[min(420px,60vh)] overflow-y-auto p-2">
					{filtered.length ? (
						filtered.map((item, index) => (
							<PaletteRow
								active={activeIndex === index}
								item={item}
								key={item.id}
								onHover={() => setActiveIndex(index)}
							/>
						))
					) : (
						<div className="px-3 py-8 text-center text-sm text-muted-foreground">
							No matching commands
						</div>
					)}
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

function PaletteRow({
	active,
	item,
	onHover,
}: {
	active: boolean;
	item: PaletteItem;
	onHover: () => void;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<motion.button
			aria-disabled={item.disabled}
			className={cn(
				"relative flex w-full items-center gap-3 rounded-lg px-3 py-2 text-left",
				active && "bg-accent text-accent-foreground",
				item.disabled && "opacity-40",
			)}
			disabled={item.disabled}
			onClick={item.run}
			onMouseEnter={onHover}
			type="button"
			whileTap={reduceMotion ? undefined : { scale: 0.99 }}
		>
			<item.icon
				aria-hidden="true"
				className="size-4 shrink-0 text-muted-foreground"
			/>
			<span className="min-w-0">
				<span className="block truncate text-sm font-medium">{item.label}</span>
				<span className="block truncate text-xs text-muted-foreground">
					{item.description}
				</span>
			</span>
		</motion.button>
	);
}
