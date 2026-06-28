import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";

export type WorkingChangesFilterGroup =
	| "All"
	| "Staged"
	| "Changes"
	| "Unmerged";

export type WorkingChangesFilterState = {
	group: WorkingChangesFilterGroup;
	query: string;
};

export function filterWorkingChanges(
	changes: WorkingTreeChangesResponse,
	filter: WorkingChangesFilterState,
): WorkingTreeChangesResponse {
	const matcher = createMatcher(filter.query);
	const include = (file: WorkingTreeChangedFile) => matcher(file);
	const staged =
		filter.group === "All" || filter.group === "Staged"
			? changes.staged.filter(include)
			: [];
	const unstaged =
		filter.group === "All" || filter.group === "Changes"
			? changes.unstaged.filter(include)
			: [];
	const unmerged =
		filter.group === "All" || filter.group === "Unmerged"
			? changes.unmerged.filter(include)
			: [];
	const untracked =
		filter.group === "All" || filter.group === "Changes"
			? changes.untracked.filter(include)
			: [];

	return {
		staged,
		unstaged,
		unmerged,
		untracked,
		totalCount:
			staged.length + unstaged.length + untracked.length + unmerged.length,
	};
}

export function countWorkingChanges(changes: WorkingTreeChangesResponse) {
	return (
		changes.staged.length +
		changes.unstaged.length +
		changes.untracked.length +
		changes.unmerged.length
	);
}

function createMatcher(query: string) {
	const terms = query.trim().toLocaleLowerCase().split(/\s+/).filter(Boolean);

	if (terms.length === 0) {
		return () => true;
	}

	return (file: WorkingTreeChangedFile) => {
		const haystack = `${file.path} ${file.oldPath ?? ""} ${file.status} ${
			file.group
		}`.toLocaleLowerCase();
		return terms.every((term) => haystack.includes(term));
	};
}
