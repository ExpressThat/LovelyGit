import { useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import type { CloneRepositoryProgressNotification } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { inferCloneDirectoryName } from "./CloneRepositoryHelpers";

const cloneTimeoutMs = 12 * 60 * 60 * 1000;
type CloneStatus = "idle" | "cloning" | "canceling";

export function useCloneRepository() {
	const repositories = useRepositoryContext();
	const cancelRequestedRef = useRef(false);
	const operationIdRef = useRef<string | null>(null);
	const [directoryName, setDirectoryName] = useState("");
	const [directoryNameEdited, setDirectoryNameEdited] = useState(false);
	const [open, setOpen] = useState(false);
	const [parentPath, setParentPath] = useState("");
	const [progress, setProgress] =
		useState<CloneRepositoryProgressNotification | null>(null);
	const [recurseSubmodules, setRecurseSubmodules] = useState(false);
	const [remoteUrl, setRemoteUrl] = useState("");
	const [shallow, setShallow] = useState(false);
	const [status, setStatus] = useState<CloneStatus>("idle");
	const isBusy = status !== "idle";
	const canClone = Boolean(
		remoteUrl.trim() && parentPath.trim() && directoryName.trim() && !isBusy,
	);

	useEffect(
		() =>
			subscribeToServerEvent("CloneRepositoryProgress", (notification) => {
				if (notification.operationId === operationIdRef.current) {
					setProgress(notification);
				}
			}),
		[],
	);

	const updateRemoteUrl = (value: string) => {
		setRemoteUrl(value);
		if (!directoryNameEdited) setDirectoryName(inferCloneDirectoryName(value));
	};
	const updateDirectoryName = (value: string) => {
		setDirectoryNameEdited(true);
		setDirectoryName(value);
	};
	const chooseDestination = async () => {
		try {
			const result = await sendRequestWithResponse(
				{ commandType: NativeMessageType.ChooseCloneDestination },
				{ timeoutMs: nativeDialogTimeoutMs },
			);
			if (result?.parentPath) setParentPath(result.parentPath);
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not choose a destination folder.",
			);
		}
	};

	const resetForm = () => {
		setRemoteUrl("");
		setParentPath("");
		setDirectoryName("");
		setDirectoryNameEdited(false);
		setShallow(false);
		setRecurseSubmodules(false);
		setProgress(null);
	};
	const cloneRepository = async () => {
		if (!canClone) return;

		const operationId = crypto.randomUUID();
		operationIdRef.current = operationId;
		cancelRequestedRef.current = false;
		setProgress({
			message: "Preparing destination",
			operationId,
			phasePercent: null,
			percent: null,
			stage: "Preparing",
		});
		setStatus("cloning");
		try {
			const repository = await sendRequestWithResponse(
				{
					arguments: {
						directoryName: directoryName.trim(),
						operationId,
						parentPath: parentPath.trim(),
						recurseSubmodules,
						remoteUrl: remoteUrl.trim(),
						shallow,
					},
					commandType: NativeMessageType.CloneRepository,
				},
				{ timeoutMs: cloneTimeoutMs },
			);
			await repositories.reloadRepositories();
			await repositories.setCurrentRepositoryId(repository.id);
			setOpen(false);
			toast.success(`Cloned ${repository.name || directoryName.trim()}`);
			resetForm();
		} catch (error) {
			if (cancelRequestedRef.current) toast.info("Clone canceled");
			else {
				toast.error(
					error instanceof Error ? error.message : "Repository clone failed.",
				);
			}
		} finally {
			operationIdRef.current = null;
			setStatus("idle");
		}
	};

	const cancelClone = async () => {
		const operationId = operationIdRef.current;
		if (!operationId || status !== "cloning") return;

		cancelRequestedRef.current = true;
		setStatus("canceling");
		setProgress((current) =>
			current
				? { ...current, message: "Stopping clone…", stage: "Canceling" }
				: current,
		);
		try {
			await sendRequestWithResponse({
				arguments: { operationId },
				commandType: NativeMessageType.CancelCloneRepository,
			});
		} catch (error) {
			cancelRequestedRef.current = false;
			setStatus("cloning");
			toast.error(
				error instanceof Error ? error.message : "Could not cancel the clone.",
			);
		}
	};

	return {
		canClone,
		cancelClone,
		chooseDestination,
		cloneRepository,
		directoryName,
		isBusy,
		open,
		parentPath,
		progress,
		recurseSubmodules,
		remoteUrl,
		setOpen,
		setParentPath,
		setRecurseSubmodules,
		setShallow,
		shallow,
		status,
		updateDirectoryName,
		updateRemoteUrl,
	};
}
