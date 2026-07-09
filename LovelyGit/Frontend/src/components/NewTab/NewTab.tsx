import { OpenRepoButton } from "./OpenRepoButton";
import { RecentRepositories } from "./RecentRepositories";

export function NewTab() {
	return (
		<div className="flex h-full min-h-0 flex-col items-center overflow-auto px-6 py-8">
			<div className="flex w-full max-w-2xl items-center justify-between gap-3">
				<div className="min-w-0">
					<h1 className="font-semibold text-lg">Open a repository</h1>
					<p className="text-muted-foreground text-sm">
						Choose a recent repository or add one from disk.
					</p>
				</div>
				<div className="flex shrink-0 items-center gap-2">
					<OpenRepoButton />
					<CloneRepositoryDialog />
				</div>
			</div>
			<RecentRepositories />
		</div>
	);
}

import { CloneRepositoryDialog } from "./CloneRepositoryDialog";
