import {
	Archive,
	Copy,
	ExternalLink,
	FolderGit2,
	FolderOpen,
	GitBranch,
	GitCompareArrows,
	Plus,
	RadioTower,
	RefreshCw,
	Search,
	Settings,
	SquareTerminal,
} from "@/components/icons/lovelyIcons";
import type { KnownGitRepository } from "@/generated/types";

export type PaletteItem = {
	description: string;
	disabled?: boolean;
	icon: typeof Search;
	id: string;
	keywords: string;
	label: string;
	run: () => void;
};

type ItemOptions = {
	currentRepositoryId: string | null;
	currentRepositoryPath: string | null;
	onClose: () => void;
	onCopyRepositoryPath: () => void;
	onOpenCommitSearch: () => void;
	onCreateBranch: () => void;
	onManageRemotes: () => void;
	onManageStashes: () => void;
	onOpenSettings: () => void;
	onOpenRemote: () => void;
	onOpenTerminal: () => void;
	onRefreshRepository: () => void | Promise<void>;
	onRevealRepository: () => void;
	onOpenWorkingChanges: () => void;
	repositories: KnownGitRepository[];
	setCurrentRepositoryId: (id: string | null) => Promise<void>;
};

export function createPaletteItems(options: ItemOptions): PaletteItem[] {
	const run = (action: () => void | Promise<void>) => () => {
		options.onClose();
		void action();
	};
	const needsRepository = !options.currentRepositoryId;
	return [
		{
			id: "working",
			label: "Open Working Changes",
			description: "Review, stage, and commit working tree changes",
			keywords: "status stage commit",
			icon: GitCompareArrows,
			disabled: needsRepository,
			run: run(options.onOpenWorkingChanges),
		},
		{
			id: "search",
			label: "Search Commits",
			description: "Search reachable commit history",
			keywords: "find history",
			icon: Search,
			disabled: needsRepository,
			run: run(options.onOpenCommitSearch),
		},
		{
			id: "refresh",
			label: "Refresh Repository",
			description: "Reload the graph and working tree from disk",
			keywords: "reload rescan status graph",
			icon: RefreshCw,
			disabled: needsRepository,
			run: run(options.onRefreshRepository),
		},
		{
			id: "copy-path",
			label: "Copy Repository Path",
			description: "Copy the active worktree path to the clipboard",
			keywords: "folder directory location clipboard",
			icon: Copy,
			disabled: !options.currentRepositoryPath,
			run: run(options.onCopyRepositoryPath),
		},
		{
			id: "reveal",
			label: "Show Repository in File Explorer",
			description: "Reveal the active worktree in its containing folder",
			keywords: "open folder directory reveal",
			icon: FolderOpen,
			disabled: needsRepository,
			run: run(options.onRevealRepository),
		},
		{
			id: "create-branch",
			label: "Create Branch",
			description: "Create a branch from the current commit",
			keywords: "new checkout ref",
			icon: GitBranch,
			disabled: needsRepository,
			run: run(options.onCreateBranch),
		},
		{
			id: "manage-remotes",
			label: "Manage Remotes",
			description: "Add, edit, or remove repository remotes",
			keywords: "origin url fetch push",
			icon: RadioTower,
			disabled: needsRepository,
			run: run(options.onManageRemotes),
		},
		{
			id: "manage-stashes",
			label: "Manage Stashes",
			description: "Save work or apply, pop, and delete existing stashes",
			keywords: "wip shelf checkpoint apply pop",
			icon: Archive,
			disabled: needsRepository,
			run: run(options.onManageStashes),
		},
		{
			id: "terminal",
			label: "Open Repository in Terminal",
			description: "Start your configured terminal in this worktree",
			keywords: "shell command prompt console",
			icon: SquareTerminal,
			disabled: needsRepository,
			run: run(options.onOpenTerminal),
		},
		{
			id: "remote-web",
			label: "Open Repository on Remote",
			description: "Open the repository in its hosting service",
			keywords: "github gitlab bitbucket browser website",
			icon: ExternalLink,
			disabled: needsRepository,
			run: run(options.onOpenRemote),
		},
		{
			id: "settings",
			label: "Open Settings",
			description: "Appearance, Git, diff, graph, and remote preferences",
			keywords: "preferences theme",
			icon: Settings,
			run: run(options.onOpenSettings),
		},
		{
			id: "new-tab",
			label: "Open New Tab",
			description: "Open, clone, or initialize another repository",
			keywords: "repository clone init",
			icon: Plus,
			run: run(() => options.setCurrentRepositoryId(null)),
		},
		...options.repositories.map((repository) => ({
			id: `repo:${repository.id}`,
			label: `Switch to ${repository.name || "repository"}`,
			description: repository.path ?? "",
			keywords: "repository tab open",
			icon: FolderGit2,
			run: run(() => options.setCurrentRepositoryId(repository.id)),
		})),
	];
}

export function filterPaletteItems(items: PaletteItem[], query: string) {
	const terms = query.toLocaleLowerCase().trim().split(/\s+/).filter(Boolean);
	return terms.length === 0
		? items
		: items.filter((item) => {
				const text =
					`${item.label} ${item.description} ${item.keywords}`.toLocaleLowerCase();
				return terms.every((term) => text.includes(term));
			});
}

export function nextEnabledItem(
	items: PaletteItem[],
	index: number,
	direction: 1 | -1,
) {
	if (items.length === 0) return 0;
	for (let offset = 1; offset <= items.length; offset++) {
		const candidate =
			(index + direction * offset + items.length) % items.length;
		if (!items[candidate]?.disabled) return candidate;
	}
	return index;
}
