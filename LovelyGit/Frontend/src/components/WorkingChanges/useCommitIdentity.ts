import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";
import type { GitCommitIdentity } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useCommitIdentity(repositoryId: string) {
	const [identity, setIdentity] = useState<GitCommitIdentity | null>(null);
	const [error, setError] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(true);
	const [isSaving, setIsSaving] = useState(false);
	const requestId = useRef(0);

	const load = useCallback(async () => {
		const currentRequest = ++requestId.current;
		setIsLoading(true);
		setError(null);
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.GetCommitIdentity,
				arguments: { repositoryId },
			});
			if (currentRequest === requestId.current) setIdentity(response);
		} catch (caught) {
			if (currentRequest === requestId.current) {
				setError(errorMessage(caught, "Could not read the commit identity"));
			}
		} finally {
			if (currentRequest === requestId.current) setIsLoading(false);
		}
	}, [repositoryId]);

	useEffect(() => {
		setIdentity(null);
		void load();
		return () => {
			requestId.current++;
		};
	}, [load]);

	async function save(name: string, email: string) {
		return mutate(false, name, email);
	}

	async function clear() {
		return mutate(true, null, null);
	}

	async function mutate(
		clearRepositoryOverride: boolean,
		name: string | null,
		email: string | null,
	) {
		if (isSaving) return false;
		setIsSaving(true);
		setError(null);
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.ManageCommitIdentity,
				arguments: {
					clearRepositoryOverride,
					email,
					name,
					repositoryId,
				},
			});
			setIdentity(response);
			toast.success(
				clearRepositoryOverride
					? "Using your Git default identity"
					: "Repository commit identity saved",
			);
			return true;
		} catch (caught) {
			const message = errorMessage(
				caught,
				"Could not update the commit identity",
			);
			setError(message);
			toast.error(message);
			return false;
		} finally {
			setIsSaving(false);
		}
	}

	return { clear, error, identity, isLoading, isSaving, load, save };
}

function errorMessage(error: unknown, fallback: string) {
	return error instanceof Error && error.message ? error.message : fallback;
}
