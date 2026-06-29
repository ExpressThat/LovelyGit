import { AlertTriangle, CheckCircle2, ListTree } from "lucide-react";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import type { GitConflictFile } from "@/generated/types";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";
import { FileButton, FileTreeGroup } from "./ConflictFileRows";

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
	const viewMode = useSetting("ConflictFileViewMode");
	return (
		<aside className="flex min-h-0 w-88 flex-col border-r bg-card/40">
			<Tabs
				className="min-h-0 flex-1 gap-0"
				onValueChange={(value) =>
					void setSetting(
						"ConflictFileViewMode",
						value === "Tree" ? "Tree" : "Path",
					)
				}
				value={viewMode}
			>
				<div className="flex items-center justify-between border-b px-3 py-2">
					<h2 className="font-medium text-sm">Conflict files</h2>
					<TabsList aria-label="Conflict file view" variant="line">
						<TabsTrigger title="Show conflict files by path" value="Path">
							Path
						</TabsTrigger>
						<TabsTrigger title="Show conflict files as a tree" value="Tree">
							<ListTree aria-hidden="true" />
							Tree
						</TabsTrigger>
					</TabsList>
				</div>
				<TabsContent className="min-h-0 overflow-hidden" value="Path">
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
				</TabsContent>
				<TabsContent
					className="custom-scrollbar min-h-0 overflow-auto p-2"
					value="Tree"
				>
					<FileTreeGroup
						files={conflictedFiles}
						icon="conflict"
						onSelectFile={onSelectFile}
						selectedPath={selectedPath}
						title={`Conflicted (${conflictedFiles.length})`}
					/>
					<FileTreeGroup
						files={resolvedFiles}
						icon="resolved"
						onSelectFile={onSelectFile}
						selectedPath={selectedPath}
						title={`Resolved (${resolvedFiles.length})`}
					/>
				</TabsContent>
			</Tabs>
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
			<div className="custom-scrollbar max-h-full overflow-auto p-2">
				{files.length > 0 ? (
					files.map((file) => (
						<FileButton
							file={file}
							icon={icon}
							key={`${icon}:${file.path}`}
							onSelectFile={onSelectFile}
							selectedPath={selectedPath}
						/>
					))
				) : (
					<p className="px-2 py-1.5 text-muted-foreground text-xs">
						No files in this group.
					</p>
				)}
			</div>
		</section>
	);
}
