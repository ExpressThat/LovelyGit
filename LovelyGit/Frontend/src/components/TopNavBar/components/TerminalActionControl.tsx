import { SquareTerminal } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

export function TerminalActionControl({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	const openTerminal = async () => {
		if (!repositoryId) {
			return;
		}

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
	};

	return (
		<Button
			aria-label="Open terminal at repository"
			className="h-8"
			disabled={!repositoryId}
			onClick={() => void openTerminal()}
			size="sm"
			title={
				repositoryId
					? "Open terminal at repository"
					: "Select a repository to open a terminal"
			}
			type="button"
			variant="ghost"
		>
			<SquareTerminal aria-hidden="true" />
			<span>Terminal</span>
		</Button>
	);
}
