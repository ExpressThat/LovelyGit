import { LoaderCircle, Save, X } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { RemoteDraft } from "./useRemoteManager";

export function RemoteEditor({
	draft,
	existingNames,
	isSaving,
	onCancel,
	onSave,
}: {
	draft: RemoteDraft;
	existingNames: string[];
	isSaving: boolean;
	onCancel: () => void;
	onSave: (draft: RemoteDraft) => void;
}) {
	const [value, setValue] = useState(draft);
	const reduceMotion = useReducedMotion();
	const normalizedName = value.name.trim();
	const duplicate = existingNames.some(
		(name) => name === normalizedName && name !== draft.originalName,
	);
	const canSave =
		normalizedName.length > 0 && value.url.trim().length > 0 && !duplicate;
	return (
		<motion.form
			animate={{ opacity: 1, y: 0 }}
			className="grid gap-3 rounded-lg border bg-card p-4"
			initial={{ opacity: 0, y: reduceMotion ? 0 : -6 }}
			onSubmit={(event) => {
				event.preventDefault();
				if (canSave) onSave(value);
			}}
		>
			<div>
				<h3 className="font-semibold text-sm">
					{draft.originalName ? `Edit ${draft.originalName}` : "Add remote"}
				</h3>
				<p className="text-muted-foreground text-xs">
					Use HTTPS, SSH, or a local repository path.
				</p>
			</div>
			<label className="grid gap-1.5 text-xs" htmlFor="remote-name">
				<span className="font-medium">Name</span>
				<Input
					autoFocus
					disabled={isSaving}
					id="remote-name"
					onChange={(event) => setValue({ ...value, name: event.target.value })}
					placeholder="origin"
					value={value.name}
				/>
			</label>
			{duplicate ? (
				<p className="text-destructive text-xs">
					A remote with this name already exists.
				</p>
			) : null}
			<label className="grid gap-1.5 text-xs" htmlFor="remote-fetch-url">
				<span className="font-medium">Fetch URL</span>
				<Input
					disabled={isSaving}
					id="remote-fetch-url"
					onChange={(event) => setValue({ ...value, url: event.target.value })}
					placeholder="git@github.com:owner/repository.git"
					value={value.url}
				/>
			</label>
			<label className="grid gap-1.5 text-xs" htmlFor="remote-push-url">
				<span className="font-medium">Push URL (optional)</span>
				<Input
					disabled={isSaving}
					id="remote-push-url"
					onChange={(event) =>
						setValue({ ...value, pushUrl: event.target.value })
					}
					placeholder="Uses the fetch URL"
					value={value.pushUrl}
				/>
			</label>
			<div className="flex justify-end gap-2">
				<Button
					disabled={isSaving}
					onClick={onCancel}
					type="button"
					variant="ghost"
				>
					<X aria-hidden="true" /> Cancel
				</Button>
				<Button disabled={isSaving || !canSave} type="submit">
					{isSaving ? <LoaderCircle className="animate-spin" /> : <Save />}
					{isSaving ? "Saving" : "Save remote"}
				</Button>
			</div>
		</motion.form>
	);
}
