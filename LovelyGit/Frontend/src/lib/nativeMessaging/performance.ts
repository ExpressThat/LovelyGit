import type {
	NativeMessageResponse,
	NativeMessageTypesWithResponse,
} from "./types";

const maxInteractionMs = 200;
const maxMemoryBytes = 200 * 1024 * 1024;

export function recordNativeMessagePerformance<
	TMessageType extends NativeMessageTypesWithResponse,
>(
	messageType: TMessageType,
	response: NativeMessageResponse<TMessageType>,
	roundTripMs: number,
) {
	const metrics = response.metrics;
	const roundedRoundTripMs = Math.ceil(roundTripMs);
	const missedLatency =
		roundedRoundTripMs > maxInteractionMs ||
		(metrics?.durationMs ?? 0) > maxInteractionMs;
	const missedMemory =
		(metrics?.workingSetBytes ?? 0) > maxMemoryBytes ||
		(metrics?.privateMemoryBytes ?? 0) > maxMemoryBytes;

	if (!missedLatency && !missedMemory) {
		return;
	}

	console.warn(
		"LovelyGit performance target missed",
		JSON.stringify({
			messageType,
			roundTripMs: roundedRoundTripMs,
			metrics,
		}),
	);
}
