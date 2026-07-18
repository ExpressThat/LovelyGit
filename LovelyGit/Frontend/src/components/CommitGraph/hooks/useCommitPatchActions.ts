import { useState } from "react";
import { toast } from "sonner";
import type { CommitGraphRow } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { expandCommitPatchPayload } from "../commitPatchPayload";
import { copyToClipboard } from "../utils/clipboard";

const SERIES_BUSY_KEY = "__patch-series__";

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
			}).then(expandCommitPatchPayload);
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

	async function copyPatchSeries(rows: CommitGraphRow[]) {
		if (!repositoryId || busyCommitHash || rows.length < 2) return false;
		setBusyCommitHash(SERIES_BUSY_KEY);
		setBusyAction("copy");
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.GetCommitPatchSeries,
				arguments: {
					commitHashes: rows.map((row) => row.commit.hash),
					repositoryId,
				},
			}).then(expandCommitPatchPayload);
			if (
				!response ||
				response.isTruncated ||
				response.hasUnsupportedBinaryChanges
			) {
				toast.error(seriesFailureMessage(response));
				return false;
			}
			await copyToClipboard(
				response.patch,
				`${response.commitCount}-commit patch series`,
			);
			return true;
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not create the patch series",
			);
			return false;
		} finally {
			setBusyCommitHash(null);
			setBusyAction(null);
		}
	}

	async function savePatchSeries(rows: CommitGraphRow[]) {
		if (!repositoryId || busyCommitHash || rows.length < 2) return false;
		setBusyCommitHash(SERIES_BUSY_KEY);
		setBusyAction("save");
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.SaveCommitPatchSeries,
				arguments: {
					commitHashes: rows.map((row) => row.commit.hash),
					repositoryId,
				},
			});
			if (!response?.saved) return false;
			toast.success("Patch series saved");
			return true;
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not save the patch series",
			);
			return false;
		} finally {
			setBusyCommitHash(null);
			setBusyAction(null);
		}
	}

	return {
		busyAction,
		busyCommitHash,
		copyPatch,
		copyPatchSeries,
		saveArchive,
		savePatch,
		savePatchSeries,
		seriesBusyAction:
			busyCommitHash === SERIES_BUSY_KEY && busyAction !== "archive"
				? busyAction
				: null,
	};
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

function seriesFailureMessage(
	response:
		| { hasUnsupportedBinaryChanges: boolean; isTruncated: boolean }
		| null
		| undefined,
) {
	if (response?.hasUnsupportedBinaryChanges) {
		return "Binary changes cannot yet be exported in a patch series";
	}
	return response?.isTruncated
		? "This patch series is too large to export safely"
		: "Could not create the patch series";
}
