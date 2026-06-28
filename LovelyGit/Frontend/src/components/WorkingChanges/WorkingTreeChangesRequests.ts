import { sendRequestWithResponse } from "@/lib/commands";
import { createEmptyWorkingTreeChanges } from "./OptimisticWorkingTreeChanges";

const workingTreeStatusTimeoutMs = 60_000;

export async function loadWorkingTreeChanges(repositoryId: string) {
	const changes = await sendRequestWithResponse(
		{
			commandType: "GetWorkingTreeChanges",
			arguments: { repositoryId },
		},
		{ timeoutMs: workingTreeStatusTimeoutMs },
	);
	return changes ?? createEmptyWorkingTreeChanges();
}

export async function loadWorkingTreeChangeSummary(repositoryId: string) {
	const summary = await sendRequestWithResponse(
		{
			commandType: "GetWorkingTreeChangeSummary",
			arguments: { repositoryId },
		},
		{ timeoutMs: workingTreeStatusTimeoutMs },
	);
	return summary?.totalCount ?? 0;
}
