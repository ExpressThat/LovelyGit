export type WorkingChangedFileRowAction = "stage" | "unstage" | "discard";

export type WorkingChangedFileRowActionOptions = {
	canDiscard: boolean;
	canStage: boolean;
	canUnstage: boolean;
	isBusy: boolean;
};

export function getWorkingChangedFileRowActions({
	canDiscard,
	canStage,
	canUnstage,
	isBusy,
}: WorkingChangedFileRowActionOptions): WorkingChangedFileRowAction[] {
	if (isBusy) {
		return [];
	}

	const actions: WorkingChangedFileRowAction[] = [];
	if (canStage) {
		actions.push("stage");
	}
	if (canUnstage) {
		actions.push("unstage");
	}
	if (canDiscard) {
		actions.push("discard");
	}

	return actions;
}
