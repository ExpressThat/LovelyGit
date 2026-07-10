import "./App.css";
import { AnimatePresence, motion } from "motion/react";
import { useEffect, useState } from "react";
import * as LazySurfaces from "./AppLazySurfaces";
import { AppOverlays } from "./AppOverlays";
import { type DetailsPanelState, panelTitle } from "./AppPanelState";
import { CommitGraphLayer } from "./components/CommitGraph/CommitGraphLayer";
import { isCommitSearchShortcut } from "./components/CommitSearch/commitSearchShortcut";
import { SlidingDetailsPanel } from "./components/DetailsPanel/SlidingDetailsPanel";
import { NewTab } from "./components/NewTab/NewTab";
import { TopNavBar } from "./components/TopNavBar/TopNavBar";
import { useWorkingTreeChanges } from "./components/WorkingChanges/useWorkingTreeChanges";
import type { CommitGraphRow } from "./generated/types";
import { RepositoryProvider } from "./lib/repositoryContext";
import { useApplyFont } from "./lib/settings/font/useApplyFont";
import { useSetting } from "./lib/settings/settingsStore";
import { useApplyTheme } from "./lib/settings/theme/useApplyTheme";
import { useFileDiscoveryTargets } from "./useFileDiscoveryTargets";
import { useResetOnRepositoryChange } from "./useResetOnRepositoryChange";

function App() {
	useApplyTheme();
	useApplyFont();
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	const [detailsPanel, setDetailsPanel] = useState<DetailsPanelState | null>(
		null,
	);
	const [commitGraphRefreshToken, setCommitGraphRefreshToken] = useState(0);
	const [isCommitSearchOpen, setIsCommitSearchOpen] = useState(false);
	const fileDiscovery = useFileDiscoveryTargets();
	const [currentBranchName, setCurrentBranchName] = useState<string | null>(
		null,
	);
	const isWorkingChangesPanelOpen = detailsPanel?.kind === "workingChanges";
	const workingTreeChanges = useWorkingTreeChanges(
		currentGitRepositoryId,
		isWorkingChangesPanelOpen,
	);
	useResetOnRepositoryChange(
		currentGitRepositoryId,
		setCurrentBranchName,
		setDetailsPanel,
		setIsCommitSearchOpen,
		fileDiscovery.reset,
	);
	const selectCommit = (row: CommitGraphRow) => {
		setDetailsPanel({ commitHash: row.commit.hash, kind: "commit" });
	};
	useEffect(() => {
		const openSearch = (event: KeyboardEvent) => {
			if (currentGitRepositoryId && isCommitSearchShortcut(event)) {
				event.preventDefault();
				setIsCommitSearchOpen(true);
			}
		};
		window.addEventListener("keydown", openSearch);
		return () => window.removeEventListener("keydown", openSearch);
	}, [currentGitRepositoryId]);
	const closeDetailsPanel = () => setDetailsPanel(null);
	return (
		<RepositoryProvider>
			<main className="app-shell">
				<TopNavBar
					currentBranchName={currentBranchName}
					onBranchChanged={(branchName) => {
						setCurrentBranchName(branchName);
						setDetailsPanel(null);
						setCommitGraphRefreshToken((token) => token + 1);
					}}
					onOpenWorkingChanges={() =>
						setDetailsPanel({ kind: "workingChanges" })
					}
					onSearchCommits={() => setIsCommitSearchOpen(true)}
					repositoryId={currentGitRepositoryId}
					workingChangesCount={workingTreeChanges.totalCount}
				/>
				<div className="flex min-h-0 flex-1 overflow-hidden">
					<div className="relative min-w-0 flex-1 overflow-hidden">
						{currentGitRepositoryId && (
							<>
								<CommitGraphLayer
									isDimmed={Boolean(
										detailsPanel?.kind === "commit" &&
											detailsPanel.selectedFile,
									)}
									onCurrentBranchNameChange={setCurrentBranchName}
									onOpenWorkingChanges={() =>
										setDetailsPanel({ kind: "workingChanges" })
									}
									onRepositoryChanged={() =>
										setCommitGraphRefreshToken((token) => token + 1)
									}
									onSelectCommit={selectCommit}
									refreshToken={commitGraphRefreshToken}
									repositoryId={currentGitRepositoryId}
									selectedCommitHash={
										detailsPanel?.kind === "commit"
											? detailsPanel.commitHash
											: null
									}
								/>
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
											<LazySurfaces.CommitFileDiffSurface
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
											<LazySurfaces.WorkingTreeDiffSurface
												file={detailsPanel.selectedFile}
												onChange={() => workingTreeChanges.reload()}
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
							<LazySurfaces.CommitDetailsSurface
								commitHash={detailsPanel.commitHash}
								onOpenFileBlame={(file) =>
									fileDiscovery.openBlame(file.path, detailsPanel.commitHash)
								}
								onOpenFileHistory={(file) =>
									fileDiscovery.openHistory(file.path, detailsPanel.commitHash)
								}
								onSelectFile={(file) =>
									setDetailsPanel({
										commitHash: detailsPanel.commitHash,
										kind: "commit",
										selectedFile: file,
									})
								}
								refreshToken={commitGraphRefreshToken}
								repositoryId={currentGitRepositoryId}
							/>
						) : null}
						{detailsPanel?.kind === "workingChanges" &&
						currentGitRepositoryId ? (
							<LazySurfaces.WorkingChangesSurface
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
								onOpenFileHistory={(file) =>
									fileDiscovery.openHistory(file.path, null)
								}
								onOpenFileBlame={(file) =>
									fileDiscovery.openBlame(file.path, null)
								}
								onSelectFile={(file) =>
									setDetailsPanel({
										kind: "workingChanges",
										selectedFile: file,
									})
								}
								repositoryId={currentGitRepositoryId}
								totalCount={workingTreeChanges.totalCount}
							/>
						) : null}
					</SlidingDetailsPanel>
				</div>
			</main>
			<AppOverlays
				fileBlameTarget={fileDiscovery.blameTarget}
				fileHistoryTarget={fileDiscovery.historyTarget}
				isCommitSearchOpen={isCommitSearchOpen}
				onFileBlameOpenChange={(open) => !open && fileDiscovery.closeBlame()}
				onFileHistoryOpenChange={(open) =>
					!open && fileDiscovery.closeHistory()
				}
				onSearchOpenChange={setIsCommitSearchOpen}
				onSelectCommit={(commitHash) =>
					setDetailsPanel({ commitHash, kind: "commit" })
				}
				repositoryId={currentGitRepositoryId}
			/>
		</RepositoryProvider>
	);
}
export default App;
