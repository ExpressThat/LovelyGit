import "./App.css";
import { AnimatePresence, motion } from "motion/react";
import { useEffect, useRef, useState } from "react";
import { CommitDetails } from "./components/CommitDetails/CommitDetails";
import { CommitFileDiffView } from "./components/CommitFileDiff/CommitFileDiffView";
import { CommitGraphView } from "./components/CommitGraph/CommitGraphView";
import { SlidingDetailsPanel } from "./components/DetailsPanel/SlidingDetailsPanel";
import { NewTab } from "./components/NewTab/NewTab";
import { TopNavBar } from "./components/TopNavBar/TopNavBar";
import { useWorkingTreeChanges } from "./components/WorkingChanges/useWorkingTreeChanges";
import { WorkingChangesPanel } from "./components/WorkingChanges/WorkingChangesPanel";
import { WorkingTreeFileDiffView } from "./components/WorkingChanges/WorkingTreeFileDiffView";
import { Toaster } from "./components/ui/sonner";
import type { CommitChangedFile } from "./generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import type { WorkingTreeChangedFile } from "./generated/ExpressThat.LovelyGit.Services.Git.WorkingTree.Models";
import { sendRequestWithoutResponse } from "./lib/registerSignalR";
import { RepositoryProvider } from "./lib/repositoryContext";
import { useSetting } from "./lib/settings/settingsStore";

