import { loadWorkingTreeChanges } from "@/components/WorkingChanges/WorkingTreeChangesRequests";

const verificationDelaysMs = [0, 250, 500, 1_000, 2_000, 4_000, 4_000] as const;

export async function verifyExternalConflictResolved(
	repositoryId: string,
	path: string,
) {
	for (const delayMs of verificationDelaysMs) {
		if (delayMs > 0) {
			await new Promise((resolve) => globalThis.setTimeout(resolve, delayMs));
		}
		const changes = await loadWorkingTreeChanges(repositoryId);
		if (!changes.unmerged.some((item) => item.path === path)) return;
	}
	throw new Error(
		"The external merge tool closed without resolving this conflict.",
	);
}
