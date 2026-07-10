import { useState } from "react";
import { toast } from "sonner";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { copyToClipboard } from "../utils/clipboard";

export function useCommitPatchActions(repositoryId: string | null) {
	const [copyingCommitHash, setCopyingCommitHash] = useState<string | null>(
		null,
	);

	async function copyPatch(row: CommitGraphRow) {
		if (!repositoryId || copyingCommitHash) return;

		setCopyingCommitHash(row.commit.hash);
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.GetCommitPatch,
				arguments: { commitHash: row.commit.hash, repositoryId },
			});
			if (!response || response.isTruncated) {
				toast.error(
					response?.isTruncated
						? "This patch is too large to copy safely"
						: "Could not create the commit patch",
				);
				return;
			}

			await copyToClipboard(response.patch, "Commit patch");
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not create the commit patch",
			);
		} finally {
			setCopyingCommitHash(null);
		}
	}

	return { copyingCommitHash, copyPatch };
}
