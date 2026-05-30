import { Plus, X } from "lucide-react";
import { AnimatePresence, LayoutGroup, motion } from "motion/react";
import { useState } from "react";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { cn, getPathTail } from "@/lib/utils";

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
				<div className="flex min-w-0 flex-1 items-end gap-0.5 overflow-x-auto overflow-y-hidden">
					<AnimatePresence initial={false}>
						{repositories.map((repository) => {
							const isActive = currentRepositoryId === repository.id;
							const isPendingClose = pendingCloseRepositoryId === repository.id;
							const label =
								repository.name || getPathTail(repository.path ?? "") || "Repo";

							return (
								<motion.button
									layout="position"
									type="button"
									key={repository.id}
									initial={{ opacity: 0, y: -4, scale: 0.98 }}
									animate={{ opacity: 1, y: 0, scale: 1 }}
									exit={{ opacity: 0, y: 10, scale: 0.96 }}
									disabled={isLoadingRepositories}
									onClick={() => void setCurrentRepositoryId(repository.id)}
									onMouseLeave={() => {
										if (pendingCloseRepositoryId === repository.id) {
											setPendingCloseRepositoryId(null);
											setSuppressCloseRepositoryId(repository.id);
											window.setTimeout(() => {
												setSuppressCloseRepositoryId((current) =>
													current === repository.id ? null : current,
												);
											}, 140);
										}
									}}
									whileTap={{ scale: 0.98 }}
									transition={{
										layout: { type: "spring", stiffness: 340, damping: 28 },
									}}
									className={cn(
										"relative -mb-px inline-flex h-8 max-w-48 shrink-0 items-center rounded-t-md border px-2.5 text-xs font-semibold transition-colors focus-visible:z-20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
										isActive
											? "z-10 border-border border-b-background text-foreground"
											: "border-transparent text-muted-foreground hover:border-border/70 hover:bg-accent/60 hover:text-accent-foreground",
									)}
									aria-current={isActive ? "page" : undefined}
									title={repository.path ?? label}
								>
									{isActive ? (
										<motion.span
											layoutId="top-nav-active-tab"
											className="absolute inset-0 rounded-t-md bg-background"
											transition={{
												type: "spring",
												stiffness: 420,
												damping: 34,
												mass: 0.65,
											}}
										/>
									) : null}
									<span className="relative z-10 flex min-w-0 flex-1 items-center">
										<span className="min-w-0 flex-1 truncate text-left">
											{label}
										</span>
										<motion.span
											layout
											className="relative ml-1 inline-flex h-5 shrink-0 items-center justify-center overflow-visible"
											animate={{ width: isPendingClose ? 44 : 16 }}
											transition={{
												type: "spring",
												stiffness: 340,
												damping: 28,
											}}
										>
											<AnimatePresence initial={false}>
												{isPendingClose ? (
													<motion.button
														key="sure"
														type="button"
														initial={{ opacity: 0 }}
														animate={{ opacity: 1 }}
														exit={{ opacity: 0 }}
														transition={{ duration: 0.16 }}
														onClick={(event) => {
															event.stopPropagation();
															void closeRepoTab(repository.id);
														}}
														className="absolute inset-0 m-auto h-fit w-fit rounded bg-destructive/15 px-0.5 py-0.5 text-[10px] font-bold text-destructive hover:bg-destructive/20"
													>
														Sure?
													</motion.button>
												) : (
													<motion.button
														key="close"
														type="button"
														initial={{ opacity: 0 }}
														animate={{ opacity: 1 }}
														exit={{ opacity: 0 }}
														transition={{ duration: 0.16 }}
														onClick={(event) => {
															event.stopPropagation();
															setSuppressCloseRepositoryId(null);
															setPendingCloseRepositoryId(repository.id);
														}}
														className="absolute inset-0 m-auto h-fit w-fit rounded p-0.5 text-muted-foreground hover:bg-accent hover:text-foreground"
														style={{
															opacity:
																suppressCloseRepositoryId === repository.id
																	? 0
																	: 1,
															pointerEvents:
																suppressCloseRepositoryId === repository.id
																	? "none"
																	: "auto",
														}}
														aria-label={`Close ${label} tab`}
													>
														<X className="size-3 hover:text-destructive" />
													</motion.button>
												)}
											</AnimatePresence>
										</motion.span>
									</span>
								</motion.button>
							);
						})}
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
					</AnimatePresence>
				</div>
			</LayoutGroup>
		</div>
	);
}
