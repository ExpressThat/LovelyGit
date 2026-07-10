import type { RepositoryRefsResponse } from "@/generated/types";
import * as refMetadata from "./refMetadata";

export function buildCommitGraphRefView(
	refs: RepositoryRefsResponse | null,
	remotePrefixes: string[],
	currentBranchName: string | null,
) {
	const tracking = refMetadata.branchTrackingMetadata(refs);
	return {
		branchNames: refMetadata.refNames(refs, "Local"),
		branchUpstreams: tracking.upstreams,
		currentHeadHash: refMetadata.refCommitHash(
			refs,
			"Local",
			currentBranchName,
		),
		existingTagNames: refMetadata.refNames(refs, "Tag"),
		remoteBranchNames: tracking.remoteBranchNames,
		tagRemoteName: refs?.remotePrefixes[0] ?? remotePrefixes[0] ?? null,
	};
}
