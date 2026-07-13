import type { ConflictResolutionResponse } from "@/generated/types";

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
