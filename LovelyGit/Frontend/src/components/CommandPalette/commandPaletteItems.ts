import {
	FolderGit2,
	GitCompareArrows,
	Plus,
	Search,
	Settings,
} from "lucide-react";
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
	onClose: () => void;
	onOpenCommitSearch: () => void;
	onOpenSettings: () => void;
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
