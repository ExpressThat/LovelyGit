import { useId } from "react";
import { Checkbox } from "@/components/ui/checkbox";
import type { PatchPreviewResponse } from "@/generated/types";

const MaximumVisibleFiles = 100;

export function PatchApplyPreview({
	disabled,
	onReverseChange,
	onStageChangesChange,
	preview,
	reverse,
	stageChanges,
}: {
	disabled: boolean;
	onReverseChange: (checked: boolean) => void;
	onStageChangesChange: (checked: boolean) => void;
	preview: PatchPreviewResponse;
	reverse: boolean;
	stageChanges: boolean;
}) {
	return (
		<>
			<div className="grid grid-cols-3 gap-2">
				<Metric label="Files" value={preview.files.length} />
				<Metric
					className="text-emerald-500"
					label="Additions"
					value={`+${preview.totalAdditions}`}
				/>
				<Metric
					className="text-rose-500"
					label="Deletions"
					value={`−${preview.totalDeletions}`}
				/>
			</div>
			<PatchFiles preview={preview} />
			<div className="grid gap-3 rounded-lg border bg-card p-3">
				<Option
					checked={stageChanges}
					description="Update the index as well as the working tree."
					disabled={disabled}
					label="Stage applied changes"
					onChange={onStageChangesChange}
				/>
				<Option
					checked={reverse}
					description="Apply the inverse of every change in this patch."
					disabled={disabled}
					label="Reverse patch"
					onChange={onReverseChange}
				/>
			</div>
		</>
	);
}

function Metric({
	className = "",
	label,
	value,
}: {
	className?: string;
	label: string;
	value: number | string;
}) {
	return (
		<div className="rounded-lg border bg-card p-3">
			<div className={`font-semibold text-lg ${className}`}>{value}</div>
			<div className="text-muted-foreground text-xs">{label}</div>
		</div>
	);
}

function PatchFiles({ preview }: { preview: PatchPreviewResponse }) {
	const visibleFiles = preview.files.slice(0, MaximumVisibleFiles);
	return (
		<div className="custom-scrollbar max-h-64 overflow-y-auto rounded-lg border bg-background">
			{visibleFiles.map((file) => (
				<div
					className="flex h-8 items-center gap-2 border-b px-3 last:border-b-0"
					key={file.path}
				>
					<span
						className="min-w-0 flex-1 truncate font-mono text-xs"
						title={file.path}
					>
						{file.path}
					</span>
					<span className="text-emerald-500 text-xs">+{file.additions}</span>
					<span className="text-rose-500 text-xs">−{file.deletions}</span>
				</div>
			))}
			{preview.files.length > visibleFiles.length ? (
				<div className="px-3 py-2 text-muted-foreground text-xs">
					And {preview.files.length - visibleFiles.length} more files
				</div>
			) : null}
		</div>
	);
}

function Option({
	checked,
	description,
	disabled,
	label,
	onChange,
}: {
	checked: boolean;
	description: string;
	disabled: boolean;
	label: string;
	onChange: (checked: boolean) => void;
}) {
	const id = useId();
	return (
		<div className="flex items-start gap-3">
			<Checkbox
				aria-label={label}
				checked={checked}
				disabled={disabled}
				id={id}
				onCheckedChange={(value) => onChange(value === true)}
			/>
			<label htmlFor={id}>
				<span className="block font-medium text-sm">{label}</span>
				<span className="text-muted-foreground text-xs">{description}</span>
			</label>
		</div>
	);
}
