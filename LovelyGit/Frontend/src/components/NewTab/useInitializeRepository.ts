import { useState } from "react";
import { toast } from "sonner";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { useRepositoryContext } from "@/lib/repositoryContext";

export function useInitializeRepository() {
	const repositories = useRepositoryContext();
	const [directoryName, setDirectoryName] = useState("");
	const [initialBranchName, setInitialBranchName] = useState("main");
	const [isBusy, setIsBusy] = useState(false);
	const [open, setOpen] = useState(false);
	const [parentPath, setParentPath] = useState("");
	const canInitialize = Boolean(
		parentPath.trim() &&
			directoryName.trim() &&
			initialBranchName.trim() &&
			!isBusy,
	);

	const chooseDestination = async () => {
		try {
			const result = await sendRequestWithResponse({
				commandType: NativeMessageType.ChooseRepositoryDestination,
			});
			if (result?.parentPath) setParentPath(result.parentPath);
		} catch (error) {
			toast.error(message(error, "Could not choose a destination folder."));
		}
	};

	const initializeRepository = async () => {
		if (!canInitialize) return;

		setIsBusy(true);
		try {
			const repository = await sendRequestWithResponse({
				arguments: {
					directoryName: directoryName.trim(),
					initialBranchName: initialBranchName.trim(),
					parentPath: parentPath.trim(),
				},
				commandType: NativeMessageType.InitializeRepository,
			});
			await repositories.reloadRepositories();
			await repositories.setCurrentRepositoryId(repository.id);
			setOpen(false);
			toast.success(`Created ${repository.name || directoryName.trim()}`);
			resetForm();
		} catch (error) {
			toast.error(message(error, "Could not create the repository."));
		} finally {
			setIsBusy(false);
		}
	};

	const resetForm = () => {
		setDirectoryName("");
		setInitialBranchName("main");
		setParentPath("");
	};

	return {
		canInitialize,
		chooseDestination,
		directoryName,
		initialBranchName,
		initializeRepository,
		isBusy,
		open,
		parentPath,
		setDirectoryName,
		setInitialBranchName,
		setOpen,
		setParentPath,
	};
}

function message(error: unknown, fallback: string) {
	return error instanceof Error ? error.message : fallback;
}
