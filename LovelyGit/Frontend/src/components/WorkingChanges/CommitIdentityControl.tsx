import { lazy, Suspense, useState } from "react";
import {
	AlertTriangle,
	LoaderCircle,
	Pencil,
	UserRound,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type {
	GitCommitIdentity,
	GitIdentityValueSource,
} from "@/generated/types";
import { AnimatePresence, motion, useReducedMotion } from "@/lib/motion";
import { useCommitIdentity } from "./useCommitIdentity";

const CommitIdentityDialog = lazy(() =>
	import("./CommitIdentityDialog").then((module) => ({
		default: module.CommitIdentityDialog,
	})),
);

export function CommitIdentityControl({
	disabled,
	repositoryId,
}: {
	disabled: boolean;
	repositoryId: string;
}) {
	const [open, setOpen] = useState(false);
	const identity = useCommitIdentity(repositoryId);
	const reduceMotion = useReducedMotion();
	return (
		<>
			<div className="flex min-w-0 items-center gap-2 rounded-md border bg-background px-3 py-2">
				<div className="grid size-8 shrink-0 place-items-center rounded-full bg-primary/10 text-primary">
					{identity.isLoading ? (
						<LoaderCircle
							aria-label="Loading commit identity"
							className="animate-spin"
							size={15}
						/>
					) : identity.identity?.isComplete ? (
						<UserRound aria-hidden="true" size={15} />
					) : (
						<AlertTriangle
							aria-hidden="true"
							className="text-destructive"
							size={15}
						/>
					)}
				</div>
				<AnimatePresence initial={false} mode="wait">
					<motion.div
						animate={{ opacity: 1, y: 0 }}
						className="min-w-0 flex-1"
						exit={{ opacity: 0, y: reduceMotion ? 0 : -2 }}
						initial={{ opacity: 0, y: reduceMotion ? 0 : 2 }}
						key={identityKey(
							identity.identity,
							identity.error,
							identity.isLoading,
						)}
					>
						<p className="truncate font-medium text-xs">
							{identityTitle(
								identity.identity,
								identity.error,
								identity.isLoading,
							)}
						</p>
						<p className="truncate text-muted-foreground text-xs">
							{identitySubtitle(
								identity.identity,
								identity.error,
								identity.isLoading,
							)}
						</p>
					</motion.div>
				</AnimatePresence>
				<Button
					aria-label="Edit commit identity"
					disabled={disabled || identity.isLoading || identity.isSaving}
					onClick={() => setOpen(true)}
					size="icon-sm"
					title="Edit commit identity"
					type="button"
					variant="ghost"
				>
					<Pencil aria-hidden="true" />
				</Button>
			</div>
			{open ? (
				<Suspense fallback={null}>
					<CommitIdentityDialog
						error={identity.error}
						identity={identity.identity}
						isSaving={identity.isSaving}
						onClear={identity.clear}
						onOpenChange={setOpen}
						onSave={identity.save}
						open={open}
					/>
				</Suspense>
			) : null}
		</>
	);
}

function identityTitle(
	identity: GitCommitIdentity | null,
	error: string | null,
	loading: boolean,
) {
	if (loading) return "Reading commit identity";
	if (error) return "Commit identity unavailable";
	if (!identity?.isComplete) return "Commit identity not configured";
	return `${identity.name} <${identity.email}>`;
}

function identitySubtitle(
	identity: GitCommitIdentity | null,
	error: string | null,
	loading: boolean,
) {
	if (loading) return "Resolving Git configuration without starting a process";
	if (error) return error;
	if (!identity?.isComplete)
		return "Add an author name and email before committing";
	return sourceLabel(identity.nameSource, identity.emailSource);
}

function sourceLabel(
	name: GitIdentityValueSource,
	email: GitIdentityValueSource,
) {
	if (name !== email) return "Mixed Git settings";
	if (name === "Repository" || name === "Worktree") return "This repository";
	if (name === "Environment") return "Environment override";
	return "Git defaults";
}

function identityKey(
	identity: GitCommitIdentity | null,
	error: string | null,
	loading: boolean,
) {
	return loading
		? "loading"
		: (error ?? `${identity?.name}:${identity?.email}`);
}
