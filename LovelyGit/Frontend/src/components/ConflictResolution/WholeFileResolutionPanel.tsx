import { motion } from "motion/react";
import { FileMinus2, GitBranch } from "@/components/icons/lovelyIcons";
import { Button } from "@/components/ui/button";
import type { ConflictResolutionSource } from "@/generated/types";

export function WholeFileResolutionPanel({
	exists,
	selection,
	setSelection,
}: {
	exists: { base: boolean; ours: boolean; theirs: boolean };
	selection: ConflictResolutionSource | "delete" | null;
	setSelection: (selection: ConflictResolutionSource | "delete") => void;
}) {
	return (
		<motion.section
			animate={{ opacity: 1, y: 0 }}
			className="flex flex-[0.7] flex-col items-center justify-center gap-4 border-t bg-popover p-6"
			initial={{ opacity: 0, y: 16 }}
		>
			<div className="max-w-xl text-center">
				<h2 className="text-sm font-semibold">
					Choose the complete file result
				</h2>
				<p className="mt-1 text-xs text-muted-foreground">
					A side deleted this file, or its content cannot be edited safely
					line-by-line. Choose the complete result.
				</p>
			</div>
			<div className="flex flex-wrap justify-center gap-2">
				<Choice
					active={selection === "Ours"}
					label={exists.ours ? "Use current branch" : "Keep current deletion"}
					onClick={() => setSelection("Ours")}
				/>
				<Choice
					active={selection === "Theirs"}
					label={
						exists.theirs ? "Use incoming branch" : "Keep incoming deletion"
					}
					onClick={() => setSelection("Theirs")}
				/>
				{exists.base ? (
					<Choice
						active={selection === "Base"}
						label="Use common base"
						onClick={() => setSelection("Base")}
					/>
				) : null}
				<Button
					onClick={() => setSelection("delete")}
					variant={selection === "delete" ? "destructive" : "outline"}
				>
					<FileMinus2 /> Delete file
				</Button>
			</div>
		</motion.section>
	);
}

function Choice({
	active,
	label,
	onClick,
}: {
	active: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button onClick={onClick} variant={active ? "default" : "outline"}>
			<GitBranch /> {label}
		</Button>
	);
}
