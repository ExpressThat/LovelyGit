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
