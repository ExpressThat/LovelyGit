import { useEffect, useRef, useState } from "react";
import type { RepositoryRefsResponse } from "@/generated/types";
import {
	sendRequestWithResponse,
	subscribeToServerEvent,
} from "@/lib/commands";
import { NativeMessageType } from "@/lib/nativeMessaging";

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
	const previousRepositoryIdRef = useRef<string | null>(repositoryId);
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
		const repositoryChanged = previousRepositoryIdRef.current !== repositoryId;
		previousRepositoryIdRef.current = repositoryId;
		setState((current) => ({
			status: "loading",
			refs: repositoryChanged ? null : current.refs,
		}));
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

	return state;
}
