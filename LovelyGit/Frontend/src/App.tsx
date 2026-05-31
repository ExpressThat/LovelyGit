import "./App.css";
import { useEffect, useRef, useState } from "react";
import { CommitDetails } from "./components/CommitDetails/CommitDetails";
import { CommitGraphView } from "./components/CommitGraph/CommitGraphView";
import { SlidingDetailsPanel } from "./components/DetailsPanel/SlidingDetailsPanel";
import { NewTab } from "./components/NewTab/NewTab";
import { TopNavBar } from "./components/TopNavBar/TopNavBar";
import { Toaster } from "./components/ui/sonner";
import { RepositoryProvider } from "./lib/repositoryContext";
import { useSetting } from "./lib/settings/settingsStore";

function App() {
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	const [detailsPanel, setDetailsPanel] = useState<DetailsPanelState | null>(
		null,
	);
	const previousRepositoryIdRef = useRef<string | null>(currentGitRepositoryId);

	useEffect(() => {
		if (previousRepositoryIdRef.current === currentGitRepositoryId) {
			return;
		}

		previousRepositoryIdRef.current = currentGitRepositoryId;
		setDetailsPanel(null);
	}, [currentGitRepositoryId]);

	return (
		<RepositoryProvider>
			<main className="app-shell">
				<TopNavBar />
				<div className="flex min-h-0 flex-1 overflow-hidden">
					<div className="min-w-0 flex-1 overflow-hidden">
						{currentGitRepositoryId && (
							<CommitGraphView
								onSelectCommit={(row) =>
									setDetailsPanel({
										commitHash: row.commit.hash,
										kind: "commit",
									})
								}
								selectedCommitHash={
									detailsPanel?.kind === "commit"
										? detailsPanel.commitHash
										: null
								}
							/>
						)}
						{!currentGitRepositoryId && <NewTab />}
					</div>
					<SlidingDetailsPanel
						isOpen={Boolean(detailsPanel && currentGitRepositoryId)}
						onClose={() => setDetailsPanel(null)}
						title={panelTitle(detailsPanel)}
					>
						{detailsPanel?.kind === "commit" && currentGitRepositoryId ? (
							<CommitDetails
								commitHash={detailsPanel.commitHash}
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

type DetailsPanelState = {
	commitHash: string;
	kind: "commit";
};

function panelTitle(panel: DetailsPanelState | null) {
	if (panel?.kind === "commit") {
		return "Commit Details";
	}

	return "Details";
}

export default App;
