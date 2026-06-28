import { toast } from "sonner";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export async function revealWorkingTreeFile(
	repositoryId: string,
	path: string,
) {
	try {
		await sendRequestWithResponse({
			arguments: { path, repositoryId },
			commandType: NativeMessageType.RevealWorkingTreeFile,
		});
		toast.success("Opened file location");
	} catch (error) {
		toast.error(
			error instanceof Error ? error.message : "Could not reveal file location",
		);
	}
}
