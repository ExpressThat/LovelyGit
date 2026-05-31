import "./App.css";
import { CommitGraphView } from "./components/CommitGraph/CommitGraphView";
import { NewTab } from "./components/NewTab/NewTab";
import { TopNavBar } from "./components/TopNavBar/TopNavBar";
import { Toaster } from "./components/ui/sonner";
import { RepositoryProvider } from "./lib/repositoryContext";
import { useSetting } from "./lib/settings/settingsStore";

function App() {
	const currentGitRepositoryId = useSetting("CurrentGitRepositoryId");
	return (
		<RepositoryProvider>
			<main className="app-shell">
				<TopNavBar />
				{currentGitRepositoryId && <CommitGraphView />}
				{!currentGitRepositoryId && <NewTab />}
			</main>
			<Toaster />
		</RepositoryProvider>
	);
}

export default App;
