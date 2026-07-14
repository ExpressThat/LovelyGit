import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";

export type OptimisticIndexCommand = "stage" | "unstage";

export function applyOptimisticIndexMutation(
	current: WorkingTreeChangesResponse,
	command: OptimisticIndexCommand,
	files: WorkingTreeChangedFile[],
	includeAll: boolean,
) {
	const candidates = includeAll ? allCandidates(current, command) : files;
	const selected = uniqueByPath(candidates);
	if (selected.length === 0) return current;

	const paths = new Set(selected.map((file) => file.path));
	const next = {
		staged: current.staged.filter((file) => !paths.has(file.path)),
		unstaged: current.unstaged.filter((file) => !paths.has(file.path)),
		untracked: current.untracked.filter((file) => !paths.has(file.path)),
		unmerged: [...current.unmerged],
		totalCount: 0,
	};
	for (const file of selected) {
		if (command === "stage") {
			next.staged.push({ ...file, group: "Staged" });
			continue;
		}

		const existingWorking = findWorkingFile(current, file.path);
		if (file.status === "Added") {
			next.untracked.push({ ...file, group: "Untracked", status: "Added" });
		} else {
			next.unstaged.push({
				...(existingWorking ?? file),
				group: "Unstaged",
			});
		}
	}

	sort(next.staged);
	sort(next.unstaged);
	sort(next.untracked);
	next.totalCount =
		next.staged.length +
		next.unstaged.length +
		next.untracked.length +
		next.unmerged.length;
	return next;
}

function allCandidates(
	current: WorkingTreeChangesResponse,
	command: OptimisticIndexCommand,
) {
	return command === "stage"
		? [...current.unstaged, ...current.untracked]
		: current.staged;
}

function uniqueByPath(files: WorkingTreeChangedFile[]) {
	return [...new Map(files.map((file) => [file.path, file])).values()];
}

function findWorkingFile(current: WorkingTreeChangesResponse, path: string) {
	return [...current.unstaged, ...current.untracked].find(
		(file) => file.path === path,
	);
}

function sort(files: WorkingTreeChangedFile[]) {
	files.sort((left, right) => left.path.localeCompare(right.path));
}
