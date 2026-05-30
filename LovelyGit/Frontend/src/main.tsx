import ReactDOM from "react-dom/client";
import App from "./App";
import { registerSignalR } from "./lib/registerSignalR";

async function bootstrap() {
	await registerSignalR();

	ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
		<App />,
	);
}

bootstrap();