function App() {
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	const [detailsPanel, setDetailsPanel] = useState<DetailsPanelState | null>(
		null,
	);
	const [commitGraphRefreshToken, setCommitGraphRefreshToken] = useState(0);
	const workingTreeChanges = useWorkingTreeChanges(currentGitRepositoryId);
	const previousRepositoryIdRef = useRef<string | null>(currentGitRepositoryId);

	useEffect(() => {
		if (previousRepositoryIdRef.current === currentGitRepositoryId) {
			return;
		}

		if (detailsPanel?.kind === "commit" && previousRepositoryIdRef.current) {
			cancelCommitDiffPreparation(
				previousRepositoryIdRef.current,
				detailsPanel.commitHash,
			);
		}

		previousRepositoryIdRef.current = currentGitRepositoryId;
		setDetailsPanel(null);
	}, [currentGitRepositoryId, detailsPanel]);

	const closeDetailsPanel = () => {
		if (detailsPanel?.kind === "commit" && currentGitRepositoryId) {
			cancelCommitDiffPreparation(
				currentGitRepositoryId,
				detailsPanel.commitHash,
			);
		}

		setDetailsPanel(null);
	};

	return (
		<RepositoryProvider>
			<main className="app-shell">
					<TopNavBar
						onOpenWorkingChanges={() =>
							setDetailsPanel({ kind: "workingChanges" })
						}
						workingChangesCount={workingTreeChanges.changes?.totalCount ?? 0}
					/>
				<div className="flex min-h-0 flex-1 overflow-hidden">
					<div className="relative min-w-0 flex-1 overflow-hidden">
						{currentGitRepositoryId && (
							<>
								<motion.div
									animate={{
										opacity:
											detailsPanel?.kind === "commit" &&
											detailsPanel.selectedFile
												? 0.92
												: 1,
										scale:
											detailsPanel?.kind === "commit" &&
											detailsPanel.selectedFile
												? 0.998
												: 1,
									}}
									className="absolute inset-0 min-w-0 overflow-hidden"
									initial={false}
									transition={{
										duration: 0.18,
										ease: [0.22, 1, 0.36, 1],
									}}
								>
									<CommitGraphView
										onSelectCommit={(row) =>
											setDetailsPanel((currentPanel) => {
												if (
													currentPanel?.kind === "commit" &&
													currentPanel.commitHash === row.commit.hash
												) {
													return {
														commitHash: row.commit.hash,
														kind: "commit",
													};
												}

												return {
													commitHash: row.commit.hash,
													kind: "commit",
												};
											})
										}
										selectedCommitHash={
											detailsPanel?.kind === "commit"
												? detailsPanel.commitHash
												: null
										}
										refreshToken={commitGraphRefreshToken}
									/>
								</motion.div>
								<AnimatePresence initial={false}>
									{detailsPanel?.kind === "commit" &&
									detailsPanel.selectedFile ? (
										<motion.div
											animate={{ opacity: 1, x: 0, scale: 1 }}
											className="absolute inset-0 z-10 min-w-0 overflow-hidden"
											exit={{ opacity: 0, x: 56, scale: 0.995 }}
											initial={{ opacity: 0, x: 56, scale: 0.995 }}
											key={`diff:${detailsPanel.commitHash}:${detailsPanel.selectedFile.path}`}
											transition={{
												duration: 0.24,
												ease: [0.22, 1, 0.36, 1],
											}}
										>
											<CommitFileDiffView
												commitHash={detailsPanel.commitHash}
												file={detailsPanel.selectedFile}
												onClose={() =>
													setDetailsPanel({
														commitHash: detailsPanel.commitHash,
														kind: "commit",
													})
												}
												repositoryId={currentGitRepositoryId}
											/>
										</motion.div>
									) : null}
									{detailsPanel?.kind === "workingChanges" &&
									detailsPanel.selectedFile &&
									currentGitRepositoryId ? (
										<motion.div
											animate={{ opacity: 1, x: 0, scale: 1 }}
											className="absolute inset-0 z-10 min-w-0 overflow-hidden"
											exit={{ opacity: 0, x: 56, scale: 0.995 }}
											initial={{ opacity: 0, x: 56, scale: 0.995 }}
											key={`working-diff:${detailsPanel.selectedFile.group}:${detailsPanel.selectedFile.path}`}
											transition={{
												duration: 0.24,
												ease: [0.22, 1, 0.36, 1],
											}}
										>
											<WorkingTreeFileDiffView
												file={detailsPanel.selectedFile}
												onClose={() =>
													setDetailsPanel({
														kind: "workingChanges",
													})
												}
												repositoryId={currentGitRepositoryId}
											/>
										</motion.div>
									) : null}
								</AnimatePresence>
							</>
						)}
						{!currentGitRepositoryId && (
							<motion.div
								animate={{ opacity: 1 }}
								className="absolute inset-0 min-w-0 overflow-hidden"
								initial={{ opacity: 0 }}
								transition={{ duration: 0.18 }}
							>
								<NewTab />
							</motion.div>
						)}
					</div>
					<SlidingDetailsPanel
						isOpen={Boolean(detailsPanel && currentGitRepositoryId)}
						onClose={closeDetailsPanel}
						title={panelTitle(detailsPanel)}
					>
						{detailsPanel?.kind === "commit" && currentGitRepositoryId ? (
							<CommitDetails
								commitHash={detailsPanel.commitHash}
								onSelectFile={(file) =>
									setDetailsPanel({
										commitHash: detailsPanel.commitHash,
										kind: "commit",
										selectedFile: file,
									})
								}
								repositoryId={currentGitRepositoryId}
							/>
						) : null}
						{detailsPanel?.kind === "workingChanges" &&
						currentGitRepositoryId ? (
							<WorkingChangesPanel
								changes={workingTreeChanges.changes}
								error={
									workingTreeChanges.status === "error"
										? workingTreeChanges.message
										: null
								}
								isLoading={workingTreeChanges.status === "loading"}
								onRefresh={() => {
									return workingTreeChanges.reload();
								}}
								onCommitSuccess={() => {
									setCommitGraphRefreshToken((token) => token + 1);
									return workingTreeChanges.reload();
								}}
								onSelectFile={(file) =>
									setDetailsPanel({
										kind: "workingChanges",
										selectedFile: file,
									})
								}
								repositoryId={currentGitRepositoryId}
							/>
						) : null}
					</SlidingDetailsPanel>
				</div>
			</main>
			<Toaster />
		</RepositoryProvider>
	);
}

type DetailsPanelState =
	| {
			commitHash: string;
			kind: "commit";
			selectedFile?: CommitChangedFile;
	  }
	| {
			kind: "workingChanges";
			selectedFile?: WorkingTreeChangedFile;
	  };

function cancelCommitDiffPreparation(repositoryId: string, commitHash: string) {
	void sendRequestWithoutResponse({
		commandType: "CancelCommitDiffPreparation",
		arguments: {
			commitHash,
			repositoryId,
		},
	});
}

function panelTitle(panel: DetailsPanelState | null) {
	if (panel?.kind === "commit") {
		return "Commit Details";
	}

	if (panel?.kind === "workingChanges") {
		return "Working Changes";
	}

	return "Details";
}

export default App;
