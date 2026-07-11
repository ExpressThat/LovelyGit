import { AlertTriangle } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { useRef } from "react";
import type { WorkingTreeChangedFile } from "@/generated/types";
import { LoadingDiff } from "../CommitFileDiff/DiffContent";
import { DiffToolbarControls } from "../CommitFileDiff/DiffToolbarControls";
import { ConflictMergeHeader } from "./ConflictMergeHeader";
import { ConflictNavigationBar } from "./ConflictNavigationBar";
import { ConflictResultPanel } from "./ConflictResultPanel";
import { ConflictSourcePanes } from "./ConflictSourcePanes";
import { useConflictResolution } from "./useConflictResolution";
import { useConflictSplitter } from "./useConflictSplitter";
import { WholeFileResolutionPanel } from "./WholeFileResolutionPanel";

export function ConflictResolutionView({
	file,
	onChange,
	onClose,
	repositoryId,
}: {
	file: WorkingTreeChangedFile;
	onChange?: () => Promise<void> | void;
	onClose: () => void;
	repositoryId: string;
}) {
	const model = useConflictResolution({
		file,
		onChange,
		onClose,
		repositoryId,
	});
	const workspaceRef = useRef<HTMLDivElement>(null);
	const splitter = useConflictSplitter(workspaceRef);
	const reduceMotion = useReducedMotion();
	return (
		<section className="flex h-full min-w-0 flex-1 flex-col overflow-hidden border-l bg-background text-foreground">
			<ConflictMergeHeader
				conflictCount={
					model.state.status === "loaded"
						? Math.max(1, model.textConflicts.length)
						: 0
				}
				controlsDisabled={model.state.status !== "loaded"}
				fileName={file.path}
				isBusy={model.busyAction !== null}
				isExternalOpen={model.busyAction === "external"}
				onClose={onClose}
				onExternalTool={model.openExternalTool}
				onSave={model.save}
				saveDisabled={!model.canSave}
			/>
			<DiffToolbarControls
				className="h-9 shrink-0 border-t-0 border-b"
				disabled={model.busyAction !== null}
				showViewMode={false}
			/>
			{model.actionError ? <ErrorMessage message={model.actionError} /> : null}
			{model.state.status === "loading" ? <LoadingDiff /> : null}
			{model.state.status === "error" ? (
				<ErrorMessage message={model.state.message} />
			) : null}
			{model.state.status === "loaded" ? (
				<div className="flex min-h-0 flex-1 flex-col" ref={workspaceRef}>
					<motion.div
						animate={{ height: `${splitter.sourcePercent}%` }}
						className="min-h-0 shrink-0 overflow-hidden"
						transition={{
							duration: splitter.isDragging || reduceMotion ? 0 : 0.16,
						}}
					>
						{model.wholeFile ? (
							<WholeFileNotice />
						) : (
							<ConflictSourcePanes
								activeConflict={model.activeConflict}
								choices={model.choices}
								conflict={model.state.conflict}
								contextLines={model.contextLines}
								disabled={model.busyAction !== null || model.isManualResult}
								lineDisplayMode={model.lineDisplayMode}
								onChoice={model.updateChoice}
								wrapLines={model.wrapLines}
							/>
						)}
					</motion.div>
					<ConflictNavigationBar
						active={model.activeConflict}
						count={model.wholeFile ? 0 : model.textConflicts.length}
						disabled={model.busyAction !== null || model.isManualResult}
						onNavigate={model.setActiveConflict}
						onOmit={model.omitActiveConflict}
						onPointerDown={splitter.startDrag}
						onResizeBy={splitter.resizeBy}
						onReset={model.resetResult}
						resetDisabled={model.busyAction !== null}
						unresolved={
							model.wholeFile
								? model.wholeFileSelection
									? 0
									: 1
								: model.unresolved
						}
					/>
					<div className="min-h-0 flex-1 overflow-hidden">
						{model.wholeFile ? (
							<WholeFileResolutionPanel
								exists={{
									base: model.state.conflict.base.exists,
									ours: model.state.conflict.ours.exists,
									theirs: model.state.conflict.theirs.exists,
								}}
								selection={model.wholeFileSelection}
								setSelection={model.setWholeFileSelection}
							/>
						) : (
							<ConflictResultPanel
								isManualResult={model.isManualResult}
								isResolved={model.isTextResolved}
								onEdit={model.editResult}
								value={model.resultText}
								wrapLines={model.wrapLines}
							/>
						)}
					</div>
				</div>
			) : null}
		</section>
	);
}

function ErrorMessage({ message }: { message: string }) {
	return (
		<div className="border-b border-destructive/40 bg-destructive/10 px-3 py-2 text-xs text-destructive">
			{message}
		</div>
	);
}

function WholeFileNotice() {
	return (
		<div className="grid h-full place-items-center p-6 text-center text-sm text-muted-foreground">
			<div>
				<AlertTriangle className="mx-auto mb-2 size-5" />
				Line comparison is unavailable for this file.
			</div>
		</div>
	);
}
