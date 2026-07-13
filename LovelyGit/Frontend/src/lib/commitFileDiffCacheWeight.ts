import type { CommitFileDiffResponse } from "@/generated/types";
import { getCompactDiffPayloadIdentity } from "./compactDiffPayloadIdentity";

const ESTIMATED_LINE_WEIGHT = 160;
const weights = new WeakMap<object, number>();

export function getCommitFileDiffPayloadWeight(
	response: CommitFileDiffResponse,
) {
	const identity = getCompactDiffPayloadIdentity(response);
	const cached = weights.get(identity);
	if (cached !== undefined) return { identity, weight: cached };

	const lineCount = Math.max(
		response.lines?.length ?? 0,
		response.compactLineCount ?? 0,
		response.virtualLineCount ?? 0,
	);
	let weight = lineCount * ESTIMATED_LINE_WEIGHT;
	weight += response.compactLinesGzipBase64?.length ?? 0;
	weight += response.compactSourceBundleGzipBase64?.length ?? 0;
	weight += response.virtualText?.length ?? 0;
	weight += response.virtualTextGzipBase64?.length ?? 0;
	for (const line of response.lines ?? []) {
		weight += line.oldText?.length ?? 0;
		weight += line.newText?.length ?? 0;
		weight += line.text?.length ?? 0;
	}
	weights.set(identity, weight);
	return { identity, weight };
}
