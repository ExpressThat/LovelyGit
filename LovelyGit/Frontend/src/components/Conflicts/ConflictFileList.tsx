import { AlertTriangle, CheckCircle2, FileWarning } from "lucide-react";
import type { GitConflictFile } from "@/generated/types";

export function ConflictFileList({
	conflictedFiles,
	onSelectFile,
	resolvedFiles,
	selectedPath,
}: {
	conflictedFiles: GitConflictFile[];
	onSelectFile: (file: GitConflictFile) => void;
	resolvedFiles: GitConflictFile[];
	selectedPath: string | null;
}) {
	return (
		<aside className="flex min-h-0 w-80 flex-col border-r bg-card/40">
			<FileGroup
				files={conflictedFiles}
				icon="conflict"
				onSelectFile={onSelectFile}
				selectedPath={selectedPath}
				title={`Conflicted Files (${conflictedFiles.length})`}
			/>
			<FileGroup
				files={resolvedFiles}
				icon="resolved"
				onSelectFile={onSelectFile}
				selectedPath={selectedPath}
				title={`Resolved Files (${resolvedFiles.length})`}
			/>
		</aside>
	);
}

function FileGroup({
	files,
	icon,
	onSelectFile,
	selectedPath,
	title,
}: {
	files: GitConflictFile[];
	icon: "conflict" | "resolved";
	onSelectFile: (file: GitConflictFile) => void;
	selectedPath: string | null;
	title: string;
}) {
	const Icon = icon === "conflict" ? AlertTriangle : CheckCircle2;
	return (
		<section className="min-h-0 flex-1 overflow-hidden border-b">
			<h2 className="flex items-center gap-2 border-b px-3 py-2 font-medium text-sm">
				<Icon className="size-4" />
				{title}
			</h2>
			<div className="max-h-full overflow-auto p-2">
				{files.map((file) => (
					<button
						className={`flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm ${
							file.path === selectedPath
								? "bg-accent text-accent-foreground"
								: "hover:bg-accent/70"
						}`}
						key={`${icon}:${file.path}`}
						onClick={() => onSelectFile(file)}
						title={file.path}
						type="button"
					>
						{icon === "conflict" ? (
							<FileWarning className="size-4 shrink-0 text-amber-500" />
						) : (
							<CheckCircle2 className="size-4 shrink-0 text-emerald-500" />
						)}
						<span className="min-w-0 flex-1 truncate">{file.path}</span>
						{file.conflictCount > 0 ? (
							<span className="text-muted-foreground text-xs">
								{file.conflictCount}
							</span>
						) : null}
					</button>
				))}
			</div>
		</section>
	);
}
