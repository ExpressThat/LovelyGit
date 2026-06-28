import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import type { useRemoteTargets } from "./RemoteTargets";

export function RemoteTargetSelect({
	disabled,
	remoteTargets,
}: {
	disabled: boolean;
	remoteTargets: ReturnType<typeof useRemoteTargets>;
}) {
	if (remoteTargets.status === "loading") {
		return (
			<p className="px-3 py-2 text-muted-foreground text-xs">Loading remotes</p>
		);
	}

	if (remoteTargets.remotes.length === 0) {
		return (
			<p className="px-3 py-2 text-destructive text-xs">
				No remotes configured
			</p>
		);
	}

	if (remoteTargets.remotes.length === 1) {
		return (
			<p className="px-3 py-2 text-muted-foreground text-xs">
				Remote: {remoteTargets.remotes[0].name}
			</p>
		);
	}

	return (
		<div className="px-3 py-2">
			<Select
				disabled={disabled}
				onValueChange={(value) => {
					if (value) {
						remoteTargets.setSelectedRemoteName(value);
					}
				}}
				value={remoteTargets.selectedRemoteName ?? undefined}
			>
				<SelectTrigger aria-label="Remote target" className="h-8 w-full">
					<SelectValue placeholder="Select remote" />
				</SelectTrigger>
				<SelectContent>
					{remoteTargets.remotes.map((remote) => (
						<SelectItem key={remote.name} value={remote.name}>
							{remote.name}
						</SelectItem>
					))}
				</SelectContent>
			</Select>
		</div>
	);
}
