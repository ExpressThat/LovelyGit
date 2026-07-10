import type { RefGroup } from "./RefCellUtils";

export type RefCellGroupView = {
	hiddenCount: number;
	visibleGroups: RefGroup[];
};

export function buildRefCellGroupView(groups: RefGroup[]): RefCellGroupView {
	if (groups.length === 0) {
		return { hiddenCount: 0, visibleGroups: [] };
	}

	return {
		hiddenCount: groups.length - 1,
		visibleGroups: [groups[0]],
	};
}
