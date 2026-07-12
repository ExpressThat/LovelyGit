import { useEffect, useState } from "react";
import type { RepositoryRefsResponse } from "@/generated/types";
import { subscribeToServerEvent } from "@/lib/commands";
import {
	loadRepositoryRefs,
	setCachedRepositoryRefs,
} from "@/lib/repositoryRefsCache";
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
		loadRepositoryRefs(repositoryId, true)
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
		setState((current) => {
			if (!current.refs || !repositoryId) return current;
			const refs = withBranchUpstream(current.refs, branchName, upstreamName);
			setCachedRepositoryRefs(repositoryId, refs);
			return { ...current, refs };
		});
	};
	const refresh = () => setInvalidationToken((token) => token + 1);

	return { ...state, refresh, updateBranchUpstream };
}
