import { useState } from "react";
import { toast } from "sonner";
import type { PatchPreviewResponse } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function usePatchApply(
	repositoryId: string | null,
	onApplied: () => void,
) {
	const [preview, setPreview] = useState<PatchPreviewResponse | null>(null);
	const [isSelecting, setIsSelecting] = useState(false);
	const [isApplying, setIsApplying] = useState(false);
	const [stageChanges, setStageChanges] = useState(false);
	const [reverse, setReverse] = useState(false);

	async function choosePatch() {
		if (!repositoryId || isSelecting || isApplying) return;
		setIsSelecting(true);
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.ChoosePatchFile,
			});
			if (response?.selected) {
				setPreview(response);
				setStageChanges(false);
				setReverse(false);
			}
		} catch (error) {
			toast.error(errorMessage(error, "Could not read this patch"));
		} finally {
			setIsSelecting(false);
		}
	}

	async function applyPatch() {
		if (!repositoryId || !preview?.path || isApplying) return;
		setIsApplying(true);
		try {
			await sendRequestWithResponse(
				{
					commandType: NativeMessageType.ApplyPatch,
					arguments: {
						patchPath: preview.path,
						repositoryId,
						reverse,
						stageChanges,
					},
				},
				{ timeoutMs: 30_000 },
			);
			toast.success(
				stageChanges ? "Patch applied and staged" : "Patch applied",
			);
			setPreview(null);
			onApplied();
		} catch (error) {
			toast.error(errorMessage(error, "Git could not apply this patch"), {
				duration: 8_000,
			});
		} finally {
			setIsApplying(false);
		}
	}

	return {
		applyPatch,
		choosePatch,
		isApplying,
		isSelecting,
		preview,
		reverse,
		setPreview,
		setReverse,
		setStageChanges,
		stageChanges,
	};
}

function errorMessage(error: unknown, fallback: string) {
	return error instanceof Error && error.message ? error.message : fallback;
}
