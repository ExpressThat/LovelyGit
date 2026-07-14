import { sendRequestWithResponse } from "@/lib/commands";
import { createEmptyWorkingTreeChanges } from "./OptimisticWorkingTreeChanges";

const workingTreeStatusTimeoutMs = 60_000;
const pendingChanges = new Map<string, Promise<ReturnTypeResult>>();

export async function loadWorkingTreeChanges(
	repositoryId: string,
	onPreliminary?: (changes: ReturnTypeResult) => void,
) {
	if (!onPreliminary) return loadWorkingTreeChangesMode(repositoryId, false);
	const preliminaryRequest = loadWorkingTreeChangesMode(repositoryId, true);
	const completeRequest = loadWorkingTreeChangesMode(repositoryId, false);
	let preliminary: ReturnTypeResult;
	try {
		preliminary = await preliminaryRequest;
	} catch {
		return completeRequest;
	}
	if (preliminary.isComplete) return preliminary;
	onPreliminary(preliminary);
	return completeRequest;
}

async function loadWorkingTreeChangesMode(
	repositoryId: string,
	trackedOnly: boolean,
) {
	const requestKey = `${repositoryId}:${trackedOnly}`;
	const existing = pendingChanges.get(requestKey);
	if (existing) return existing;
	const pending = sendRequestWithResponse(
		{
			commandType: "GetWorkingTreeChanges",
			arguments: { allowIncompleteSummary: false, repositoryId, trackedOnly },
		},
		{ timeoutMs: workingTreeStatusTimeoutMs },
	).then((changes) => changes ?? createEmptyWorkingTreeChanges());
	pendingChanges.set(requestKey, pending);
	try {
		return await pending;
	} finally {
		if (pendingChanges.get(requestKey) === pending) {
			pendingChanges.delete(requestKey);
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
			arguments: { allowIncompleteSummary, repositoryId, trackedOnly: false },
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
