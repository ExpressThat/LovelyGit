import { useEffect, useState } from "react";
import { toast } from "sonner";
import type {
	GitConflictFile,
	GitConflictStateResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { ConflictContent, type ContentState } from "./ConflictContent";
import { ConflictFileList } from "./ConflictFileList";
import {
	ConflictHeader,
	type ConflictOperationCommand,
	ConflictToolbar,
} from "./ConflictToolbar";

export function ConflictWorkspace({
	onOperationChanged,
	onReload,
	repositoryId,
	state,
}: {
	onOperationChanged: () => void;
	onReload: () => Promise<GitConflictStateResponse | null>;
	repositoryId: string;
	state: GitConflictStateResponse;
}) {
	const [selectedFile, setSelectedFile] = useState<GitConflictFile | null>(
		state.conflictedFiles[0] ?? state.resolvedFiles[0] ?? null,
	);
	const [contentState, setContentState] = useState<ContentState>({
		status: "idle",
		content: null,
	});
	const [isBusy, setIsBusy] = useState(false);
	const [isClosed, setIsClosed] = useState(false);
	const hasConflicts = state.conflictedFiles.length > 0;
	const fallbackFile =
		state.conflictedFiles[0] ?? state.resolvedFiles[0] ?? null;

	useEffect(() => {
		if (
			selectedFile &&
			[...state.conflictedFiles, ...state.resolvedFiles].some(
				(file) => file.path === selectedFile.path,
			)
		) {
			return;
		}

		setSelectedFile(fallbackFile);
	}, [fallbackFile, selectedFile, state.conflictedFiles, state.resolvedFiles]);

	useEffect(() => {
		if (!selectedFile) {
			setContentState({ status: "idle", content: null });
			return;
		}

		let isActive = true;
		setContentState({ status: "loading", content: null });
		sendRequestWithResponse({
			arguments: { path: selectedFile.path, repositoryId },
			commandType: NativeMessageType.GetConflictFileContent,
		})
			.then((content) => {
				if (isActive) setContentState({ status: "loaded", content });
			})
			.catch((error) => {
				const message =
					error instanceof Error ? error.message : "Could not load file.";
				if (isActive)
					setContentState({ status: "error", content: null, message });
			});

		return () => {
			isActive = false;
		};
	}, [repositoryId, selectedFile]);

	const resolveFile = async (
		action: "UseOurs" | "UseTheirs" | "MarkResolved",
	) => {
		if (!selectedFile) return;
		setIsBusy(true);
		try {
			await sendRequestWithResponse({
				arguments: { action, path: selectedFile.path, repositoryId },
				commandType: NativeMessageType.ResolveConflictFile,
			});
			toast.success(`${selectedFile.path} marked resolved`);
			const nextState = await onReload();
			setSelectedFile(
				nextState?.conflictedFiles[0] ?? nextState?.resolvedFiles[0] ?? null,
			);
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not resolve file",
			);
		} finally {
			setIsBusy(false);
		}
	};

	const runOperation = async (commandType: ConflictOperationCommand) => {
		setIsBusy(true);
		try {
			await sendRequestWithResponse({
				arguments: { repositoryId },
				commandType,
			});
			toast.success(
				commandType === NativeMessageType.AbortConflictOperation
					? "Operation aborted"
					: "Operation continued",
			);
			const nextState = await onReload();
			if (nextState && !nextState.operation.isInProgress) {
				setIsClosed(true);
			}
			onOperationChanged();
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Git operation failed",
			);
		} finally {
			setIsBusy(false);
		}
	};

	return isClosed ? null : (
		<section className="flex h-full min-w-0 flex-col overflow-hidden bg-background text-foreground">
			<ConflictHeader
				disabled={isBusy}
				hasConflicts={hasConflicts}
				label={state.operation.label}
				onAbort={() =>
					void runOperation(NativeMessageType.AbortConflictOperation)
				}
				onContinue={() =>
					void runOperation(NativeMessageType.ContinueConflictOperation)
				}
				onRefresh={() => void onReload()}
			/>
			<div className="flex min-h-0 flex-1 overflow-hidden">
				<ConflictFileList
					conflictedFiles={state.conflictedFiles}
					onSelectFile={setSelectedFile}
					resolvedFiles={state.resolvedFiles}
					selectedPath={selectedFile?.path ?? null}
				/>
				<div className="flex min-w-0 flex-1 flex-col overflow-hidden">
					<ConflictToolbar
						disabled={
							isBusy || !selectedFile || selectedFile.status === "Resolved"
						}
						onMarkResolved={() => void resolveFile("MarkResolved")}
						onUseOurs={() => void resolveFile("UseOurs")}
						onUseTheirs={() => void resolveFile("UseTheirs")}
						oursLabel={state.oursLabel}
						path={selectedFile?.path ?? null}
						theirsLabel={state.theirsLabel}
					/>
					<ConflictContent
						oursLabel={state.oursLabel}
						state={contentState}
						theirsLabel={state.theirsLabel}
					/>
				</div>
			</div>
		</section>
	);
}
