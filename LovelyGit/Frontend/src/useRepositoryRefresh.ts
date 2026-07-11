import type { Dispatch, SetStateAction } from "react";
import { toast } from "sonner";

export function createRepositoryRefreshAction(
	reloadWorkingTree: () => Promise<void>,
	setGraphRefreshToken: Dispatch<SetStateAction<number>>,
) {
	return async () => {
		try {
			await reloadWorkingTree();
			setGraphRefreshToken((token) => token + 1);
			toast.success("Repository refreshed");
		} catch (error) {
			toast.error(
				error instanceof Error ? error.message : "Could not refresh repository",
			);
		}
	};
}
