import { GitCommitHorizontal, LoaderCircle, PencilLine } from "lucide-react";
import { AnimatePresence, motion, useReducedMotion } from "motion/react";
import { Switch } from "@/components/ui/switch";
import { CommitIdentityControl } from "./CommitIdentityControl";
import { ActionButton } from "./WorkingChangesPanelParts";

const COMMIT_TITLE_LIMIT = 72;

export function CommitStagedForm({
	canCommit,
	commitBody,
	commitTitle,
	isBusy,
	isAmending,
	isCommitting,
	isLoadingAmendMessage,
	onAmendChange,
	onCommit,
	onCommitBodyChange,
	onCommitTitleChange,
	repositoryId,
}: {
	canCommit: boolean;
	commitBody: string;
	commitTitle: string;
	isBusy: boolean;
	isAmending: boolean;
	isCommitting: boolean;
	isLoadingAmendMessage: boolean;
	onAmendChange: (enabled: boolean) => void;
	onCommit: () => void;
	onCommitBodyChange: (value: string) => void;
	onCommitTitleChange: (value: string) => void;
	repositoryId: string;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<section className="space-y-2 rounded-md border bg-card p-3">
			<div className="flex items-center gap-2 text-xs font-semibold text-foreground">
				{isAmending ? (
					<PencilLine aria-hidden="true" size={15} />
				) : (
					<GitCommitHorizontal aria-hidden="true" size={15} />
				)}
				<span>
					{isAmending ? "Amend last commit" : "Commit staged changes"}
				</span>
			</div>
			<div className="relative min-h-36 rounded-md border bg-background px-3 py-2 focus-within:border-sky-500">
				<div
					className={`absolute right-3 top-2 font-mono text-xs ${
						commitTitle.length > COMMIT_TITLE_LIMIT
							? "text-destructive"
							: "text-muted-foreground"
					}`}
				>
					{COMMIT_TITLE_LIMIT - commitTitle.length}
				</div>
				<label className="block pr-12">
					<span className="sr-only">Commit title</span>
					<input
						autoComplete="new-password"
						className="h-8 w-full border-0 bg-transparent p-0 text-lg text-foreground outline-none placeholder:text-muted-foreground"
						disabled={isBusy}
						onChange={(event) => onCommitTitleChange(event.target.value)}
						placeholder="Title"
						type="text"
						value={commitTitle}
					/>
				</label>
				<label className="block">
					<span className="sr-only">Commit body</span>
					<textarea
						autoComplete="new-password"
						className="min-h-24 w-full resize-none border-0 bg-transparent p-0 text-sm text-muted-foreground outline-none placeholder:text-muted-foreground"
						disabled={isBusy}
						onChange={(event) => onCommitBodyChange(event.target.value)}
						placeholder="Body"
						value={commitBody}
					/>
				</label>
			</div>
			<CommitIdentityControl disabled={isBusy} repositoryId={repositoryId} />
			<label
				className="flex cursor-pointer items-center justify-between gap-3 rounded-md border bg-background px-3 py-2"
				htmlFor="amend-last-commit"
			>
				<span className="grid gap-0.5">
					<span className="font-medium text-xs">Amend last commit</span>
					<span className="text-muted-foreground text-xs">
						Rewrite HEAD with this message and any staged changes.
					</span>
				</span>
				{isLoadingAmendMessage ? (
					<LoaderCircle
						aria-label="Loading last commit"
						className="animate-spin"
						size={16}
					/>
				) : (
					<Switch
						checked={isAmending}
						disabled={isBusy}
						id="amend-last-commit"
						onCheckedChange={onAmendChange}
					/>
				)}
			</label>
			<AnimatePresence initial={false}>
				{isAmending ? (
					<motion.p
						animate={{ opacity: 1, y: 0 }}
						className="text-amber-600 text-xs dark:text-amber-400"
						exit={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
						initial={{ opacity: 0, y: reduceMotion ? 0 : -3 }}
					>
						Amending replaces the current commit and changes its hash.
					</motion.p>
				) : null}
			</AnimatePresence>
			<div className="flex justify-end gap-2">
				<ActionButton
					disabled={isBusy || !canCommit || commitTitle.trim().length === 0}
					icon={
						<GitCommitHorizontal
							aria-hidden="true"
							className={isCommitting ? "animate-pulse" : undefined}
							size={14}
						/>
					}
					label={
						isCommitting ? "Committing" : isAmending ? "Amend commit" : "Commit"
					}
					onClick={onCommit}
				/>
			</div>
		</section>
	);
}
