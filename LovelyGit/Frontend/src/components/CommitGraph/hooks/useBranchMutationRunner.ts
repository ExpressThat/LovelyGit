import { useState } from "react";
import { toast } from "sonner";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";

export type BranchMutate = (
	branchName: string,
	loadingMessage: string,
	successMessage: string,
	request: Parameters<typeof sendRequestWithResponse>[0],
	afterSuccess?: () => void,
) => Promise<void>;

export function useBranchMutationRunner(repositoryId: string | null) {
	const [busyBranch, setBusyBranch] = useState<string | null>(null);

	const mutate: BranchMutate = async (
		branchName,
		loadingMessage,
		successMessage,
		request,
		afterSuccess,
	) => {
		if (!repositoryId || busyBranch) return;
		setBusyBranch(branchName);
		const toastId = toast.loading(loadingMessage);
		try {
			await sendRequestWithResponse(request, {
				timeoutMs: gitMutationTimeoutMs,
			});
			afterSuccess?.();
			toast.success(successMessage, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Git operation failed.",
				{ id: toastId },
			);
		} finally {
			setBusyBranch(null);
		}
	};

	return { busyBranch, mutate };
}
