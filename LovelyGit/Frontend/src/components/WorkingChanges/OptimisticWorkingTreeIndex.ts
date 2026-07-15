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
	if (!includeAll && selected.length === 1) {
		return applySinglePathMutation(current, command, selected[0]);
	}

	const paths = new Set<string>();
	for (const file of selected) paths.add(file.path);
	const workingByPath =
		command === "unstage" ? indexWorkingFiles(current) : undefined;
	const next = {
		isComplete: current.isComplete,
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

		const existingWorking = workingByPath?.get(file.path);
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

function applySinglePathMutation(
	current: WorkingTreeChangesResponse,
	command: OptimisticIndexCommand,
	file: WorkingTreeChangedFile,
) {
	const path = file.path;
	let staged = removePath(current.staged, path);
	let unstaged = removePath(current.unstaged, path);
	let untracked = removePath(current.untracked, path);

	if (command === "stage") {
		staged = insertSorted(staged, { ...file, group: "Staged" });
	} else {
		const existingWorking = findWorkingFile(current, path);
		if (file.status === "Added") {
			untracked = insertSorted(untracked, {
				...file,
				group: "Untracked",
				status: "Added",
			});
		} else {
			unstaged = insertSorted(unstaged, {
				...(existingWorking ?? file),
				group: "Unstaged",
			});
		}
	}

	return {
		isComplete: current.isComplete,
		staged,
		unstaged,
		untracked,
		unmerged: current.unmerged,
		totalCount:
			staged.length +
			unstaged.length +
			untracked.length +
			current.unmerged.length,
	};
}

function removePath(files: WorkingTreeChangedFile[], path: string) {
	const index = files.findIndex((file) => file.path === path);
	if (index < 0) return files;
	return [...files.slice(0, index), ...files.slice(index + 1)];
}

function insertSorted(
	files: WorkingTreeChangedFile[],
	file: WorkingTreeChangedFile,
) {
	const index = lowerBound(files, file.path);
	return [...files.slice(0, index), file, ...files.slice(index)];
}

function lowerBound(files: WorkingTreeChangedFile[], path: string) {
	let low = 0;
	let high = files.length;
	while (low < high) {
		const middle = (low + high) >>> 1;
		if ((files[middle]?.path ?? "") < path) low = middle + 1;
		else high = middle;
	}
	return low;
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
	const byPath = new Map<string, WorkingTreeChangedFile>();
	for (const file of files) byPath.set(file.path, file);
	return [...byPath.values()];
}

function indexWorkingFiles(current: WorkingTreeChangesResponse) {
	const byPath = new Map<string, WorkingTreeChangedFile>();
	for (const file of current.untracked) byPath.set(file.path, file);
	for (const file of current.unstaged) byPath.set(file.path, file);
	return byPath;
}

function findWorkingFile(current: WorkingTreeChangesResponse, path: string) {
	return (
		current.unstaged.find((file) => file.path === path) ??
		current.untracked.find((file) => file.path === path)
	);
}

function sort(files: WorkingTreeChangedFile[]) {
	files.sort((left, right) => left.path.localeCompare(right.path));
}
