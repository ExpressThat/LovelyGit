import { motion } from "motion/react";
import type { CommitGraphRow } from "@/generated/types";
import { CommitGraphView } from "./CommitGraphView";

export function CommitGraphLayer({
	isDimmed,
	onSelectCommit,
	refreshToken,
	repositoryId,
	selectedCommitHash,
}: {
	isDimmed: boolean;
	onSelectCommit: (row: CommitGraphRow) => void;
	refreshToken: number;
	repositoryId: string;
	selectedCommitHash: string | null;
}) {
	return (
		<motion.div
			animate={{
				opacity: isDimmed ? 0.92 : 1,
				scale: isDimmed ? 0.998 : 1,
			}}
			className="absolute inset-0 min-w-0 overflow-hidden"
			initial={false}
			transition={{
				duration: 0.18,
				ease: [0.22, 1, 0.36, 1],
			}}
		>
			<CommitGraphView
				onSelectCommit={onSelectCommit}
				refreshToken={refreshToken}
				repositoryId={repositoryId}
				selectedCommitHash={selectedCommitHash}
			/>
		</motion.div>
	);
}
