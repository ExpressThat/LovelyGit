import { useState } from "react";
import { toast } from "sonner";
import type { HeadCommitMessageResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";

export function useUndoLastCommit({
	onSuccess,
	repositoryId,
}: {
	onSuccess: (message: HeadCommitMessageResponse) => Promise<void> | void;
	repositoryId: string;
}) {
	const [preview, setPreview] = useState<HeadCommitMessageResponse | null>(
		null,
	);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(false);
	const [isUndoing, setIsUndoing] = useState(false);
	const [isOpen, setIsOpen] = useState(false);

	const open = async () => {
		setIsOpen(true);
		setIsLoading(true);
		setError(null);
		setPreview(null);
		try {
			const message = await sendRequestWithResponse({
				commandType: "GetHeadCommitMessage",
				arguments: { repositoryId },
			});
			if (!message)
				throw new Error("The repository does not have a commit to undo.");
			setPreview(message);
		} catch (reason) {
			setError(errorMessage(reason, "Failed to load the last commit."));
		} finally {
			setIsLoading(false);
		}
	};

	const confirm = async () => {
		if (!preview?.firstParentHash || isUndoing) return;
		setIsUndoing(true);
		setError(null);
		try {
			const message = await sendRequestWithResponse(
				{
					commandType: "UndoLastCommit",
					arguments: {
						expectedHeadHash: preview.hash,
						repositoryId,
					},
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			if (!message) throw new Error("Git did not return the undone commit.");
			await onSuccess(message);
			setIsOpen(false);
			toast.success(`Undid “${message.title}” and kept its changes staged`);
		} catch (reason) {
			setError(errorMessage(reason, "Failed to undo the last commit."));
		} finally {
			setIsUndoing(false);
		}
	};

	return {
		close: () => !isUndoing && setIsOpen(false),
		confirm,
		error,
		isBusy: isLoading || isUndoing,
		isLoading,
		isOpen,
		isUndoing,
		open,
		preview,
	};
}

function errorMessage(reason: unknown, fallback: string) {
	return reason instanceof Error ? reason.message : fallback;
}
