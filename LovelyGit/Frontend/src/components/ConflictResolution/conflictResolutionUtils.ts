import type { ConflictResolutionResponse } from "@/generated/types";

export function requiresWholeFileChoice(conflict: ConflictResolutionResponse) {
	if (!conflict.ours.exists || !conflict.theirs.exists) return true;
	return [conflict.ours, conflict.theirs, conflict.result].some(
		(version) => version.isBinary || version.isTooLarge,
	);
}

export function conflictErrorMessage(error: unknown) {
	return error instanceof Error
		? error.message
		: "Failed to resolve the conflict.";
}
