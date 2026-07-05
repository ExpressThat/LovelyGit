import { SquareTerminal } from "lucide-react";
import { Button } from "@/components/ui/button";
import { openRepositoryTerminal } from "./RepositoryCommands";

export function TerminalActionControl({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	const openTerminal = async () => {
		if (!repositoryId) {
			return;
		}

		await openRepositoryTerminal(repositoryId);
	};

	return (
		<Button
			aria-label="Open terminal at repository"
			className="h-9 px-3"
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
			<SquareTerminal aria-hidden="true" className="size-6" />
			<span>Terminal</span>
		</Button>
	);
}
