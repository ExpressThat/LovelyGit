import { Command, GitCompareArrows, Search } from "lucide-react";
import { SettingsDialog } from "../Settings/SettingsDialog";
import { BisectControl } from "./components/BisectControl";
import { BranchControl } from "./components/BranchControl";
import { PatchApplyControl } from "./components/PatchApplyControl";
import { RemoteActionsControl } from "./components/RemoteActionsControl";
import { RemoteWebActionControl } from "./components/RemoteWebActionControl";
import { SubmoduleManager } from "./components/SubmoduleManager";
import { Tabs } from "./components/Tabs";
import { TerminalActionControl } from "./components/TerminalActionControl";

export function TopNavBar({
	currentBranchName,
	onBranchChanged,
	onOpenCommandPalette,
	onOpenWorkingChanges,
	onSearchCommits,
	onSettingsOpenChange,
	repositoryId,
	settingsOpen,
	workingChangesCount,
}: {
	currentBranchName: string | null;
	onBranchChanged: (branchName: string) => void;
	onOpenCommandPalette: () => void;
	onOpenWorkingChanges: () => void;
	onSearchCommits: () => void;
	onSettingsOpenChange: (open: boolean) => void;
	repositoryId: string | null;
	settingsOpen: boolean;
	workingChangesCount: number;
}) {
	return (
		<header className="shrink-0">
			<Tabs />
			<div className="grid h-12 w-full grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-2 border-b bg-card px-2">
				<div className="min-w-0">
					<BranchControl
						currentBranchName={currentBranchName}
						key={repositoryId}
						onBranchChanged={onBranchChanged}
						onOpenWorkingChanges={onOpenWorkingChanges}
						onRepositoryChanged={() => {
							if (currentBranchName) {
								onBranchChanged(currentBranchName);
							}
						}}
						repositoryId={repositoryId}
					/>
				</div>
				<div className="flex items-center justify-center gap-2">
					<RemoteActionsControl
						currentBranchName={currentBranchName}
						repositoryId={repositoryId}
					/>
					<PatchApplyControl
						onApplied={onOpenWorkingChanges}
						repositoryId={repositoryId}
					/>
					<TerminalActionControl repositoryId={repositoryId} />
					<RemoteWebActionControl repositoryId={repositoryId} />
				</div>
				<div className="flex items-center justify-end gap-1">
					<button
						aria-label="Open command palette"
						className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
						onClick={onOpenCommandPalette}
						title="Command palette (Ctrl+K)"
						type="button"
					>
						<Command aria-hidden="true" className="size-5" />
					</button>
					<BisectControl repositoryId={repositoryId} />
					<SubmoduleManager repositoryId={repositoryId} />
					<button
						aria-label="Search commits"
						className="inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground disabled:pointer-events-none disabled:opacity-40"
						disabled={!repositoryId}
						onClick={onSearchCommits}
						title="Search commits (Ctrl+F)"
						type="button"
					>
						<Search aria-hidden="true" className="size-5" />
					</button>
					<button
						aria-label="Open working changes"
						className="relative inline-flex size-9 items-center justify-center rounded-md text-muted-foreground hover:bg-accent hover:text-accent-foreground"
						onClick={onOpenWorkingChanges}
						title="Working changes"
						type="button"
					>
						<GitCompareArrows aria-hidden="true" className="size-6" />
						{workingChangesCount > 0 ? (
							<span className="-right-1 -top-1 absolute min-w-4 rounded-full bg-primary px-1 text-[9px] font-bold leading-4 text-primary-foreground">
								{workingChangesCount > 99 ? "99+" : workingChangesCount}
							</span>
						) : null}
					</button>
					<SettingsDialog
						onOpenChange={onSettingsOpenChange}
						open={settingsOpen}
					/>
				</div>
			</div>
		</header>
	);
}
