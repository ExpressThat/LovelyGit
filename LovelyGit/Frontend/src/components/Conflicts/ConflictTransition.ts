import { toast } from "sonner";
import type { GitConflictStateResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { notifyGitOperationChanged } from "./ConflictOperationEvents";

export async function showConflictWorkspaceIfNeeded({
	repositoryId,
	toastId,
}: {
	repositoryId: string;
	toastId?: string | number;
}) {
	const state = await loadConflictState(repositoryId);
	if (!state || !isConflictOperationActive(state)) {
		return false;
	}

	notifyGitOperationChanged(repositoryId, state);
	toast.warning(`${state.operation.label}. Resolve conflicts to continue.`, {
		id: toastId,
	});
	return true;
}

export function isConflictOperationActive(
	state: GitConflictStateResponse | null | undefined,
) {
	return Boolean(
		state?.operation.isInProgress && state.operation.kind !== "None",
	);
}

export function showGitActionError(
	error: unknown,
	fallbackMessage: string,
	toastId?: string | number,
) {
	toast.error(error instanceof Error ? error.message : fallbackMessage, {
		id: toastId,
	});
}

async function loadConflictState(
	repositoryId: string,
): Promise<GitConflictStateResponse | null> {
	try {
		return await sendRequestWithResponse({
			arguments: { repositoryId },
			commandType: NativeMessageType.GetConflictState,
		});
	} catch {
		return null;
	}
}
