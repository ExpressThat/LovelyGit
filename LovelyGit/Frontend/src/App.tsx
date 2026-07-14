import "./App.css";
import { useState } from "react";
import { AnimatePresence, motion } from "@/lib/motion";
import { AppCommitDetailsPanel } from "./AppCommitDetailsPanel";
import * as LazySurfaces from "./AppLazySurfaces";
import { AppOverlaysContainer } from "./AppOverlaysContainer";
import { commitDetailsPanel, panelTitle } from "./AppPanelState";
import { CommitGraphLayer } from "./components/CommitGraph/CommitGraphLayer";
import { SlidingDetailsPanel } from "./components/DetailsPanel/SlidingDetailsPanel";
import { workspaceDrillInLayerClassName } from "./components/layout/workspaceLayering";
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
import { useRepositoryScopedDetailsPanel } from "./useRepositoryScopedDetailsPanel";

function App() {
	useApplyTheme();
	useApplyFont();
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	const [commitGraphRefreshToken, setCommitGraphRefreshToken] = useState(0);
	const overlays = useAppOverlayState(Boolean(currentGitRepositoryId));
	const fileDiscovery = useFileDiscoveryTargets();
	const [currentBranchName, setCurrentBranchName] = useState<string | null>(
		null,
	);
	const [scopedDetailsPanel, setDetailsPanel] = useRepositoryScopedDetailsPanel(
		currentGitRepositoryId,
		setCurrentBranchName,
		overlays.resetRepositoryOverlays,
		fileDiscovery.reset,
	);
	const isWorkingChangesPanelOpen =
		scopedDetailsPanel?.kind === "workingChanges";
	const workingTreeChanges = useWorkingTreeChanges(
		currentGitRepositoryId,
		isWorkingChangesPanelOpen,
	);
	const refreshRepository = createRepositoryRefreshAction(
		workingTreeChanges.reload,
		setCommitGraphRefreshToken,
	);
	const selectCommit = (row: CommitGraphRow) => {
		void LazySurfaces.preloadCommitFileDiffSurface();
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
										scopedDetailsPanel?.kind === "commit" &&
											scopedDetailsPanel.selectedFile,
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
										scopedDetailsPanel?.kind === "commit"
											? scopedDetailsPanel.commitHash
											: null
									}
								/>
								<AnimatePresence initial={false}>
									{scopedDetailsPanel?.kind === "commit" &&
									scopedDetailsPanel.selectedFile ? (
										<motion.div
											animate={{ opacity: 1, x: 0, scale: 1 }}
											className={workspaceDrillInLayerClassName}
											exit={{ opacity: 0, x: 56, scale: 0.995 }}
											initial={{ opacity: 0, x: 56, scale: 0.995 }}
											key={`diff:${scopedDetailsPanel.commitHash}:${scopedDetailsPanel.parentIndex ?? 0}:${scopedDetailsPanel.selectedFile.path}`}
											transition={{
												duration: 0.24,
												ease: [0.22, 1, 0.36, 1],
											}}
										>
											<LazySurfaces.CommitFileDiffSurface
												commitHash={scopedDetailsPanel.commitHash}
												file={scopedDetailsPanel.selectedFile}
												onClose={() =>
													setDetailsPanel(
														commitDetailsPanel(
															scopedDetailsPanel.commitHash,
															scopedDetailsPanel.parentIndex,
														),
													)
												}
												parentIndex={scopedDetailsPanel.parentIndex ?? 0}
												repositoryId={currentGitRepositoryId}
											/>
										</motion.div>
									) : null}
									{scopedDetailsPanel?.kind === "workingChanges" &&
									scopedDetailsPanel.selectedFile &&
									currentGitRepositoryId ? (
										<motion.div
											animate={{ opacity: 1, x: 0, scale: 1 }}
											className={workspaceDrillInLayerClassName}
											exit={{ opacity: 0, x: 56, scale: 0.995 }}
											initial={{ opacity: 0, x: 56, scale: 0.995 }}
											key={`working-diff:${scopedDetailsPanel.selectedFile.group}:${scopedDetailsPanel.selectedFile.path}`}
											transition={{
												duration: 0.24,
												ease: [0.22, 1, 0.36, 1],
											}}
										>
											<LazySurfaces.WorkingTreeDiffSurface
												file={scopedDetailsPanel.selectedFile}
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
						isOpen={Boolean(scopedDetailsPanel && currentGitRepositoryId)}
						onClose={() => setDetailsPanel(null)}
						title={panelTitle(scopedDetailsPanel)}
					>
						{scopedDetailsPanel?.kind === "commit" && currentGitRepositoryId ? (
							<AppCommitDetailsPanel
								onOpenFileBlame={(file) =>
									fileDiscovery.openBlame(
										file.path,
										scopedDetailsPanel.commitHash,
									)
								}
								onOpenFileHistory={(file) =>
									fileDiscovery.openHistory(
										file.path,
										scopedDetailsPanel.commitHash,
									)
								}
								onPanelChange={setDetailsPanel}
								panel={scopedDetailsPanel}
								refreshToken={commitGraphRefreshToken}
								repositoryId={currentGitRepositoryId}
							/>
						) : null}
						{scopedDetailsPanel?.kind === "workingChanges" &&
						currentGitRepositoryId ? (
							<LazySurfaces.WorkingChangesSurface
								changes={workingTreeChanges.changes}
								error={
									workingTreeChanges.status === "error"
										? workingTreeChanges.message
										: null
								}
								isLoading={
									workingTreeChanges.status === "loading" ||
									workingTreeChanges.isReloading
								}
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
