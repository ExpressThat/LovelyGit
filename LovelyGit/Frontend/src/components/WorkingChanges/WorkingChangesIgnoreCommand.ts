import { flushSync } from "react-dom";
import { toast } from "sonner";
import type {
	GitIgnoreTarget,
	WorkingTreeChangesResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	applyOptimisticIgnore,
	mergeTargetedStatus,
} from "./OptimisticWorkingTreeIgnore";
import { waitForWorkingTreePaint } from "./WorkingTreePaintBoundary";

type IgnoreCommandOptions = {
	clearOptimisticChanges: (expected: WorkingTreeChangesResponse) => void;
	changes: WorkingTreeChangesResponse | null;
	isOptimisticChangesCurrent: (expected: WorkingTreeChangesResponse) => boolean;
	onRefresh: () => Promise<void> | void;
	path: string;
	repositoryId: string;
	setActionError: (message: string | null) => void;
	setIsMutating: (isMutating: boolean) => void;
	setOptimisticChanges: (changes: WorkingTreeChangesResponse | null) => void;
	target: GitIgnoreTarget;
};

export async function ignoreWorkingTreePath(options: IgnoreCommandOptions) {
	const {
		changes,
		path,
		repositoryId,
		setActionError,
		setIsMutating,
		setOptimisticChanges,
		target,
	} = options;
	const optimisticChanges = changes
		? applyOptimisticIgnore(changes, path)
		: null;
	flushSync(() => {
		setIsMutating(true);
		setActionError(null);
		if (optimisticChanges) setOptimisticChanges(optimisticChanges);
	});
	await waitForWorkingTreePaint();
	let ignoreUpdated = false;
	try {
		const result = await sendRequestWithResponse({
			commandType: "IgnoreWorkingTreePath",
			arguments: { path, repositoryId, target },
		});
		ignoreUpdated = true;
		const destination = target === "Local" ? ".git/info/exclude" : ".gitignore";
		toast.success(
			result.added
				? `Ignored ${path} in ${destination}`
				: `${path} is already listed in ${destination}`,
		);

		let reconciled = optimisticChanges;
		if (optimisticChanges && result.targetChanges) {
			reconciled = mergeTargetedStatus(
				optimisticChanges,
				result.targetChanges,
				".gitignore",
			);
			setOptimisticChanges(reconciled);
		}
		if ((target === "Local" || result.targetChanges) && reconciled) {
			void reconcileIgnore(options, reconciled);
			return;
		}

		await options.onRefresh();
		if (optimisticChanges) options.clearOptimisticChanges(optimisticChanges);
	} catch (error) {
		if (!ignoreUpdated) setOptimisticChanges(null);
		setActionError(errorMessage(error, ignoreUpdated));
	} finally {
		setIsMutating(false);
	}
}

async function reconcileIgnore(
	options: IgnoreCommandOptions,
	expected: WorkingTreeChangesResponse,
) {
	try {
		await options.onRefresh();
		if (options.isOptimisticChangesCurrent(expected)) {
			options.clearOptimisticChanges(expected);
		}
	} catch {
		if (options.isOptimisticChangesCurrent(expected)) {
			options.setActionError(
				"The path was ignored, but its status could not be refreshed.",
			);
		}
	}
}

function errorMessage(error: unknown, ignoreUpdated: boolean) {
	if (error instanceof Error) return error.message;
	return ignoreUpdated
		? "The path was ignored, but its status could not be refreshed."
		: "Failed to ignore this path.";
}
