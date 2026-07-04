import { GitCompareArrows } from "lucide-react";
import { RemoteActionsControl } from "./components/RemoteActionsControl";
import { SettingsDialog } from "./components/SettingsDialog";
import { Tabs } from "./components/Tabs";
import { TerminalActionControl } from "./components/TerminalActionControl";
import { ThemeSelector } from "./components/ThemeSelector";

export function TopNavBar({
	onOpenWorkingChanges,
	repositoryId,
	workingChangesCount,
}: {
	onOpenWorkingChanges: () => void;
	repositoryId: string | null;
	workingChangesCount: number;
}) {
	return (
		<header className="shrink-0">
			<Tabs />
			<div className="flex h-10 w-full items-center justify-between gap-2 border-b bg-card px-2">
				<div className="flex items-center gap-1">
					<RemoteActionsControl repositoryId={repositoryId} />
					<TerminalActionControl repositoryId={repositoryId} />
				</div>
				<div className="flex items-center justify-end gap-1">
					<button
						aria-label="Open working changes"
						className="relative inline-flex size-7 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
						onClick={onOpenWorkingChanges}
						title="Working changes"
						type="button"
					>
						<GitCompareArrows aria-hidden="true" size={15} />
						{workingChangesCount > 0 ? (
							<span className="-right-1 -top-1 absolute min-w-4 rounded-full bg-primary px-1 text-[9px] font-bold leading-4 text-primary-foreground">
								{workingChangesCount > 99 ? "99+" : workingChangesCount}
							</span>
						) : null}
					</button>
					<ThemeSelector />
					<SettingsDialog />
				</div>
			</div>
		</header>
	);
}
