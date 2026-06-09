import { Download, GitCompareArrows, Upload } from "lucide-react";
import { useState } from "react";
import type { ReactNode } from "react";
import { toast } from "sonner";
import { sendRequestWithResponse } from "@/lib/registerSignalR";
import { Button } from "../ui/button";
import { SettingsDialog } from "./components/SettingsDialog";
import { Tabs } from "./components/Tabs";
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
	const [busyCommand, setBusyCommand] = useState<"fetch" | "push" | null>(null);

	const runRemoteCommand = async (kind: "fetch" | "push") => {
		if (!repositoryId || busyCommand) {
			return;
		}

		setBusyCommand(kind);
		const label = kind === "fetch" ? "Fetch" : "Push";
		const toastId = toast.loading(`${label} in progress`);
		try {
			await sendRequestWithResponse({
				commandType: kind === "fetch" ? "FetchRepository" : "PushRepository",
				arguments: {
					repositoryId,
				},
			});
			toast.success(`${label} complete`, { id: toastId });
		} catch (error) {
			toast.error(error instanceof Error ? error.message : `${label} failed`, {
				id: toastId,
			});
		} finally {
			setBusyCommand(null);
		}
	};

	return (
		<header className="shrink-0">
			<Tabs />
			<div className="flex h-10 w-full items-center justify-between gap-2 border-b bg-card px-2">
				<div className="flex items-center gap-1">
					<ToolbarButton
						disabled={!repositoryId || busyCommand !== null}
						icon={
							<Download
								aria-hidden="true"
								className={busyCommand === "fetch" ? "animate-pulse" : undefined}
								size={15}
							/>
						}
						label="Fetch"
						onClick={() => void runRemoteCommand("fetch")}
					/>
					<ToolbarButton
						disabled={!repositoryId || busyCommand !== null}
						icon={
							<Upload
								aria-hidden="true"
								className={busyCommand === "push" ? "animate-pulse" : undefined}
								size={15}
							/>
						}
						label="Push"
						onClick={() => void runRemoteCommand("push")}
					/>
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

function ToolbarButton({
	disabled,
	icon,
	label,
	onClick,
}: {
	disabled: boolean;
	icon: ReactNode;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button
			disabled={disabled}
			onClick={onClick}
			size="sm"
			title={label}
			type="button"
			variant="outline"
		>
			{icon}
			<span>{label}</span>
		</Button>
	);
}
