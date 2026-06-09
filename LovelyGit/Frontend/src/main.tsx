import ReactDOM from "react-dom/client";
import App from "./App";
import { TooltipProvider } from "./components/ui/tooltip";
import { registerSignalR } from "./lib/registerSignalR";

async function bootstrap() {
	await registerSignalR();

	ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
		<TooltipProvider>
			<App />
		</TooltipProvider>,
	);
}

bootstrap();
