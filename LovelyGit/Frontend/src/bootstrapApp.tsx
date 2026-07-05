import ReactDOM from "react-dom/client";
import App from "./App";
import { TooltipProvider } from "./components/ui/tooltip";
import { applyFontToDocument } from "./lib/settings/font/fontUtils";
import { getSetting, initSettingsStore } from "./lib/settings/settingsStore";
import {
	applyThemeToDocument,
	calculateTheme,
} from "./lib/settings/theme/themeUtils";

export async function bootstrapApp(rootElement: HTMLElement) {
	await initSettingsStore();
	applyThemeToDocument(calculateTheme(getSetting("Theme")));
	applyFontToDocument(getSetting("Font"));

	ReactDOM.createRoot(rootElement).render(
		<TooltipProvider>
			<App />
		</TooltipProvider>,
	);
}
