import type { CommitFileDiffResponse } from "@/generated/types";

const identities = new WeakMap<CommitFileDiffResponse, object>();

export function getCompactDiffPayloadIdentity(
	response: CommitFileDiffResponse,
) {
	let identity = identities.get(response);
	if (!identity) {
		identity = {};
		identities.set(response, identity);
	}
	return identity;
}

export function shareCompactDiffPayloadIdentity(
	source: CommitFileDiffResponse,
	target: CommitFileDiffResponse,
) {
	identities.set(target, getCompactDiffPayloadIdentity(source));
}
