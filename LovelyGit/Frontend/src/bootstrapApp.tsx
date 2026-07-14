import ReactDOM from "react-dom/client";
import App from "./App";
import { installLovelyIconSprite } from "./components/icons/LovelyIcon";
import { TooltipProvider } from "./components/ui/tooltip";
import { LazyMotion } from "./lib/motion";
import { applyFontsToDocument } from "./lib/settings/font/fontUtils";
import { getSetting, initSettingsStore } from "./lib/settings/settingsStore";
import {
	applyThemeToDocument,
	calculateAppearanceSide,
	calculateTheme,
} from "./lib/settings/theme/themeUtils";

const loadMotionFeatures = () =>
	import("./lib/motionFeatures").then((module) => module.default);

export async function bootstrapApp(rootElement: HTMLElement) {
	installLovelyIconSprite(rootElement.ownerDocument);
	await initSettingsStore();
	const side = calculateAppearanceSide(getSetting("Theme"));
	applyThemeToDocument(
		calculateTheme(
			getSetting("Theme"),
			getSetting("LightTheme"),
			getSetting("DarkTheme"),
		),
		side === "dark"
			? {
					accent: getSetting("DarkAccent"),
					background: getSetting("DarkBackground"),
					foreground: getSetting("DarkForeground"),
				}
			: {
					accent: getSetting("LightAccent"),
					background: getSetting("LightBackground"),
					foreground: getSetting("LightForeground"),
				},
	);
	applyFontsToDocument(
		side === "dark"
			? getSetting("DarkUiFont") || getSetting("UiFont") || getSetting("Font")
			: getSetting("LightUiFont") || getSetting("UiFont") || getSetting("Font"),
		side === "dark"
			? getSetting("DarkCodeFont") ||
					getSetting("CodeFont") ||
					getSetting("Font")
			: getSetting("LightCodeFont") ||
					getSetting("CodeFont") ||
					getSetting("Font"),
	);

	ReactDOM.createRoot(rootElement).render(
		<LazyMotion features={loadMotionFeatures} strict>
			<TooltipProvider>
				<App />
			</TooltipProvider>
		</LazyMotion>,
	);
}
