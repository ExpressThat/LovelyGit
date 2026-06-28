import type { GitConflictFile } from "@/generated/types";

export type ConflictFileTreeNode =
	| {
			children: ConflictFileTreeNode[];
			name: string;
			type: "directory";
	  }
	| {
			file: GitConflictFile;
			name: string;
			type: "file";
	  };

export function buildConflictFileTree(
	files: GitConflictFile[],
): ConflictFileTreeNode[] {
	const roots = new Map<string, MutableNode>();

	for (const file of files) {
		const parts = file.path.split("/").filter(Boolean);
		if (parts.length === 0) {
			continue;
		}

		let current: Map<string, MutableNode> = roots;
		for (const [index, part] of parts.entries()) {
			if (index === parts.length - 1) {
				current.set(part, { file, name: part, type: "file" });
				continue;
			}

			let node = current.get(part);
			if (!node || node.type === "file") {
				node = { children: new Map(), name: part, type: "directory" };
				current.set(part, node);
			}
			current = node.children;
		}
	}

	return sortNodes([...roots.values()]).map(toPublicNode);
}

type MutableFileNode = Extract<ConflictFileTreeNode, { type: "file" }>;

type MutableDirectoryNode = {
	children: Map<string, MutableNode>;
	name: string;
	type: "directory";
};

type MutableNode = MutableDirectoryNode | MutableFileNode;

function sortNodes(nodes: MutableNode[]) {
	return nodes.sort((left, right) => {
		if (left.type !== right.type) {
			return left.type === "directory" ? -1 : 1;
		}
		return left.name.localeCompare(right.name);
	});
}

function toPublicNode(node: MutableNode): ConflictFileTreeNode {
	if (node.type === "file") {
		return node;
	}

	return {
		children: sortNodes([...node.children.values()]).map(toPublicNode),
		name: node.name,
		type: "directory",
	};
}
