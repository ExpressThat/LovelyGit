import { sendRequestWithResponse } from "@/lib/commands";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { setSetting } from "@/lib/settings/settingsStore";

export function OpenRepoButton() {
	const knownRepositorys = useRepositoryContext();

	return (
		<button
			className="h-7 cursor-pointer rounded-md border border-input bg-secondary px-2 text-[11px] font-semibold leading-3.5 text-secondary-foreground transition-colors hover:bg-accent hover:text-accent-foreground"
			onClick={async () => {
				const result = await sendRequestWithResponse({
					commandType: "AddKnownGitRepositorys",
				});
				if (result != null) {
					await knownRepositorys.reloadRepositories();
					await setSetting("CurrentGitRepositoryId", result.id);
				}
			}}
			type="button"
		>
			Open Repo
		</button>
	);
}
