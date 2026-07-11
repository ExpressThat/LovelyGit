import { AnimatePresence, motion } from "motion/react";
import { X } from "@/components/icons/lovelyIcons";
import type { KnownGitRepository } from "@/generated/types";
import { cn, getPathTail } from "@/lib/utils";

type RepositoryTabProps = {
	closeRepository: (repositoryId: string) => void;
	currentRepositoryId: string | null;
	isLoadingRepositories: boolean;
	pendingCloseRepositoryId: string | null;
	repository: KnownGitRepository;
	setCurrentRepositoryId: (repositoryId: string) => void;
	setPendingCloseRepositoryId: (repositoryId: string | null) => void;
	setSuppressCloseRepositoryId: (
		setter: (repositoryId: string | null) => string | null,
	) => void;
	suppressCloseRepositoryId: string | null;
};

export function RepositoryTab({
	closeRepository,
	currentRepositoryId,
	isLoadingRepositories,
	pendingCloseRepositoryId,
	repository,
	setCurrentRepositoryId,
	setPendingCloseRepositoryId,
	setSuppressCloseRepositoryId,
	suppressCloseRepositoryId,
}: RepositoryTabProps) {
	const isActive = currentRepositoryId === repository.id;
	const isPendingClose = pendingCloseRepositoryId === repository.id;
	const label = repository.name || getPathTail(repository.path ?? "") || "Repo";
	const path = repository.path ?? "";

	const onMouseLeave = () => {
		if (pendingCloseRepositoryId !== repository.id) {
			return;
		}

		setPendingCloseRepositoryId(null);
		setSuppressCloseRepositoryId((current) =>
			current === repository.id ? null : repository.id,
		);
		window.setTimeout(() => {
			setSuppressCloseRepositoryId((current) =>
				current === repository.id ? null : current,
			);
		}, 140);
	};

	return (
		// biome-ignore lint/a11y/useSemanticElements: The tab contains its own close button, so a native button wrapper would create nested buttons.
		<div
			aria-current={isActive ? "page" : undefined}
			aria-disabled={isLoadingRepositories}
			className={cn(
				"relative -mb-px inline-flex h-8 max-w-48 shrink-0 items-center rounded-t-md border px-2.5 text-xs font-semibold transition-colors focus-visible:z-20 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background",
				isActive
					? "z-10 border-border border-b-background text-foreground"
					: "border-transparent text-muted-foreground hover:border-border/70 hover:bg-accent/60 hover:text-accent-foreground",
				isLoadingRepositories ? "pointer-events-none opacity-60" : null,
			)}
			onClick={() => {
				if (!isLoadingRepositories) {
					setCurrentRepositoryId(repository.id);
				}
			}}
			onKeyDown={(event) => {
				if (isLoadingRepositories) {
					return;
				}
				if (event.key === "Enter" || event.key === " ") {
					event.preventDefault();
					setCurrentRepositoryId(repository.id);
				}
			}}
			onMouseLeave={onMouseLeave}
			role="button"
			tabIndex={isLoadingRepositories ? -1 : 0}
			title={path || label}
		>
			{isActive ? (
				<span className="absolute inset-0 rounded-t-md bg-background" />
			) : null}
			<span className="relative z-10 flex min-w-0 flex-1 items-center">
				<span className="min-w-0 flex-1 truncate text-left">{label}</span>
				<motion.span
					animate={{ width: isPendingClose ? 44 : 16 }}
					className="relative ml-1 inline-flex h-5 shrink-0 items-center justify-center overflow-visible"
					layout
					transition={{ type: "spring", stiffness: 340, damping: 28 }}
				>
					<AnimatePresence initial={false}>
						{isPendingClose ? (
							<motion.button
								animate={{ opacity: 1 }}
								className="absolute inset-0 m-auto h-fit w-fit rounded bg-destructive/15 px-0.5 py-0.5 text-[10px] font-bold text-destructive hover:bg-destructive/20"
								exit={{ opacity: 0 }}
								initial={{ opacity: 0 }}
								key="sure"
								onClick={(event) => {
									event.stopPropagation();
									closeRepository(repository.id);
								}}
								transition={{ duration: 0.16 }}
								type="button"
							>
								Sure?
							</motion.button>
						) : (
							<motion.button
								animate={{ opacity: 1 }}
								aria-label={`Close ${label} tab`}
								className="absolute inset-0 m-auto h-fit w-fit rounded p-0.5 text-muted-foreground hover:bg-accent hover:text-foreground"
								exit={{ opacity: 0 }}
								initial={{ opacity: 0 }}
								key="close"
								onClick={(event) => {
									event.stopPropagation();
									setSuppressCloseRepositoryId(() => null);
									setPendingCloseRepositoryId(repository.id);
								}}
								style={{
									opacity: suppressCloseRepositoryId === repository.id ? 0 : 1,
									pointerEvents:
										suppressCloseRepositoryId === repository.id
											? "none"
											: "auto",
								}}
								transition={{ duration: 0.16 }}
								type="button"
							>
								<X className="size-3 hover:text-destructive" />
							</motion.button>
						)}
					</AnimatePresence>
				</motion.span>
			</span>
		</div>
	);
}
