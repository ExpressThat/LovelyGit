import { useState } from "react";
import { toast } from "sonner";
import type { GitSubmodule, SubmoduleAction } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function useSubmoduleManager(repositoryId: string | null) {
	const [submodules, setSubmodules] = useState<GitSubmodule[]>([]);
	const [isLoading, setIsLoading] = useState(false);
	const [busyPath, setBusyPath] = useState<string | null>(null);
	const [error, setError] = useState<string | null>(null);

	async function load() {
		if (!repositoryId || isLoading) return;
		setIsLoading(true);
		setError(null);
		try {
			const response = await sendRequestWithResponse({
				commandType: NativeMessageType.GetSubmodules,
				arguments: { repositoryId },
			});
			setSubmodules(response ?? []);
		} catch (loadError) {
			setError(message(loadError, "Could not read submodules"));
		} finally {
			setIsLoading(false);
		}
	}

	async function run(path: string, action: SubmoduleAction) {
		if (!repositoryId || busyPath) return;
		setBusyPath(path);
		try {
			await sendRequestWithResponse(
				{
					commandType: NativeMessageType.ManageSubmodule,
					arguments: { action, path, repositoryId },
				},
				{ timeoutMs: 120_000 },
			);
			toast.success(successMessage(action));
			await loadAfterMutation(repositoryId, setSubmodules);
		} catch (runError) {
			toast.error(message(runError, "Git could not update this submodule"), {
				duration: 8_000,
			});
		} finally {
			setBusyPath(null);
		}
	}

	return { busyPath, error, isLoading, load, run, submodules };
}

async function loadAfterMutation(
	repositoryId: string,
	setSubmodules: (value: GitSubmodule[]) => void,
) {
	const response = await sendRequestWithResponse({
		commandType: NativeMessageType.GetSubmodules,
		arguments: { repositoryId },
	});
	setSubmodules(response ?? []);
}

function successMessage(action: SubmoduleAction) {
	if (action === "Initialize") return "Submodule initialized";
	if (action === "Update") return "Submodule updated";
	if (action === "Synchronize") return "Submodule URLs synchronized";
	return "Submodule deinitialized";
}

function message(error: unknown, fallback: string) {
	return error instanceof Error && error.message ? error.message : fallback;
}
