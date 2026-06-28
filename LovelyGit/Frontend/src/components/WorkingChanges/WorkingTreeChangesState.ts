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
