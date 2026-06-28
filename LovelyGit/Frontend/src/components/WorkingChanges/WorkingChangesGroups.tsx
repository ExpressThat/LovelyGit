import type { WorkingTreeChangedFile } from "@/generated/types";
import { ChangeGroup, selectedFiles } from "./WorkingChangesPanelParts";

export function WorkingChangesGroups({
	isBusy,
	onDiscardSelected,
	onIndexCommand,
	onSelectFile,
	onToggleSelected,
	selectedKeys,
	stagedFiles,
	workingFiles,
	unmergedFiles,
}: {
	isBusy: boolean;
	onDiscardSelected: (files: WorkingTreeChangedFile[]) => void;
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected: (file: WorkingTreeChangedFile) => void;
	selectedKeys: Set<string>;
	stagedFiles: WorkingTreeChangedFile[];
	workingFiles: WorkingTreeChangedFile[];
	unmergedFiles: WorkingTreeChangedFile[];
}) {
	const selectedStagedFiles = selectedFiles(stagedFiles, selectedKeys);
	const selectedWorkingFiles = selectedFiles(workingFiles, selectedKeys);

	return (
		<>
			<ChangeGroup
				actionLabel={
					selectedStagedFiles.length > 0
						? `Unstage selected (${selectedStagedFiles.length})`
						: "Unstage selected"
				}
				files={stagedFiles}
				isActionDisabled={isBusy || selectedStagedFiles.length === 0}
				isBusy={isBusy}
				onAction={() =>
					onIndexCommand("UnstageWorkingTreeFiles", selectedStagedFiles, false)
				}
				onFileAction={(file) =>
					onIndexCommand("UnstageWorkingTreeFiles", [file], false)
				}
				onSelectFile={onSelectFile}
				onToggleSelected={onToggleSelected}
				selectedKeys={selectedKeys}
				title="Staged"
			/>
			<ChangeGroup
				actionLabel={
					selectedWorkingFiles.length > 0
						? `Stage selected (${selectedWorkingFiles.length})`
						: "Stage selected"
				}
				destructiveActionLabel={
					selectedWorkingFiles.length > 0
						? `Discard selected (${selectedWorkingFiles.length})`
						: "Discard selected"
				}
				files={workingFiles}
				hideGroupLabel
				isActionDisabled={isBusy || selectedWorkingFiles.length === 0}
				isBusy={isBusy}
				isDestructiveActionDisabled={
					isBusy || selectedWorkingFiles.length === 0
				}
				onAction={() =>
					onIndexCommand("StageWorkingTreeFiles", selectedWorkingFiles, false)
				}
				onDestructiveAction={() => onDiscardSelected(selectedWorkingFiles)}
				onFileDestructiveAction={(file) => onDiscardSelected([file])}
				onFileAction={(file) =>
					onIndexCommand("StageWorkingTreeFiles", [file], false)
				}
				onSelectFile={onSelectFile}
				onToggleSelected={onToggleSelected}
				selectedKeys={selectedKeys}
				title="Changes"
			/>
			<ChangeGroup
				files={unmergedFiles}
				isBusy={isBusy}
				onSelectFile={onSelectFile}
				title="Unmerged"
			/>
		</>
	);
}
