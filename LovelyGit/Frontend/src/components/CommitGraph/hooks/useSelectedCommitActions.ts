import type { useCommitGraphDialogs } from "./useCommitGraphDialogs";
import type { useCommitMultiSelection } from "./useCommitMultiSelection";
import type { useCommitPatchActions } from "./useCommitPatchActions";

export function createSelectedCommitActions({
	dialogs,
	patchActions,
	selection,
}: {
	dialogs: ReturnType<typeof useCommitGraphDialogs>;
	patchActions: ReturnType<typeof useCommitPatchActions>;
	selection: ReturnType<typeof useCommitMultiSelection>;
}) {
	const openOperation = (mode: "cherry-pick" | "revert") => {
		const commits = selection.ordered(mode);
		if (commits.length === 0) return;
		if (mode === "cherry-pick") dialogs.setCherryPickCommits(commits);
		else dialogs.setRevertCommits(commits);
		selection.clear();
	};
	const openComparison = () => {
		const comparison = selection.comparison();
		if (!comparison) return;
		dialogs.comparison.setBase(comparison.base);
		dialogs.comparison.setTarget(comparison.target);
		selection.clear();
	};
	const runPatchSeries = (mode: "copy" | "save") => {
		const commits = selection.ordered("cherry-pick");
		const action =
			mode === "copy"
				? patchActions.copyPatchSeries(commits)
				: patchActions.savePatchSeries(commits);
		void action.then((completed) => {
			if (completed) selection.clear();
		});
	};

	return { openComparison, openOperation, runPatchSeries };
}
