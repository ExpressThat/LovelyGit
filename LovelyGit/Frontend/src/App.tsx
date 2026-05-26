import "./App.css";
import { CommitGraphView } from "./components/CommitGraph/CommitGraphView";
import { Toaster } from "./components/ui/sonner";

function App() {
	return (
		<>
			<main className="app-shell">
				<CommitGraphView />
			</main>
			<Toaster />
		</>
	);
}

export default App;
