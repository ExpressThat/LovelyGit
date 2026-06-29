import type { WorkingTreeChangedFile } from "@/generated/types";
import { fileKey, selectedFiles } from "./WorkingChangesPanelParts";

export type WorkingChangesGroupConfig = {
	actionLabel?: string;
	destructiveActionLabel?: string;
	files: WorkingTreeChangedFile[];
	hideGroupLabel?: boolean;
	isActionDisabled?: boolean;
	isDestructiveActionDisabled?: boolean;
	onAction?: () => void;
	onDestructiveAction?: () => void;
	onFileAction?: (file: WorkingTreeChangedFile) => void;
	onFileDestructiveAction?: (file: WorkingTreeChangedFile) => void;
	onToggleSelected?: (file: WorkingTreeChangedFile) => void;
	title: string;
};

export type WorkingChangesVirtualRow =
	| { group: WorkingChangesGroupConfig; id: string; type: "header" }
	| {
			file: WorkingTreeChangedFile;
			group: WorkingChangesGroupConfig;
			id: string;
			type: "file";
	  };

export function buildWorkingChangesGroups({
	isBusy,
	onDiscardSelected,
	onIndexCommand,
	onToggleSelected,
	selectedKeys,
	stagedFiles,
	unmergedFiles,
	workingFiles,
}: {
	isBusy: boolean;
	onDiscardSelected: (files: WorkingTreeChangedFile[]) => void;
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void;
	onToggleSelected: (file: WorkingTreeChangedFile) => void;
	selectedKeys: Set<string>;
	stagedFiles: WorkingTreeChangedFile[];
	unmergedFiles: WorkingTreeChangedFile[];
	workingFiles: WorkingTreeChangedFile[];
}): WorkingChangesGroupConfig[] {
	const selectedStagedFiles = selectedFiles(stagedFiles, selectedKeys);
	const selectedWorkingFiles = selectedFiles(workingFiles, selectedKeys);
	return [
		{
			actionLabel: selectedStagedFiles.length
				? `Unstage selected (${selectedStagedFiles.length})`
				: "Unstage selected",
			files: stagedFiles,
			isActionDisabled: isBusy || selectedStagedFiles.length === 0,
			onAction: () =>
				onIndexCommand("UnstageWorkingTreeFiles", selectedStagedFiles, false),
			onFileAction: (file) =>
				onIndexCommand("UnstageWorkingTreeFiles", [file], false),
			onToggleSelected,
			title: "Staged",
		},
		{
			actionLabel: selectedWorkingFiles.length
				? `Stage selected (${selectedWorkingFiles.length})`
				: "Stage selected",
			destructiveActionLabel: selectedWorkingFiles.length
				? `Discard selected (${selectedWorkingFiles.length})`
				: "Discard selected",
			files: workingFiles,
			hideGroupLabel: true,
			isActionDisabled: isBusy || selectedWorkingFiles.length === 0,
			isDestructiveActionDisabled: isBusy || selectedWorkingFiles.length === 0,
			onAction: () =>
				onIndexCommand("StageWorkingTreeFiles", selectedWorkingFiles, false),
			onDestructiveAction: () => onDiscardSelected(selectedWorkingFiles),
			onFileAction: (file) =>
				onIndexCommand("StageWorkingTreeFiles", [file], false),
			onFileDestructiveAction: (file) => onDiscardSelected([file]),
			onToggleSelected,
			title: "Changes",
		},
		{ files: unmergedFiles, title: "Unmerged" },
	];
}

export function buildWorkingChangesVirtualRows(
	groups: WorkingChangesGroupConfig[],
): WorkingChangesVirtualRow[] {
	return groups.flatMap((group) =>
		group.files.length === 0
			? []
			: [
					{ group, id: `${group.title}:header`, type: "header" as const },
					...group.files.map((file) => ({
						file,
						group,
						id: fileKey(file),
						type: "file" as const,
					})),
				],
	);
}
