import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";

export function applyOptimisticDiscard(
	current: WorkingTreeChangesResponse,
	files: WorkingTreeChangedFile[],
) {
	if (files.length === 0) return current;
	const paths = new Set(files.map((file) => file.path));
	const unstaged = current.unstaged.filter((file) => !paths.has(file.path));
	const untracked = current.untracked.filter((file) => !paths.has(file.path));
	return {
		...current,
		totalCount:
			current.staged.length +
			unstaged.length +
			untracked.length +
			current.unmerged.length,
		unstaged,
		untracked,
	};
}
