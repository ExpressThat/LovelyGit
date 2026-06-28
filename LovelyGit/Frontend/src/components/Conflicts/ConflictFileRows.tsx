import {
	AlertTriangle,
	CheckCircle2,
	ChevronRight,
	FileWarning,
	Folder,
} from "lucide-react";
import type { CSSProperties } from "react";
import type { GitConflictFile } from "@/generated/types";
import {
	buildConflictFileTree,
	type ConflictFileTreeNode,
} from "./ConflictFileTree";

export function FileButton({
	file,
	icon,
	onSelectFile,
	selectedPath,
	style,
}: {
	file: GitConflictFile;
	icon: "conflict" | "resolved";
	onSelectFile: (file: GitConflictFile) => void;
	selectedPath: string | null;
	style?: CSSProperties;
}) {
	return (
		<button
			className={`flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm ${
				file.path === selectedPath
					? "bg-accent text-accent-foreground"
					: "hover:bg-accent/70"
			}`}
			onClick={() => onSelectFile(file)}
			style={style}
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
	);
}

export function FileTreeGroup({
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
	const tree = buildConflictFileTree(files);
	const Icon = icon === "conflict" ? AlertTriangle : CheckCircle2;
	return (
		<section className="mb-3 last:mb-0">
			<h3 className="mb-1 flex items-center gap-2 px-1 font-medium text-muted-foreground text-xs">
				<Icon className="size-3.5" />
				{title}
			</h3>
			{tree.length > 0 ? (
				tree.map((node) => (
					<TreeNode
						depth={0}
						icon={icon}
						key={`${icon}:${node.name}`}
						node={node}
						onSelectFile={onSelectFile}
						selectedPath={selectedPath}
					/>
				))
			) : (
				<p className="px-2 py-1.5 text-muted-foreground text-xs">
					No files in this group.
				</p>
			)}
		</section>
	);
}

function TreeNode({
	depth,
	icon,
	node,
	onSelectFile,
	selectedPath,
}: {
	depth: number;
	icon: "conflict" | "resolved";
	node: ConflictFileTreeNode;
	onSelectFile: (file: GitConflictFile) => void;
	selectedPath: string | null;
}) {
	if (node.type === "file") {
		return (
			<FileButton
				file={node.file}
				icon={icon}
				onSelectFile={onSelectFile}
				selectedPath={selectedPath}
				style={{ paddingLeft: `${depth * 14 + 8}px` }}
			/>
		);
	}

	return (
		<div>
			<div
				className="flex items-center gap-1.5 px-2 py-1 text-muted-foreground text-xs"
				style={{ paddingLeft: `${depth * 14 + 8}px` }}
				title={node.name}
			>
				<ChevronRight aria-hidden="true" className="size-3" />
				<Folder aria-hidden="true" className="size-3.5" />
				<span className="truncate">{node.name}</span>
			</div>
			{node.children.map((child) => (
				<TreeNode
					depth={depth + 1}
					icon={icon}
					key={`${node.name}:${child.name}`}
					node={child}
					onSelectFile={onSelectFile}
					selectedPath={selectedPath}
				/>
			))}
		</div>
	);
}
