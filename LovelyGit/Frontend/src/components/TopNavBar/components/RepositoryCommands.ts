import { toast } from "sonner";
import type { RemoteWebResourceKind } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export async function openRepositoryTerminal(repositoryId: string) {
	toast.success("Opening terminal at repository");
	try {
		await sendRequestWithResponse(
			{
				arguments: { knownRepositoryId: repositoryId },
				commandType: NativeMessageType.OpenRepositoryTerminal,
			},
			{ timeoutMs: 2_000 },
		);
	} catch (error) {
		if (error instanceof Error && error.message.includes("Timed out")) {
			return;
		}

		toast.error(
			error instanceof Error ? error.message : "Could not open terminal",
		);
	}
}

export async function revealKnownRepository(repositoryId: string) {
	try {
		await sendRequestWithResponse({
			arguments: { knownRepositoryId: repositoryId },
			commandType: "RevealKnownGitRepository",
		});
	} catch (error) {
		toast.error(
			error instanceof Error
				? error.message
				: "Could not reveal the repository",
		);
	}
}

export async function openRemoteWebResource(
	repositoryId: string,
	kind: RemoteWebResourceKind,
	value: string | null = null,
	targetValue: string | null = null,
) {
	try {
		await sendRequestWithResponse({
			arguments: { knownRepositoryId: repositoryId, kind, targetValue, value },
			commandType: NativeMessageType.OpenRemoteWebResource,
		});
	} catch (error) {
		toast.error(
			error instanceof Error ? error.message : "Could not open remote website",
		);
	}
}
