import { useCallback, useState } from "react";
import type { FileBlameTarget } from "./components/FileBlame/FileBlameDialog";
import type { FileHistoryTarget } from "./components/FileHistory/FileHistoryDialog";

export function useFileDiscoveryTargets() {
	const [blameTarget, setBlameTarget] = useState<FileBlameTarget | null>(null);
	const [historyTarget, setHistoryTarget] = useState<FileHistoryTarget | null>(
		null,
	);
	const openBlame = useCallback(
		(path: string, startCommitHash: string | null) =>
			setBlameTarget({ path, startCommitHash }),
		[],
	);
	const openHistory = useCallback(
		(path: string, startCommitHash: string | null) =>
			setHistoryTarget({ path, startCommitHash }),
		[],
	);
	const reset = useCallback(() => {
		setBlameTarget(null);
		setHistoryTarget(null);
	}, []);

	return {
		blameTarget,
		closeBlame: () => setBlameTarget(null),
		closeHistory: () => setHistoryTarget(null),
		historyTarget,
		openBlame,
		openHistory,
		reset,
	};
}
