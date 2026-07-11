import { motion, useReducedMotion } from "motion/react";
import { useEffect, useState } from "react";
import { Link2, LoaderCircle, Unlink } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogFooter,
	DialogHeader,
	DialogTitle,
} from "@/components/ui/dialog";
import {
	Select,
	SelectContent,
	SelectItem,
	SelectTrigger,
	SelectValue,
} from "@/components/ui/select";

export function BranchUpstreamDialog({
	branchName,
	currentUpstream,
	isBusy,
	onConfirm,
	onOpenChange,
	remoteBranches,
}: {
	branchName: string;
	currentUpstream: string | null;
	isBusy: boolean;
	onConfirm: (upstreamName: string | null) => void;
	onOpenChange: (branchName: string | null) => void;
	remoteBranches: string[];
}) {
	const options =
		currentUpstream && !remoteBranches.includes(currentUpstream)
			? [currentUpstream, ...remoteBranches]
			: remoteBranches;
	const [selected, setSelected] = useState(currentUpstream ?? options[0] ?? "");
	const reduceMotion = useReducedMotion();
	useEffect(() => {
		setSelected(currentUpstream ?? options[0] ?? "");
	}, [currentUpstream, options[0]]);
	return (
		<Dialog
			open
			onOpenChange={(open) => !open && !isBusy && onOpenChange(null)}
		>
			<DialogContent>
				<form
					onSubmit={(event) => {
						event.preventDefault();
						if (selected && selected !== currentUpstream) onConfirm(selected);
					}}
				>
					<DialogHeader>
						<DialogTitle className="flex items-center gap-2">
							<Link2 aria-hidden="true" className="size-5 text-primary" />
							Upstream for {branchName}
						</DialogTitle>
						<DialogDescription>
							Choose the remote branch used by pull, push, and ahead/behind
							comparisons.
						</DialogDescription>
					</DialogHeader>
					<div className="grid gap-4 py-4">
						{currentUpstream ? (
							<motion.div
								animate={{ opacity: 1, y: 0 }}
								className="flex items-center justify-between rounded-lg border bg-card px-3 py-2 text-sm"
								initial={{ opacity: 0, y: reduceMotion ? 0 : -4 }}
							>
								<span className="text-muted-foreground">
									Currently tracking
								</span>
								<span className="font-mono font-medium">{currentUpstream}</span>
							</motion.div>
						) : null}
						{options.length > 0 ? (
							<label className="grid gap-2 text-sm" htmlFor="branch-upstream">
								<span className="font-medium">Remote branch</span>
								<Select
									disabled={isBusy}
									onValueChange={(value) => setSelected(value ?? "")}
									value={selected}
								>
									<SelectTrigger
										aria-label="Remote branch"
										className="w-full"
										id="branch-upstream"
									>
										<SelectValue>
											{selected || "Choose a remote branch"}
										</SelectValue>
									</SelectTrigger>
									<SelectContent align="start" className="max-h-64">
										{options.map((branch) => (
											<SelectItem key={branch} value={branch}>
												{branch}
											</SelectItem>
										))}
									</SelectContent>
								</Select>
							</label>
						) : (
							<div className="rounded-lg border border-dashed bg-muted/30 p-4 text-center text-muted-foreground text-sm">
								No remote branches are available. Fetch a remote first.
							</div>
						)}
					</div>
					<DialogFooter className="mx-0 mb-0 px-0 pb-0 sm:justify-between">
						{currentUpstream ? (
							<Button
								disabled={isBusy}
								onClick={() => onConfirm(null)}
								type="button"
								variant="outline"
							>
								<Unlink aria-hidden="true" /> Unset upstream
							</Button>
						) : (
							<span />
						)}
						<Button
							disabled={isBusy || !selected || selected === currentUpstream}
							type="submit"
						>
							{isBusy ? <LoaderCircle className="animate-spin" /> : <Link2 />}
							{isBusy ? "Saving" : "Set upstream"}
						</Button>
					</DialogFooter>
				</form>
			</DialogContent>
		</Dialog>
	);
}
