import { useVirtualizer } from "@tanstack/react-virtual";
import { MinusSquare, SquareCheckBig, Trash2 } from "lucide-react";
import type { ReactNode } from "react";
import { useMemo, useRef } from "react";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { ChangedFileRow } from "./WorkingChangedFileRow";
import {
	ActionButton,
	fileKey,
	selectedFiles,
} from "./WorkingChangesPanelParts";

const ROW_HEIGHT = 38;

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
				onSelectFile={onSelectFile}
				onToggleSelected={onToggleSelected}
				selectedKeys={selectedKeys}
				title="Staged files"
			/>
		</div>
	);
}

function WorkingChangesSection({
	actions,
	emptyMessage,
	files,
	isBusy,
	onIndexCommand,
	onSelectFile,
	onToggleSelected,
	selectedKeys,
	title,
}: {
	actions: ReactNode;
	emptyMessage: string;
	files: WorkingTreeChangedFile[];
	isBusy: boolean;
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void;
	onSelectFile: (file: WorkingTreeChangedFile) => void;
	onToggleSelected: (file: WorkingTreeChangedFile) => void;
	selectedKeys: Set<string>;
	title: string;
}) {
	const parentRef = useRef<HTMLDivElement>(null);
	const virtualizer = useVirtualizer({
		count: files.length,
		estimateSize: () => ROW_HEIGHT,
		getScrollElement: () => parentRef.current,
		overscan: 8,
	});
	const virtualRows = virtualizer.getVirtualItems();

	return (
		<section className="flex min-h-0 flex-1 flex-col rounded-md border bg-card">
			<header className="flex shrink-0 flex-wrap items-center justify-between gap-2 border-b px-3 py-2">
				<h3 className="text-sm font-semibold text-foreground">
					{title} ({countWorkingChanges(files)})
				</h3>
				<div className="flex flex-wrap justify-end gap-2">{actions}</div>
			</header>
			<div
				className="custom-scrollbar min-h-0 flex-1 overflow-y-auto"
				ref={parentRef}
			>
				{files.length === 0 ? (
					<div className="p-4 text-sm text-muted-foreground">
						{emptyMessage}
					</div>
				) : (
					<div
						className="relative"
						style={{ height: `${virtualizer.getTotalSize()}px` }}
					>
						{virtualRows.map((virtualRow) => {
							const file = files[virtualRow.index];
							if (!file) {
								return null;
							}

							const canSelectFile = canSelect(file);
							const rowActionLabel = singleFileActionLabel(file);
							return (
								<div
									className="absolute left-0 right-0"
									data-index={virtualRow.index}
									key={fileKey(file)}
									ref={virtualizer.measureElement}
									style={{ transform: `translateY(${virtualRow.start}px)` }}
								>
									<ChangedFileRow
										file={file}
										isBusy={isBusy}
										isSelected={
											canSelectFile && selectedKeys.has(fileKey(file))
										}
										onAction={
											rowActionLabel
												? () => runSingleFileAction(file, onIndexCommand)
												: undefined
										}
										onSelect={() => onSelectFile(file)}
										onToggleSelected={
											canSelectFile ? () => onToggleSelected(file) : undefined
										}
										rowActionLabel={rowActionLabel}
									/>
								</div>
							);
						})}
					</div>
				)}
			</div>
		</section>
	);
}

function canSelect(file: WorkingTreeChangedFile) {
	return (
		file.group === "Staged" ||
		file.group === "Unstaged" ||
		file.group === "Untracked"
	);
}

function runSingleFileAction(
	file: WorkingTreeChangedFile,
	onIndexCommand: (
		commandType: "StageWorkingTreeFiles" | "UnstageWorkingTreeFiles",
		files: WorkingTreeChangedFile[],
		includeAll: boolean,
	) => void,
) {
	if (file.group === "Staged") {
		onIndexCommand("UnstageWorkingTreeFiles", [file], false);
		return;
	}

	if (file.group === "Unstaged" || file.group === "Untracked") {
		onIndexCommand("StageWorkingTreeFiles", [file], false);
	}
}

function singleFileActionLabel(file: WorkingTreeChangedFile) {
	if (file.group === "Staged") {
		return "Unstage";
	}

	if (file.group === "Unstaged" || file.group === "Untracked") {
		return "Stage";
	}

	return undefined;
}
