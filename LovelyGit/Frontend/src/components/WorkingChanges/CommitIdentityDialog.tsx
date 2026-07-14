import { useEffect, useState } from "react";
import {
	LoaderCircle,
	RotateCcw,
	Save,
	UserRound,
} from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import type { GitCommitIdentity } from "@/generated/types";
import { motion, useReducedMotion } from "@/lib/motion";

export function CommitIdentityDialog({
	error,
	identity,
	isSaving,
	onClear,
	onOpenChange,
	onSave,
	open,
}: {
	error: string | null;
	identity: GitCommitIdentity | null;
	isSaving: boolean;
	onClear: () => Promise<boolean>;
	onOpenChange: (open: boolean) => void;
	onSave: (name: string, email: string) => Promise<boolean>;
	open: boolean;
}) {
	const [name, setName] = useState("");
	const [email, setEmail] = useState("");
	const reduceMotion = useReducedMotion();
	useEffect(() => {
		if (!open) return;
		setName(identity?.name ?? "");
		setEmail(identity?.email ?? "");
	}, [identity, open]);

	async function save() {
		if (await onSave(name, email)) onOpenChange(false);
	}

	async function clear() {
		if (await onClear()) onOpenChange(false);
	}

	return (
		<Dialog
			onOpenChange={(next) => !isSaving && onOpenChange(next)}
			open={open}
		>
			<DialogContent className="overflow-hidden sm:max-w-md">
				<DialogHeader>
					<DialogTitle className="flex items-center gap-2">
						<UserRound aria-hidden="true" className="size-5 text-primary" />
						Commit identity
					</DialogTitle>
					<DialogDescription>
						Choose the author details Git records for commits in this
						repository.
					</DialogDescription>
				</DialogHeader>
				<motion.div
					animate={{ opacity: 1, y: 0 }}
					className="grid gap-3"
					initial={{ opacity: 0, y: reduceMotion ? 0 : 6 }}
					transition={{ duration: reduceMotion ? 0 : 0.16 }}
				>
					<label
						className="grid gap-1.5 text-xs font-medium"
						htmlFor="commit-author-name"
					>
						Name
						<Input
							autoFocus
							disabled={isSaving}
							id="commit-author-name"
							onInput={(event) => setName(event.currentTarget.value)}
							placeholder="Ada Lovelace"
							value={name}
						/>
					</label>
					<label
						className="grid gap-1.5 text-xs font-medium"
						htmlFor="commit-author-email"
					>
						Email
						<Input
							disabled={isSaving}
							id="commit-author-email"
							onInput={(event) => setEmail(event.currentTarget.value)}
							placeholder="ada@example.com"
							type="email"
							value={email}
						/>
					</label>
					{error ? (
						<p className="rounded-md border border-destructive/40 bg-destructive/10 p-2 text-destructive text-xs">
							{error}
						</p>
					) : null}
					<p className="text-muted-foreground text-xs">
						This creates a repository-local override. Your global Git settings
						stay unchanged.
					</p>
				</motion.div>
				<DialogFooter className="sm:justify-between">
					<div>
						{identity?.hasRepositoryOverride ? (
							<Button
								disabled={isSaving}
								id="clear-commit-identity"
								onClick={() => void clear()}
								type="button"
								variant="ghost"
							>
								<RotateCcw aria-hidden="true" /> Use Git defaults
							</Button>
						) : null}
					</div>
					<Button
						disabled={isSaving || !name.trim() || !email.trim()}
						id="save-commit-identity"
						onClick={() => void save()}
						type="button"
					>
						{isSaving ? (
							<LoaderCircle aria-hidden="true" className="animate-spin" />
						) : (
							<Save aria-hidden="true" />
						)}
						{isSaving ? "Saving" : "Save for this repository"}
					</Button>
				</DialogFooter>
			</DialogContent>
		</Dialog>
	);
}
