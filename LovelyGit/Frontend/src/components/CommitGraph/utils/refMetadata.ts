import type { CommitRefKind, RepositoryRefsResponse } from "@/generated/types";

export function refNames(
	response: RepositoryRefsResponse | null,
	kind: CommitRefKind,
) {
	return (response?.refs ?? [])
		.filter((reference) => reference.kind === kind)
		.map((reference) => reference.name);
}

export function refCommitHash(
	response: RepositoryRefsResponse | null,
	kind: CommitRefKind,
	name: string | null,
) {
	return (
		response?.refs.find((ref) => ref.kind === kind && ref.name === name)
			?.commitHash ?? null
	);
}

export function branchTrackingMetadata(
	response: RepositoryRefsResponse | null,
) {
	return {
		remoteBranchNames: refNames(response, "Remote").filter(
			(name) => !name.endsWith("/HEAD"),
		),
		upstreams: Object.fromEntries(
			(response?.branchUpstreams ?? []).map((upstream) => [
				upstream.branchName,
				upstream.upstreamName,
			]),
		),
	};
}

export function withBranchUpstream(
	response: RepositoryRefsResponse,
	branchName: string,
	upstreamName: string | null,
) {
	return {
		...response,
		branchUpstreams: [
			...(response.branchUpstreams ?? []).filter(
				(upstream) => upstream.branchName !== branchName,
			),
			...(upstreamName ? [{ branchName, upstreamName }] : []),
		],
	};
}
