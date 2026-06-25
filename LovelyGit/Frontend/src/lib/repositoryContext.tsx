import {
	createContext,
	type ReactNode,
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useState,
} from "react";
import type { KnownGitRepository } from "@/generated/types";
import { sendRequestWithResponse } from "@/lib/commands";
import {
	initSettingsStore,
	setSetting,
	useSetting,
} from "@/lib/settings/settingsStore";

type RepositoryContextValue = {
	closeRepository: (repositoryId: string) => Promise<void>;
	currentRepository: KnownGitRepository | null;
	currentRepositoryId: string | null;
	isLoadingRepositories: boolean;
	reloadRepositories: () => Promise<void>;
	repositories: KnownGitRepository[];
	setCurrentRepositoryId: (repositoryId: string | null) => Promise<void>;
};

const RepositoryContext = createContext<RepositoryContextValue | null>(null);

export function RepositoryProvider({ children }: { children: ReactNode }) {
	const currentRepositoryId = useSetting("CurrentGitRepositoryId");
	const [repositories, setRepositories] = useState<KnownGitRepository[]>([]);
	const [isLoadingRepositories, setIsLoadingRepositories] = useState(true);

	const reloadRepositories = useCallback(async () => {
		setIsLoadingRepositories(true);
		try {
			const knownRepositories =
				(await sendRequestWithResponse({
					commandType: "KnownGitRepositorys",
				})) ?? [];
			setRepositories(knownRepositories);
		} finally {
			setIsLoadingRepositories(false);
		}
	}, []);

	const closeRepository = useCallback(
		async (repositoryId: string) => {
			await sendRequestWithResponse({
				commandType: "RemoveKnownGitRepositorys",
				arguments: {
					knownRepositoryId: repositoryId,
				},
			});

			setRepositories((current) =>
				current.filter((repository) => repository.id !== repositoryId),
			);
			await reloadRepositories();
		},
		[reloadRepositories],
	);

	useEffect(() => {
		void initSettingsStore();
		void reloadRepositories();
	}, [reloadRepositories]);

	const currentRepository = useMemo(
		() =>
			repositories.find(
				(repository) => repository.id === currentRepositoryId,
			) ?? null,
		[currentRepositoryId, repositories],
	);

	const value = useMemo<RepositoryContextValue>(
		() => ({
			currentRepository,
			currentRepositoryId,
			closeRepository,
			isLoadingRepositories,
			reloadRepositories,
			repositories,
			setCurrentRepositoryId: (repositoryId) =>
				setSetting("CurrentGitRepositoryId", repositoryId),
		}),
		[
			closeRepository,
			currentRepository,
			currentRepositoryId,
			isLoadingRepositories,
			reloadRepositories,
			repositories,
		],
	);

	return (
		<RepositoryContext.Provider value={value}>
			{children}
		</RepositoryContext.Provider>
	);
}

export function useRepositoryContext() {
	const context = useContext(RepositoryContext);
	if (!context) {
		throw new Error(
			"useRepositoryContext must be used within a RepositoryProvider",
		);
	}

	return context;
}
