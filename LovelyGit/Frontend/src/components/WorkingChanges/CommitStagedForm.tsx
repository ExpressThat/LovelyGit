import { GitCommitHorizontal } from "lucide-react";
import { ActionButton } from "./WorkingChangesPanelParts";

const COMMIT_TITLE_LIMIT = 72;

export function CommitStagedForm({
	commitBody,
	commitTitle,
	isBusy,
	isCommitting,
	onCommit,
	onCommitBodyChange,
	onCommitTitleChange,
}: {
	commitBody: string;
	commitTitle: string;
	isBusy: boolean;
	isCommitting: boolean;
	onCommit: () => void;
	onCommitBodyChange: (value: string) => void;
	onCommitTitleChange: (value: string) => void;
}) {
	return (
		<section className="space-y-2 rounded-md border bg-card p-3">
			<div className="flex items-center gap-2 text-xs font-semibold text-foreground">
				<GitCommitHorizontal aria-hidden="true" size={15} />
				<span>Commit staged changes</span>
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
						className="min-h-24 w-full resize-none border-0 bg-transparent p-0 text-sm text-muted-foreground outline-none placeholder:text-muted-foreground"
						disabled={isBusy}
						onChange={(event) => onCommitBodyChange(event.target.value)}
						placeholder="Body"
						value={commitBody}
					/>
				</label>
			</div>
			<div className="flex justify-end gap-2">
				<ActionButton
					disabled={isBusy || commitTitle.trim().length === 0}
					icon={
						<GitCommitHorizontal
							aria-hidden="true"
							className={isCommitting ? "animate-pulse" : undefined}
							size={14}
						/>
					}
					label={isCommitting ? "Committing" : "Commit"}
					onClick={onCommit}
				/>
			</div>
		</section>
	);
}
