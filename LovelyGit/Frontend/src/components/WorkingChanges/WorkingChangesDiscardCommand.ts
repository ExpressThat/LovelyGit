import { flushSync } from "react-dom";
import type {
	WorkingTreeChangedFile,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { applyOptimisticDiscard } from "./OptimisticWorkingTreeDiscard";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

export async function discardWorkingChanges({
	changes,
	discardFiles,
	onRefresh,
	repositoryId,
	setActionError,
	setDiscardFiles,
	setIsMutating,
	setOptimisticChanges,
	setSelectedKeys,
}: DiscardWorkingChangesOptions) {
	if (!changes || discardFiles.length === 0) return;

	flushSync(() => {
		setIsMutating(true);
		setActionError(null);
		setOptimisticChanges(applyOptimisticDiscard(changes, discardFiles));
		setDiscardFiles([]);
		setSelectedKeys(new Set());
	});
	await waitForWorkingTreePaint();
	let discardCompleted = false;
	try {
		await sendRequestWithResponse({
			commandType: "DiscardWorkingTreeChanges",
			arguments: { files: discardFiles, repositoryId },
		});
		discardCompleted = true;
		await onRefresh();
		setOptimisticChanges(null);
	} catch (error) {
		if (!discardCompleted) setOptimisticChanges(null);
		setActionError(
			error instanceof Error
				? error.message
				: discardCompleted
					? "Changes were discarded, but status could not be refreshed."
					: "Failed to discard selected changes.",
		);
	} finally {
		setIsMutating(false);
	}
}

type DiscardWorkingChangesOptions = {
	changes: WorkingTreeChangesResponse | null;
	discardFiles: WorkingTreeChangedFile[];
	onRefresh: () => Promise<void> | void;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setDiscardFiles: (files: WorkingTreeChangedFile[]) => void;
	setIsMutating: (isMutating: boolean) => void;
	setOptimisticChanges: (changes: WorkingTreeChangesResponse | null) => void;
	setSelectedKeys: (keys: Set<string>) => void;
};
