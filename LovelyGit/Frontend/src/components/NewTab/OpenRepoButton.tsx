import { FolderOpen } from "lucide-react";
import { Button } from "@/components/ui/button";
import { sendRequestWithResponse } from "@/lib/commands";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { setSetting } from "@/lib/settings/settingsStore";

export function OpenRepoButton() {
	const knownRepositorys = useRepositoryContext();

	return (
		<Button
			onClick={async () => {
				const result = await sendRequestWithResponse({
					commandType: "AddKnownGitRepositorys",
				});
				if (result != null) {
					await knownRepositorys.reloadRepositories();
					await setSetting("CurrentGitRepositoryId", result.id);
				}
			}}
			size="sm"
			title="Open a Git repository"
			type="button"
			variant="secondary"
		>
			<FolderOpen aria-hidden="true" />
			Open Repo
		</Button>
	);
}
