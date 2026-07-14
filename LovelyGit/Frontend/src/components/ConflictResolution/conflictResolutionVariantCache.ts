import type { ConflictResolutionResponse } from "@/generated/types";
import { createConflictDocument } from "./conflictDocument";

export type ConflictLoadState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| {
			status: "loaded";
			conflict: ConflictResolutionResponse;
			document: ReturnType<typeof createConflictDocument>;
	  };

export class ConflictDocumentCache {
	private identity = "";
	private document: ReturnType<typeof createConflictDocument> = [];

	get(owner: string, conflict: ConflictResolutionResponse) {
		const identity = `${owner}\0${conflict.worktreeFingerprint}`;
		if (this.identity !== identity) {
			this.identity = identity;
			this.document = createConflictDocument(conflict);
		}
		return this.document;
	}
}

export class ConflictResolutionVariantCache {
	private owner = "";
	private readonly variants = new Map<
		boolean,
		Promise<ConflictResolutionResponse | null>
	>();

	load(
		owner: string,
		ignoreWhitespace: boolean,
		loader: (
			sibling: ConflictResolutionResponse | null,
		) => Promise<ConflictResolutionResponse | null>,
	) {
		if (owner !== this.owner) {
			this.owner = owner;
			this.variants.clear();
		}
		const cached = this.variants.get(ignoreWhitespace);
		if (cached) return cached;

		const sibling = this.variants.get(!ignoreWhitespace);
		const pending = (async () =>
			loader(sibling ? await sibling : null))().catch((error: unknown) => {
			this.variants.delete(ignoreWhitespace);
			throw error;
		});
		this.variants.set(ignoreWhitespace, pending);
		return pending;
	}
}
