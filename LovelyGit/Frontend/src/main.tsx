import ReactDOM from "react-dom/client";
import App from "./App";

async function bootstrap() {
	// await initSettingsStore();
	//await getCurrentWebview().setZoom(getSetting("ZOOM_LEVEL"));
	//await emit("frontend-zoom-changed", getSetting("ZOOM_LEVEL"));

	// document.documentElement.classList.toggle(
	// 	"dark",
	// 	calculateTheme(getSetting("THEME")) === "dark",
	// );
	ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
		<App />,
	);
}

bootstrap();
