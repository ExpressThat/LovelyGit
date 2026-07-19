import type { ConflictScrollAnchor } from "./conflictScrollSync";

export type { ConflictScrollAnchor } from "./conflictScrollSync";

export interface ConflictScrollApi {
	scrollTo: (anchor: ConflictScrollAnchor) => void;
}
