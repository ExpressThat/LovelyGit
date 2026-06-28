import { sendRequestWithResponse } from "@/lib/commands";
import { createEmptyWorkingTreeChanges } from "./OptimisticWorkingTreeChanges";

const workingTreeStatusTimeoutMs = 60_000;

export async function loadWorkingTreeChanges(repositoryId: string) {
	const changes = await sendRequestWithResponse(
		{
			commandType: "GetWorkingTreeChanges",
			arguments: { allowIncompleteSummary: false, repositoryId },
		},
		{ timeoutMs: workingTreeStatusTimeoutMs },
	);
	return changes ?? createEmptyWorkingTreeChanges();
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
	return summary ?? { hasChanges: false, isComplete: true, totalCount: 0 };
}
