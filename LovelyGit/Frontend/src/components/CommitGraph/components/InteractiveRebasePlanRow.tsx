import { ArrowDown, ArrowUp } from "lucide-react";
import { motion, useReducedMotion } from "motion/react";
import { Button } from "@/components/ui/button";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";
import {
	InteractiveRebaseAction,
	type InteractiveRebaseCommit,
	type InteractiveRebasePlanItem,
} from "@/generated/types";
import { shortHash } from "../utils/format";

const actions = Object.values(InteractiveRebaseAction);

export function InteractiveRebasePlanRow({
	commit,
	index,
	item,
	onAction,
	onMessage,
	onMove,
	total,
}: {
	commit: InteractiveRebaseCommit;
	index: number;
	item: InteractiveRebasePlanItem;
	onAction: (action: InteractiveRebasePlanItem["action"]) => void;
	onMessage: (message: string) => void;
	onMove: (offset: number) => void;
	total: number;
}) {
	const reduceMotion = useReducedMotion();
	return (
		<motion.li
			layout="position"
			transition={
				reduceMotion
					? { duration: 0 }
					: { type: "spring", stiffness: 520, damping: 38 }
			}
			className="grid grid-cols-[auto_7rem_minmax(0,1fr)_auto] items-center gap-2 rounded-lg border bg-card p-2"
		>
			<span className="grid size-5 place-items-center rounded-full bg-muted font-mono text-[10px] text-muted-foreground">
				{index + 1}
			</span>
			<Select
				value={item.action}
				onValueChange={(value) => value && onAction(value)}
			>
				<SelectTrigger
					aria-label={`Action for ${commit.subject}`}
					className="w-full"
					size="sm"
				>
					<SelectValue />
				</SelectTrigger>
				<SelectContent alignItemWithTrigger={false}>
					{actions.map((action) => (
						<SelectItem key={action} value={action}>
							{action}
						</SelectItem>
					))}
				</SelectContent>
			</Select>
			<div className="min-w-0">
				<div
					className={`truncate font-medium text-xs ${item.action === "Drop" ? "text-muted-foreground line-through" : ""}`}
				>
					{commit.subject}
				</div>
				<div className="font-mono text-[10px] text-muted-foreground">
					{shortHash(commit.hash)} · {commit.authorName}
				</div>
				{item.action === "Reword" ? (
					<textarea
						aria-label={`New message for ${commit.subject}`}
						className="mt-2 min-h-14 w-full resize-y rounded-md border border-input bg-background px-2 py-1.5 text-xs outline-none focus-visible:ring-2 focus-visible:ring-ring"
						onChange={(event) => onMessage(event.target.value)}
						value={item.message ?? ""}
					/>
				) : null}
			</div>
			<div className="flex gap-1">
				<Button
					aria-label={`Move ${commit.subject} up`}
					disabled={index === 0}
					onClick={() => onMove(-1)}
					size="icon-xs"
					type="button"
					variant="ghost"
				>
					<ArrowUp />
				</Button>
				<Button
					aria-label={`Move ${commit.subject} down`}
					disabled={index === total - 1}
					onClick={() => onMove(1)}
					size="icon-xs"
					type="button"
					variant="ghost"
				>
					<ArrowDown />
				</Button>
			</div>
		</motion.li>
	);
}
