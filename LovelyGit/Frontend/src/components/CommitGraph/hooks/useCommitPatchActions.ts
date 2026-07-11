import { useState } from "react";
import { toast } from "sonner";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { copyToClipboard } from "../utils/clipboard";

export function useCommitPatchActions(repositoryId: string | null) {
	const [busyCommitHash, setBusyCommitHash] = useState<string | null>(null);
	const [busyAction, setBusyAction] = useState<
		"archive" | "copy" | "save" | null
	>(null);

	async function copyPatch(row: CommitGraphRow) {
		if (!repositoryId || busyCommitHash) return;

		setBusyCommitHash(row.commit.hash);
		setBusyAction("copy");
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.GetCommitPatch,
				arguments: { commitHash: row.commit.hash, repositoryId },
			});
			if (
				!response ||
				response.isTruncated ||
				response.hasUnsupportedBinaryChanges
			) {
				toast.error(patchFailureMessage(response));
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
			setBusyCommitHash(null);
			setBusyAction(null);
		}
	}

	async function savePatch(row: CommitGraphRow) {
		if (!repositoryId || busyCommitHash) return;

		setBusyCommitHash(row.commit.hash);
		setBusyAction("save");
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.SaveCommitPatch,
				arguments: { commitHash: row.commit.hash, repositoryId },
			});
			if (response?.saved) toast.success("Commit patch saved");
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not save commit patch",
			);
		} finally {
			setBusyCommitHash(null);
			setBusyAction(null);
		}
	}

	async function saveArchive(row: CommitGraphRow) {
		if (!repositoryId || busyCommitHash) return;

		setBusyCommitHash(row.commit.hash);
		setBusyAction("archive");
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.SaveCommitArchive,
				arguments: { commitHash: row.commit.hash, repositoryId },
			});
			if (response?.saved) toast.success("Commit archive exported");
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not export commit archive",
			);
		} finally {
			setBusyCommitHash(null);
			setBusyAction(null);
		}
	}

	return { busyAction, busyCommitHash, copyPatch, saveArchive, savePatch };
}

function patchFailureMessage(
	response:
		| { hasUnsupportedBinaryChanges: boolean; isTruncated: boolean }
		| null
		| undefined,
) {
	if (response?.hasUnsupportedBinaryChanges) {
		return "Binary changes cannot yet be copied as a patch";
	}
	return response?.isTruncated
		? "This patch is too large to copy safely"
		: "Could not create the commit patch";
}
