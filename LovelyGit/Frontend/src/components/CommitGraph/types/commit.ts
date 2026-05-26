export type CommitInfo = {
	hash: string;
	parents: string[];
	author: string;
	email: string;
	date: number;
	message: string;
	branches: string[];
	tags: string[];
	stats: CommitStats | null;
};

export type CommitStats = {
	additions: number;
	deletions: number;
};
