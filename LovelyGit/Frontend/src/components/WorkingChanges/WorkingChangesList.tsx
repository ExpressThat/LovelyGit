import { MinusSquare, SquareCheckBig, Trash2 } from "lucide-react";
import { useMemo } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { ActionButton, selectedFiles } from "./WorkingChangesPanelParts";
import { WorkingChangesSection } from "./WorkingChangesSection";

export function splitWorkingChanges(changes: WorkingTreeChangesResponse) {
	return {
		stagedFiles: changes.staged,
		unstagedFiles: [
			...changes.unstaged,
			...changes.untracked,
			...changes.unmerged,
		],
	};
}

export function countWorkingChanges(files: WorkingTreeChangedFile[]) {
	return files.length;
}

export function workingFilesOnly(files: WorkingTreeChangedFile[]) {
	return files.filter(
		(file) => file.group === "Unstaged" || file.group === "Untracked",
	);
}

export function stagedFilesOnly(files: WorkingTreeChangedFile[]) {
	return files.filter((file) => file.group === "Staged");
}

export function flattenWorkingChangesForTests(
	changes: WorkingTreeChangesResponse,
) {
	return [
		...changes.unstaged,
		...changes.untracked,
		...changes.unmerged,
		...changes.staged,
	];
}

export function WorkingChangesList({
	isBusy,
	isLoading,
	onDiscardAll,
	onDiscardSelected,
	onIndexCommand,
	onOpenFileBlame,
	onOpenFileHistory,
	onIgnorePath,
	onSelectFile,
	onToggleSelected,
	selectedKeys,
	stagedFiles,
	unstagedFiles,
	workingFiles,
}: {
	isBusy: boolean;
	isLoading: boolean;
	onDiscardAll: () => void;
	onDiscardSelected: (files: WorkingTreeChangedFile[]) => void;
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void;
	onOpenFileBlame: (file: WorkingTreeChangedFile) => void;
	onOpenFileHistory: (file: WorkingTreeChangedFile) => void;
	onIgnorePath: (
		file: WorkingTreeChangedFile,
		target: "Local" | "Shared",
	) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected: (file: WorkingTreeChangedFile) => void;
	selectedKeys: Set<string>;
	stagedFiles: WorkingTreeChangedFile[];
	unstagedFiles: WorkingTreeChangedFile[];
	workingFiles: WorkingTreeChangedFile[];
}) {
	const selectedUnstaged = useMemo(
		() => selectedFiles(unstagedFiles, selectedKeys),
		[unstagedFiles, selectedKeys],
	);
	const selectedWorking = workingFilesOnly(selectedUnstaged);
	const selectedStaged = useMemo(
		() => selectedFiles(stagedFiles, selectedKeys),
		[stagedFiles, selectedKeys],
	);

	return (
		<div className="flex h-full min-h-0 flex-col gap-3">
			<WorkingChangesSection
				actions={
					<>
						<ActionButton
							disabled={isBusy || workingFiles.length === 0}
							icon={<SquareCheckBig aria-hidden="true" size={14} />}
							label="Stage all changes"
							onClick={() => onIndexCommand("StageWorkingTreeFiles", [], true)}
						/>
						<ActionButton
							disabled={isBusy || workingFiles.length === 0}
							icon={<Trash2 aria-hidden="true" size={14} />}
							label="Discard all changes"
							onClick={onDiscardAll}
						/>
						<ActionButton
							disabled={isBusy || selectedWorking.length === 0}
							icon={<SquareCheckBig aria-hidden="true" size={14} />}
							label={`Stage selected (${selectedWorking.length})`}
							onClick={() =>
								onIndexCommand("StageWorkingTreeFiles", selectedWorking, false)
							}
						/>
						<ActionButton
							disabled={isBusy || selectedWorking.length === 0}
							icon={<Trash2 aria-hidden="true" size={14} />}
							label={`Discard selected (${selectedWorking.length})`}
							onClick={() => onDiscardSelected(selectedWorking)}
						/>
					</>
				}
				emptyMessage={
					isLoading ? "Checking the working tree." : "No unstaged changes."
				}
				files={unstagedFiles}
				isBusy={isBusy}
				onIndexCommand={onIndexCommand}
				onOpenFileBlame={onOpenFileBlame}
				onOpenFileHistory={onOpenFileHistory}
				onIgnorePath={onIgnorePath}
				onSelectFile={onSelectFile}
				onToggleSelected={onToggleSelected}
				selectedKeys={selectedKeys}
				title="Unstaged files"
			/>
			<WorkingChangesSection
				actions={
					<>
						<ActionButton
							disabled={isBusy || stagedFiles.length === 0}
							icon={<MinusSquare aria-hidden="true" size={14} />}
							label="Unstage all changes"
							onClick={() =>
								onIndexCommand("UnstageWorkingTreeFiles", [], true)
							}
						/>
						<ActionButton
							disabled={isBusy || selectedStaged.length === 0}
							icon={<MinusSquare aria-hidden="true" size={14} />}
							label={`Unstage selected (${selectedStaged.length})`}
							onClick={() =>
								onIndexCommand("UnstageWorkingTreeFiles", selectedStaged, false)
							}
						/>
					</>
				}
				emptyMessage="No staged changes."
				files={stagedFiles}
				isBusy={isBusy}
				onIndexCommand={onIndexCommand}
				onOpenFileBlame={onOpenFileBlame}
				onOpenFileHistory={onOpenFileHistory}
				onIgnorePath={onIgnorePath}
				onSelectFile={onSelectFile}
				onToggleSelected={onToggleSelected}
				selectedKeys={selectedKeys}
				title="Staged files"
			/>
		</div>
	);
}
