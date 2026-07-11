import "./App.css";
import { AnimatePresence, motion } from "motion/react";
import { useState } from "react";
import { AppCommitDetailsPanel } from "./AppCommitDetailsPanel";
import * as LazySurfaces from "./AppLazySurfaces";
import { AppOverlaysContainer } from "./AppOverlaysContainer";
import {
	commitDetailsPanel,
	type DetailsPanelState,
	panelTitle,
} from "./AppPanelState";
import { CommitGraphLayer } from "./components/CommitGraph/CommitGraphLayer";
import { SlidingDetailsPanel } from "./components/DetailsPanel/SlidingDetailsPanel";
import { TopNavBar } from "./components/TopNavBar/TopNavBar";
import { useWorkingTreeChanges } from "./components/WorkingChanges/useWorkingTreeChanges";
import type { CommitGraphRow } from "./generated/types";
import { RepositoryProvider } from "./lib/repositoryContext";
import { useApplyFont } from "./lib/settings/font/useApplyFont";
import { useSetting } from "./lib/settings/settingsStore";
import { useApplyTheme } from "./lib/settings/theme/useApplyTheme";
import { NewTabSurface } from "./NewTabSurface";
import { useAppOverlayState } from "./useAppOverlayState";
import { useFileDiscoveryTargets } from "./useFileDiscoveryTargets";
import { createRepositoryRefreshAction } from "./useRepositoryRefresh";
import { useResetOnRepositoryChange } from "./useResetOnRepositoryChange";

function App() {
	useApplyTheme();
	useApplyFont();
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	const [detailsPanel, setDetailsPanel] = useState<DetailsPanelState | null>(
		null,
	);
	const [commitGraphRefreshToken, setCommitGraphRefreshToken] = useState(0);
	const overlays = useAppOverlayState(Boolean(currentGitRepositoryId));
	const fileDiscovery = useFileDiscoveryTargets();
	const [currentBranchName, setCurrentBranchName] = useState<string | null>(
		null,
	);
	const isWorkingChangesPanelOpen = detailsPanel?.kind === "workingChanges";
	const workingTreeChanges = useWorkingTreeChanges(
		currentGitRepositoryId,
		isWorkingChangesPanelOpen,
	);
	const refreshRepository = createRepositoryRefreshAction(
		workingTreeChanges.reload,
		setCommitGraphRefreshToken,
	);
	useResetOnRepositoryChange(
		currentGitRepositoryId,
		setCurrentBranchName,
		setDetailsPanel,
		overlays.resetRepositoryOverlays,
		fileDiscovery.reset,
	);
	const selectCommit = (row: CommitGraphRow) => {
		setDetailsPanel({ commitHash: row.commit.hash, kind: "commit" });
	};
	return (
		<RepositoryProvider>
			<main className="app-shell">
				<TopNavBar
					currentBranchName={currentBranchName}
					onOpenCommandPalette={() => overlays.setCommandPaletteOpen(true)}
					onBranchChanged={(branchName) => {
						setCurrentBranchName(branchName);
						setDetailsPanel(null);
						setCommitGraphRefreshToken((token) => token + 1);
					}}
					onOpenWorkingChanges={() =>
						setDetailsPanel({ kind: "workingChanges" })
					}
					onSearchCommits={() => overlays.setCommitSearchOpen(true)}
					onSettingsOpenChange={overlays.setSettingsOpen}
					repositoryId={currentGitRepositoryId}
					settingsOpen={overlays.settingsOpen}
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
											key={`diff:${detailsPanel.commitHash}:${detailsPanel.parentIndex ?? 0}:${detailsPanel.selectedFile.path}`}
											transition={{
												duration: 0.24,
												ease: [0.22, 1, 0.36, 1],
											}}
										>
											<LazySurfaces.CommitFileDiffSurface
												commitHash={detailsPanel.commitHash}
												file={detailsPanel.selectedFile}
												onClose={() =>
													setDetailsPanel(
														commitDetailsPanel(
															detailsPanel.commitHash,
															detailsPanel.parentIndex,
														),
													)
												}
												parentIndex={detailsPanel.parentIndex ?? 0}
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
								<NewTabSurface />
							</motion.div>
						)}
					</div>
					<SlidingDetailsPanel
						isOpen={Boolean(detailsPanel && currentGitRepositoryId)}
						onClose={() => setDetailsPanel(null)}
						title={panelTitle(detailsPanel)}
					>
						{detailsPanel?.kind === "commit" && currentGitRepositoryId ? (
							<AppCommitDetailsPanel
								onOpenFileBlame={(file) =>
									fileDiscovery.openBlame(file.path, detailsPanel.commitHash)
								}
								onOpenFileHistory={(file) =>
									fileDiscovery.openHistory(file.path, detailsPanel.commitHash)
								}
								onPanelChange={setDetailsPanel}
								panel={detailsPanel}
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
			<AppOverlaysContainer
				canCreateStash={workingTreeChanges.totalCount > 0}
				currentBranchName={currentBranchName}
				fileDiscovery={fileDiscovery}
				onRefreshRepository={refreshRepository}
				onRepositoryChanged={() =>
					setCommitGraphRefreshToken((token) => token + 1)
				}
				overlays={overlays}
				repositoryId={currentGitRepositoryId}
				setCurrentBranchName={setCurrentBranchName}
				setDetailsPanel={setDetailsPanel}
			/>
		</RepositoryProvider>
	);
}
export default App;
