import { useState } from "react";
import { toast } from "sonner";
import type {
	SparseCheckoutAction,
	SparseCheckoutState,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useSparseCheckoutManager(repositoryId: string) {
	const [state, setState] = useState<SparseCheckoutState | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [busyAction, setBusyAction] = useState<SparseCheckoutAction | null>(
		null,
	);
	const [error, setError] = useState<string | null>(null);

	async function load() {
		if (isLoading) return;
		setIsLoading(true);
		setError(null);
		try {
			setState(
				await sendRequestWithResponse({
					commandType: NativeMessageType.GetSparseCheckoutState,
					arguments: { repositoryId },
				}),
			);
		} catch (loadError) {
			setError(message(loadError, "Could not read sparse-checkout state"));
		} finally {
			setIsLoading(false);
		}
	}

	async function run(
		action: SparseCheckoutAction,
		coneMode: boolean,
		patterns: string[],
	) {
		if (busyAction) return false;
		setBusyAction(action);
		try {
			const nextState = await sendRequestWithResponse(
				{
					commandType: NativeMessageType.ManageSparseCheckout,
					arguments: { action, coneMode, patterns, repositoryId },
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			setState(nextState);
			setError(null);
			toast.success(
				action === "Disable"
					? "Full working tree restored"
					: "Sparse checkout updated",
			);
			return true;
		} catch (runError) {
			toast.error(message(runError, "Sparse checkout could not be updated"), {
				duration: 8_000,
			});
			return false;
		} finally {
			setBusyAction(null);
		}
	}

	return { busyAction, error, isLoading, load, run, state };
}

function message(error: unknown, fallback: string) {
	return error instanceof Error && error.message ? error.message : fallback;
}
