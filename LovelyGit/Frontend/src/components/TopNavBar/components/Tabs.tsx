import { useState } from "react";
import { Plus } from "@/components/icons/lovelyIcons";
import { AnimatePresence, LayoutGroup, motion } from "@/lib/motion";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { cn } from "@/lib/utils";
import { RepositoryTab } from "./RepositoryTab";
import { VirtualRepositoryTabs } from "./VirtualRepositoryTabs";

const VIRTUAL_TAB_THRESHOLD = 30;

export function Tabs() {
	const {
		closeRepository,
		currentRepositoryId,
		isLoadingRepositories,
		repositories,
		setCurrentRepositoryId,
	} = useRepositoryContext();
	const [pendingCloseRepositoryId, setPendingCloseRepositoryId] = useState<
		string | null
	>(null);
	const [suppressCloseRepositoryId, setSuppressCloseRepositoryId] = useState<
		string | null
	>(null);
	const showNewTab = currentRepositoryId === null;
	const virtualizeTabs = repositories.length > VIRTUAL_TAB_THRESHOLD;

	const closeRepoTab = async (repositoryId: string) => {
		await closeRepository(repositoryId);
		if (currentRepositoryId === repositoryId) {
			const nextRepository =
				repositories.find((repository) => repository.id !== repositoryId) ??
				null;
			await setCurrentRepositoryId(nextRepository?.id ?? null);
		}
		setPendingCloseRepositoryId(null);
	};

	return (
		<div className="flex h-9 items-end border-b border-border bg-muted/30 px-2 pt-1">
			<LayoutGroup id="top-nav-tabs">
				{virtualizeTabs ? (
					<VirtualRepositoryTabs
						currentRepositoryId={currentRepositoryId}
						renderTab={renderRepositoryTab}
						repositories={repositories}
					/>
				) : (
					<div className="flex min-w-0 flex-1 items-end gap-0.5 overflow-x-auto overflow-y-hidden">
						<AnimatePresence initial={false}>
							{repositories.map(renderRepositoryTab)}
							{showNewTab ? (
								<motion.div
									layout
									key="new-tab-pill"
									initial={{ opacity: 0, y: 8, scale: 0.98 }}
									animate={{ opacity: 1, y: 0, scale: 1 }}
									exit={{ opacity: 0, y: 10, scale: 0.96 }}
									className="-mb-px inline-flex h-8 shrink-0 items-center rounded-t-md border border-border border-b-background bg-background px-2.5 text-xs font-semibold text-foreground"
								>
									New Tab
								</motion.div>
							) : null}
						</AnimatePresence>
					</div>
				)}
				{virtualizeTabs && showNewTab ? <NewTabPill /> : null}
				<motion.button
					layout="position"
					key="add-tab-button"
					type="button"
					disabled={isLoadingRepositories}
					onClick={() => void setCurrentRepositoryId(null)}
					whileHover={{ y: -1 }}
					whileTap={{ scale: 0.94 }}
					transition={{
						layout: { type: "spring", stiffness: 340, damping: 28 },
					}}
					className={cn(
						"mb-1 ml-1 inline-flex size-6 shrink-0 items-center justify-center rounded-md text-muted-foreground transition-colors",
						"hover:bg-accent hover:text-accent-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
					)}
					aria-label="Open new tab"
				>
					<Plus className="size-4" />
				</motion.button>
			</LayoutGroup>
		</div>
	);

	function renderRepositoryTab(repository: (typeof repositories)[number]) {
		return (
			<RepositoryTab
				closeRepository={(repositoryId) => void closeRepoTab(repositoryId)}
				currentRepositoryId={currentRepositoryId}
				isLoadingRepositories={isLoadingRepositories}
				key={repository.id}
				pendingCloseRepositoryId={pendingCloseRepositoryId}
				repository={repository}
				setCurrentRepositoryId={(repositoryId) =>
					void setCurrentRepositoryId(repositoryId)
				}
				setPendingCloseRepositoryId={setPendingCloseRepositoryId}
				setSuppressCloseRepositoryId={setSuppressCloseRepositoryId}
				suppressCloseRepositoryId={suppressCloseRepositoryId}
			/>
		);
	}
}

function NewTabPill() {
	return (
		<motion.div
			animate={{ opacity: 1, y: 0, scale: 1 }}
			className="-mb-px inline-flex h-8 shrink-0 items-center rounded-t-md border border-border border-b-background bg-background px-2.5 text-xs font-semibold text-foreground"
			initial={{ opacity: 0, y: 8, scale: 0.98 }}
			layout
		>
			New Tab
		</motion.div>
	);
}
