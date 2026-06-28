import { ChevronDown, ChevronUp, PencilLine } from "lucide-react";
import { Button } from "@/components/ui/button";
import type { GitConflictTextLine } from "@/generated/types";
import { ConflictRenderedLine } from "./ConflictRenderedLine";

export function ConflictResultEditor({
	activeIndex,
	hunkCount,
	lines,
	onChange,
	onNext,
	onPrevious,
	value,
}: {
	activeIndex: number;
	hunkCount: number;
	lines: GitConflictTextLine[];
	onChange: (value: string) => void;
	onNext: () => void;
	onPrevious: () => void;
	value: string;
}) {
	return (
		<section className="flex min-h-[38%] flex-1 flex-col overflow-hidden border-t bg-background">
			<header className="flex min-h-10 items-center gap-2 border-b px-3">
				<PencilLine
					className="size-4 text-muted-foreground"
					aria-hidden="true"
				/>
				<h3 className="font-medium text-sm">Result</h3>
				<span className="ml-auto text-muted-foreground text-xs">
					{hunkCount === 0
						? "No conflicts"
						: `Conflict ${Math.min(activeIndex + 1, hunkCount)} of ${hunkCount}`}
				</span>
				<ConflictNavButton
					disabled={hunkCount < 2 || activeIndex === 0}
					icon="previous"
					onClick={onPrevious}
				/>
				<ConflictNavButton
					disabled={hunkCount < 2 || activeIndex >= hunkCount - 1}
					icon="next"
					onClick={onNext}
				/>
			</header>
			<div className="grid min-h-0 flex-1 grid-rows-[1fr_auto] overflow-hidden">
				<div className="overflow-auto font-mono text-[12px] leading-5">
					{lines.map((line) => (
						<div
							className="grid grid-cols-[64px_minmax(0,1fr)]"
							key={line.lineNumber}
						>
							<div className="select-none border-r bg-card/45 px-2 text-right text-muted-foreground">
								{line.lineNumber}
							</div>
							<pre className="min-w-max bg-transparent px-2 whitespace-pre">
								<ConflictRenderedLine line={line} />
							</pre>
						</div>
					))}
				</div>
				<details className="border-t bg-card/20">
					<summary className="cursor-pointer px-3 py-2 text-muted-foreground text-xs">
						Manual edit
					</summary>
					<textarea
						aria-label="Editable conflict result"
						autoCapitalize="off"
						autoComplete="off"
						autoCorrect="off"
						className="h-40 w-full resize-y bg-background px-3 py-2 font-mono text-[12px] leading-5 outline-none selection:bg-primary/25"
						onChange={(event) => onChange(event.currentTarget.value)}
						spellCheck={false}
						value={value}
					/>
				</details>
			</div>
		</section>
	);
}

function ConflictNavButton({
	disabled,
	icon,
	onClick,
}: {
	disabled: boolean;
	icon: "previous" | "next";
	onClick: () => void;
}) {
	const Icon = icon === "previous" ? ChevronUp : ChevronDown;
	return (
		<Button
			aria-label={icon === "previous" ? "Previous conflict" : "Next conflict"}
			disabled={disabled}
			onClick={onClick}
			size="icon-sm"
			title={icon === "previous" ? "Previous conflict" : "Next conflict"}
			type="button"
			variant="ghost"
		>
			<Icon className="size-4" aria-hidden="true" />
		</Button>
	);
}
