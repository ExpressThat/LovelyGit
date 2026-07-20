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

export function withLocalBranchChange(
	response: RepositoryRefsResponse,
	oldName: string,
	newName: string | null,
) {
	const index = response.refs.findIndex(
		(ref) => ref.kind === "Local" && ref.name === oldName,
	);
	if (index < 0) return response;
	const refs = response.refs.slice();
	if (newName) refs[index] = { ...refs[index], name: newName };
	else refs.splice(index, 1);
	const branchUpstreams = response.branchUpstreams.flatMap((upstream) =>
		upstream.branchName !== oldName
			? [upstream]
			: newName
				? [{ ...upstream, branchName: newName }]
				: [],
	);
	return {
		...response,
		branchUpstreams,
		currentBranchName:
			response.currentBranchName === oldName
				? newName
				: response.currentBranchName,
		refs,
	};
}
