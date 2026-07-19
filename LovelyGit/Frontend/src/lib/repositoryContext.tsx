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
	reconcileRepository: (repository: KnownGitRepository) => void;
	reloadRepositories: () => Promise<void>;
	repositories: KnownGitRepository[];
	setCurrentRepositoryId: (repositoryId: string | null) => Promise<void>;
};

const RepositoryContext = createContext<RepositoryContextValue | null>(null);

export function RepositoryProvider({ children }: { children: ReactNode }) {
	const storedCurrentRepositoryId = useSetting("CurrentGitRepositoryId");
	const [repositories, setRepositories] = useState<KnownGitRepository[]>([]);
	const [hasLoadedRepositories, setHasLoadedRepositories] = useState(false);
	const [isLoadingRepositories, setIsLoadingRepositories] = useState(true);

	const reloadRepositories = useCallback(async () => {
		setIsLoadingRepositories(true);
		try {
			const knownRepositories =
				(await sendRequestWithResponse({
					commandType: "KnownGitRepositorys",
				})) ?? [];
			setRepositories(knownRepositories);
			setHasLoadedRepositories(true);
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
	const reconcileRepository = useCallback((repository: KnownGitRepository) => {
		setRepositories((current) => upsertRepository(current, repository));
	}, []);

	useEffect(() => {
		void initSettingsStore();
		void reloadRepositories();
	}, [reloadRepositories]);

	const currentRepository = useMemo(
		() =>
			repositories.find(
				(repository) => repository.id === storedCurrentRepositoryId,
			) ?? null,
		[storedCurrentRepositoryId, repositories],
	);
	const currentRepositoryId = resolveCurrentRepositoryId(
		storedCurrentRepositoryId,
		currentRepository,
		hasLoadedRepositories,
	);

	useEffect(() => {
		if (
			hasLoadedRepositories &&
			storedCurrentRepositoryId &&
			!currentRepository
		) {
			void setSetting("CurrentGitRepositoryId", null);
		}
	}, [currentRepository, hasLoadedRepositories, storedCurrentRepositoryId]);

	const value = useMemo<RepositoryContextValue>(
		() => ({
			currentRepository,
			currentRepositoryId,
			closeRepository,
			isLoadingRepositories,
			reconcileRepository,
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
			reconcileRepository,
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

export function upsertRepository(
	repositories: KnownGitRepository[],
	repository: KnownGitRepository,
) {
	const existingIndex = repositories.findIndex(
		(current) => current.id === repository.id,
	);
	if (existingIndex < 0) return [...repositories, repository];
	if (repositories[existingIndex] === repository) return repositories;

	const nextRepositories = [...repositories];
	nextRepositories[existingIndex] = repository;
	return nextRepositories;
}

export function resolveCurrentRepositoryId(
	storedRepositoryId: string | null,
	currentRepository: KnownGitRepository | null,
	hasLoadedRepositories: boolean,
) {
	if (!hasLoadedRepositories) return storedRepositoryId;
	return currentRepository?.id ?? null;
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
