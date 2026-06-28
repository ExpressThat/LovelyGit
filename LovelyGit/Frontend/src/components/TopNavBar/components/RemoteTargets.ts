import { useEffect, useState } from "react";
import { toast } from "sonner";
import type { GitRemote } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

type RemoteTargetsState =
	| { status: "idle"; remotes: GitRemote[]; selectedRemoteName: null }
	| { status: "loading"; remotes: GitRemote[]; selectedRemoteName: null }
	| {
			status: "loaded";
			remotes: GitRemote[];
			selectedRemoteName: string | null;
	  }
	| { status: "error"; remotes: GitRemote[]; selectedRemoteName: null };

export function useRemoteTargets(repositoryId: string | null) {
	const [state, setState] = useState<RemoteTargetsState>({
		status: "idle",
		remotes: [],
		selectedRemoteName: null,
	});

	useEffect(() => {
		if (!repositoryId) {
			setState({ status: "idle", remotes: [], selectedRemoteName: null });
			return;
		}

		let isActive = true;
		setState({ status: "loading", remotes: [], selectedRemoteName: null });
		sendRequestWithResponse({
			arguments: { repositoryId },
			commandType: NativeMessageType.GetRepositoryRemotes,
		})
			.then((remotes) => {
				if (!isActive) return;
				setState({
					status: "loaded",
					remotes,
					selectedRemoteName: preferredRemoteName(remotes),
				});
			})
			.catch((error) => {
				if (!isActive) return;
				toast.error(
					error instanceof Error ? error.message : "Could not load remotes",
				);
				setState({ status: "error", remotes: [], selectedRemoteName: null });
			});

		return () => {
			isActive = false;
		};
	}, [repositoryId]);

	return {
		...state,
		setSelectedRemoteName: (selectedRemoteName: string) =>
			setState((current) =>
				current.status === "loaded"
					? { ...current, selectedRemoteName }
					: current,
			),
	};
}

export function preferredRemoteName(remotes: GitRemote[]) {
	if (remotes.length === 0) {
		return null;
	}

	return (
		remotes.find((remote) => remote.name === "origin")?.name ?? remotes[0].name
	);
}
