import { ExternalLink } from "lucide-react";
import { Button } from "@/components/ui/button";
import { openRemoteWebResource } from "./RepositoryCommands";

export function RemoteWebActionControl({
	repositoryId,
}: {
	repositoryId: string | null;
}) {
	return (
		<Button
			aria-label="Open repository on remote website"
			className="h-9 px-3"
			disabled={!repositoryId}
			onClick={() => {
				if (repositoryId)
					void openRemoteWebResource(repositoryId, "Repository");
			}}
			size="sm"
			title={
				repositoryId
					? "Open repository on remote website"
					: "Select a repository first"
			}
			type="button"
			variant="ghost"
		>
			<ExternalLink aria-hidden="true" className="size-5" />
			<span>Remote</span>
		</Button>
	);
}
