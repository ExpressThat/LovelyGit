import ReactDOM from "react-dom/client";
import App from "./App";
import { TooltipProvider } from "./components/ui/tooltip";
import { initSettingsStore } from "./lib/settings/settingsStore";

export async function bootstrapApp(rootElement: HTMLElement) {
	await initSettingsStore();

	ReactDOM.createRoot(rootElement).render(
		<TooltipProvider>
			<App />
		</TooltipProvider>,
	);
}
