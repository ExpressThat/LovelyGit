import { useState } from "react";
import { toast } from "sonner";
import { FolderOpen, LoaderCircle } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import { sendRequestWithResponse } from "@/lib/commands";
import { nativeDialogTimeoutMs } from "@/lib/nativeDialogTimeout";
import { useRepositoryContext } from "@/lib/repositoryContext";

export function OpenRepoButton() {
	const knownRepositorys = useRepositoryContext();
	const [isSelecting, setIsSelecting] = useState(false);
	const openRepository = async () => {
		if (isSelecting) return;
		setIsSelecting(true);
		try {
			const result = await sendRequestWithResponse(
				{ commandType: "AddKnownGitRepositorys" },
				{ timeoutMs: nativeDialogTimeoutMs },
			);
			if (result != null) {
				knownRepositorys.reconcileRepository(result);
				await knownRepositorys.setCurrentRepositoryId(result.id);
			}
		} catch (error) {
			toast.error(
				error instanceof Error
					? error.message
					: "Could not open the repository.",
			);
		} finally {
			setIsSelecting(false);
		}
	};

	return (
		<Button
			disabled={isSelecting}
			onClick={() => void openRepository()}
			size="sm"
			title="Open a Git repository"
			type="button"
			variant="secondary"
		>
			{isSelecting ? (
				<LoaderCircle aria-hidden="true" className="animate-spin" />
			) : (
				<FolderOpen aria-hidden="true" />
			)}
			{isSelecting ? "Selecting" : "Open Repo"}
		</Button>
	);
}
