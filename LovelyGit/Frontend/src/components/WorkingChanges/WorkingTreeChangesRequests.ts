import { sendRequestWithResponse } from "@/lib/commands";
import { createEmptyWorkingTreeChanges } from "./OptimisticWorkingTreeChanges";

const workingTreeStatusTimeoutMs = 60_000;
const pendingChanges = new Map<string, Promise<ReturnTypeResult>>();

export async function loadWorkingTreeChanges(repositoryId: string) {
	const existing = pendingChanges.get(repositoryId);
	if (existing) return existing;
	const pending = sendRequestWithResponse(
		{
			commandType: "GetWorkingTreeChanges",
			arguments: { allowIncompleteSummary: false, repositoryId },
		},
		{ timeoutMs: workingTreeStatusTimeoutMs },
	).then((changes) => changes ?? createEmptyWorkingTreeChanges());
	pendingChanges.set(repositoryId, pending);
	try {
		return await pending;
	} finally {
		if (pendingChanges.get(repositoryId) === pending) {
			pendingChanges.delete(repositoryId);
		}
	}
}

export async function loadWorkingTreeChangeSummary(
	repositoryId: string,
	allowIncompleteSummary = false,
) {
	const summary = await sendRequestWithResponse(
		{
			commandType: "GetWorkingTreeChangeSummary",
			arguments: { allowIncompleteSummary, repositoryId },
		},
		{ timeoutMs: workingTreeStatusTimeoutMs },
	);
	return (
		summary ?? {
			hasChanges: false,
			isComplete: true,
			shouldPreloadChanges: true,
			totalCount: 0,
		}
	);
}

type ReturnTypeResult = ReturnType<typeof createEmptyWorkingTreeChanges>;
