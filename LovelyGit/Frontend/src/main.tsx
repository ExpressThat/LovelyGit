import ReactDOM from "react-dom/client";
import App from "./App";
import { getSignalR, registerSignalR } from "./lib/registerSignalR";

async function bootstrap() {
	await registerSignalR();

	const sr = getSignalR();

	sr.on("Result", (result: any) => {
		console.log(result);
	});

	await sr.invoke("Command", {
		commandUniqueId: "123",
		commandType: "KnownGitRepositorys"
	});




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
