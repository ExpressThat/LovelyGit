import { Tabs } from "./components/Tabs";
import { ThemeSelector } from "./components/ThemeSelector";

export function TopNavBar() {
	return (
		<header className="shrink-0">
			<Tabs />
			<div className="flex h-10 w-full items-center justify-end border-b bg-card px-2">
				<ThemeSelector />
			</div>
		</header>
	);
}
