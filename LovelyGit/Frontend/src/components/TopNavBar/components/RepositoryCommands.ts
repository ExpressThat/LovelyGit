import { toast } from "sonner";
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
