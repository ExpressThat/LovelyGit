import type { WorkingTreeChangesResponse } from "@/generated/types";

export type WorkingTreeChangesState =
	| { status: "idle"; changes: null }
	| { status: "loading"; changes: WorkingTreeChangesResponse | null }
	| {
			status: "error";
			changes: WorkingTreeChangesResponse | null;
			message: string;
	  }
	| { status: "loaded"; changes: WorkingTreeChangesResponse };

export type WorkingTreeReloadRequest = {
	promise: Promise<void>;
	repositoryId: string;
};

export function getWorkingTreeLoadErrorMessage(error: unknown) {
	return error instanceof Error
		? error.message
		: "Failed to load working changes.";
}
