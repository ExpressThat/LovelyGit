import { useEffect, useState } from "react";
import type { RepositoryRefsResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";
import { withBranchUpstream } from "../utils/refMetadata";

type RepositoryRefsState =
	| { status: "idle"; refs: null; message?: string }
	| { status: "loading"; refs: RepositoryRefsResponse | null }
	| { status: "loaded"; refs: RepositoryRefsResponse }
	| { status: "error"; refs: null; message: string };

export function useRepositoryRefs(
	repositoryId: string | null,
	refreshToken: number,
) {
	const [invalidationToken, setInvalidationToken] = useState(0);
	const loadKey = `${refreshToken}:${invalidationToken}`;
	const [state, setState] = useState<RepositoryRefsState>({
		status: "idle",
		refs: null,
	});

	useEffect(() => {
		return subscribeToServerEvent("CommitGraphChanged", () => {
			setInvalidationToken((token) => token + 1);
		});
	}, []);

	useEffect(() => {
		if (!repositoryId) {
			setState({ status: "idle", refs: null });
			return;
		}

		let isActive = true;
		const activeLoadKey = loadKey;
		setState((current) => ({ status: "loading", refs: current.refs }));
		sendRequestWithResponse({
			arguments: { knownRepositoryId: repositoryId },
			commandType: NativeMessageType.GetRepositoryRefs,
		})
			.then((refs) => {
				if (isActive && activeLoadKey === loadKey) {
					setState({ status: "loaded", refs });
				}
			})
			.catch((error) => {
				const message =
					error instanceof Error ? error.message : "Failed to load refs.";
				if (isActive && activeLoadKey === loadKey) {
					setState({ status: "error", refs: null, message });
				}
			});

		return () => {
			isActive = false;
		};
	}, [repositoryId, loadKey]);

	const updateBranchUpstream = (
		branchName: string,
		upstreamName: string | null,
	) => {
		setState((current) =>
			current.refs
				? {
						...current,
						refs: withBranchUpstream(current.refs, branchName, upstreamName),
					}
				: current,
		);
	};
	const refresh = () => setInvalidationToken((token) => token + 1);

	return { ...state, refresh, updateBranchUpstream };
}
