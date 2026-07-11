import { motion, useReducedMotion } from "motion/react";
import { GitFork, Pencil, Trash2 } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { GitRemote } from "@/generated/types";

export function RemoteRow({
	disabled,
	onEdit,
	onRemove,
	remote,
}: {
	disabled: boolean;
	onEdit: () => void;
	onRemove: () => void;
	remote: GitRemote;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<motion.li
			layout
			animate={{ opacity: 1, scale: 1 }}
			className="flex w-full min-w-0 items-start gap-3 rounded-lg border bg-card p-3"
			exit={{ opacity: 0, scale: reduceMotion ? 1 : 0.98 }}
			initial={{ opacity: 0, scale: reduceMotion ? 1 : 0.98 }}
			transition={{
				duration: reduceMotion ? 0 : 0.16,
				ease: [0.22, 1, 0.36, 1],
			}}
		>
			<div className="mt-0.5 rounded-md bg-primary/10 p-2 text-primary">
				<GitFork aria-hidden="true" className="size-4" />
			</div>
			<div className="min-w-0 flex-1">
				<div className="font-semibold text-sm">{remote.name}</div>
				<div
					className="truncate font-mono text-muted-foreground text-xs"
					title={remote.url}
				>
					Fetch · {remote.url}
				</div>
				{remote.pushUrl ? (
					<div
						className="truncate font-mono text-muted-foreground text-xs"
						title={remote.pushUrl}
					>
						Push · {remote.pushUrl}
					</div>
				) : null}
			</div>
			<div className="flex gap-1">
				<Button
					aria-label={`Edit ${remote.name}`}
					disabled={disabled}
					onClick={onEdit}
					size="icon-sm"
					type="button"
					variant="ghost"
				>
					<Pencil aria-hidden="true" />
				</Button>
				<Button
					aria-label={`Remove ${remote.name}`}
					disabled={disabled}
					onClick={onRemove}
					size="icon-sm"
					type="button"
					variant="ghost"
				>
					<Trash2 aria-hidden="true" />
				</Button>
			</div>
		</motion.li>
	);
}
