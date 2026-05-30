import "./App.css";
import { CommitGraphView } from "./components/CommitGraph/CommitGraphView";
import { TopNavBar } from "./components/TopNavBar/TopNavBar";
import { Toaster } from "./components/ui/sonner";
import { RepositoryProvider } from "./lib/repositoryContext";

function App() {
	return (
		<RepositoryProvider>
			<main className="app-shell">
				<TopNavBar />
				<CommitGraphView />
			</main>
			<Toaster />
		</RepositoryProvider>
	);
}

export default App;
