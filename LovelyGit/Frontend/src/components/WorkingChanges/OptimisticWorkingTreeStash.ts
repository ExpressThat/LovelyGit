import type { WorkingTreeChangesResponse } from "@/generated/types";

export function applyOptimisticStash(
	current: WorkingTreeChangesResponse,
	selectedOnly: boolean,
	selectedPaths: string[],
	includeUntracked: boolean,
) {
	const paths = selectedOnly ? new Set(selectedPaths) : null;
	const staged = paths
		? current.staged.filter((file) => !paths.has(file.path))
		: [];
	const unstaged = paths
		? current.unstaged.filter((file) => !paths.has(file.path))
		: [];
	const untracked = includeUntracked
		? paths
			? current.untracked.filter((file) => !paths.has(file.path))
			: []
		: current.untracked;
	return {
		...current,
		staged,
		totalCount:
			staged.length +
			unstaged.length +
			untracked.length +
			current.unmerged.length,
		unstaged,
		untracked,
	};
}
