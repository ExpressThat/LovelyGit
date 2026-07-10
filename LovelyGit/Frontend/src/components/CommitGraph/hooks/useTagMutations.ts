import { useState } from "react";
import { toast } from "sonner";
import { sendRequestWithResponse } from "@/lib/commands";
import { gitMutationTimeoutMs } from "@/lib/gitMutationTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useTagMutations({
	onRepositoryChanged,
	remoteName,
	repositoryId,
}: {
	onRepositoryChanged: () => void;
	remoteName: string | null;
	repositoryId: string | null;
}) {
	const [busyTag, setBusyTag] = useState<string | null>(null);
	const [deleteTagName, setDeleteTagName] = useState<string | null>(null);

	const pushTag = async (tagName: string) => {
		if (!repositoryId || !remoteName || busyTag) return;
		setBusyTag(tagName);
		const toastId = toast.loading(`Pushing ${tagName} to ${remoteName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { remoteName, repositoryId, tagName },
					commandType: NativeMessageType.PushTag,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			toast.success(`Pushed ${tagName} to ${remoteName}`, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not push tag.",
				{ id: toastId },
			);
		} finally {
			setBusyTag(null);
		}
	};

	const deleteTag = async () => {
		if (!repositoryId || !deleteTagName || busyTag) return;
		const tagName = deleteTagName;
		setBusyTag(tagName);
		const toastId = toast.loading(`Deleting ${tagName}`);
		try {
			await sendRequestWithResponse(
				{
					arguments: { repositoryId, tagName },
					commandType: NativeMessageType.DeleteTag,
				},
				{ timeoutMs: gitMutationTimeoutMs },
			);
			setDeleteTagName(null);
			onRepositoryChanged();
			toast.success(`Deleted local tag ${tagName}`, { id: toastId });
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not delete tag.",
				{ id: toastId },
			);
		} finally {
			setBusyTag(null);
		}
	};

	return { busyTag, deleteTag, deleteTagName, pushTag, setDeleteTagName };
}
