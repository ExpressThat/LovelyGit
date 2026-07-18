import { useEffect, useState } from "react";
import type {
	InteractiveRebaseAction,
	InteractiveRebasePlanItem,
	InteractiveRebasePlanResponse,
} from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { waitForBrowserPaint } from "@/lib/waitForBrowserPaint";

export function useInteractiveRebasePlan(
	repositoryId: string | null,
	baseCommitHash: string | null,
) {
	const [response, setResponse] =
		useState<InteractiveRebasePlanResponse | null>(null);
	const [plan, setPlan] = useState<InteractiveRebasePlanItem[]>([]);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [isRunning, setIsRunning] = useState(false);

	useEffect(() => {
		if (!repositoryId || !baseCommitHash) return;
		let active = true;
		setIsLoading(true);
		setError(null);
		void sendRequestWithResponse({
			arguments: { baseCommitHash, repositoryId },
			commandType: NativeMessageType.GetInteractiveRebasePlan,
		})
			.then((result) => {
				if (!active) return;
				setResponse(result);
				setPlan(
					result.commits.map((commit) => ({
						action: "Pick",
						hash: commit.hash,
						message: commit.subject,
					})),
				);
			})
			.catch((reason) => {
				if (active) setError(toMessage(reason));
			})
			.finally(() => {
				if (active) setIsLoading(false);
			});
		return () => {
			active = false;
		};
	}, [baseCommitHash, repositoryId]);

	const updateAction = (hash: string, action: InteractiveRebaseAction) =>
		setPlan((current) =>
			current.map((item) => (item.hash === hash ? { ...item, action } : item)),
		);
	const updateMessage = (hash: string, message: string) =>
		setPlan((current) =>
			current.map((item) => (item.hash === hash ? { ...item, message } : item)),
		);
	const move = (index: number, offset: number) =>
		setPlan((current) => movePlanItem(current, index, offset));
	const validationError = validatePlan(plan);
	const start = async () => {
		if (!repositoryId || !baseCommitHash || validationError || isRunning)
			return null;
		setIsRunning(true);
		try {
			await waitForBrowserPaint();
			return await sendRequestWithResponse(
				{
					arguments: { baseCommitHash, plan, repositoryId },
					commandType: NativeMessageType.StartInteractiveRebase,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
		} finally {
			setIsRunning(false);
		}
	};

	return {
		error,
		isLoading,
		isRunning,
		move,
		plan,
		response,
		start,
		updateAction,
		updateMessage,
		validationError,
	};
}

export function movePlanItem<T>(plan: T[], index: number, offset: number) {
	const destination = index + offset;
	if (destination < 0 || destination >= plan.length) return plan;
	const next = [...plan];
	[next[index], next[destination]] = [next[destination], next[index]];
	return next;
}

export function validatePlan(plan: InteractiveRebasePlanItem[]) {
	let retained = false;
	for (const item of plan) {
		if ((item.action === "Squash" || item.action === "Fixup") && !retained) {
			return "Squash and fixup need a retained commit immediately before them.";
		}
		if (item.action === "Reword" && !item.message?.trim())
			return "Enter every reworded commit message.";
		retained ||= item.action !== "Drop";
	}
	return retained ? null : "Keep at least one commit in the plan.";
}

function toMessage(reason: unknown) {
	return reason instanceof Error
		? reason.message
		: "Could not load the interactive rebase plan.";
}
