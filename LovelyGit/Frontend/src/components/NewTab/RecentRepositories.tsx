import { Search } from "lucide-react";
import { useMemo, useState } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import type { KnownGitRepository } from "@/generated/types";
import { useRepositoryContext } from "@/lib/repositoryContext";
import { filterRepositories } from "./RepositorySearch";

export function RecentRepositories() {
	const { repositories, setCurrentRepositoryId } = useRepositoryContext();
	const [query, setQuery] = useState("");
	const filtered = useMemo(
		() => filterRepositories(repositories, query),
		[query, repositories],
	);

	if (repositories.length === 0) {
		return null;
	}

	return (
		<section className="mt-5 w-full max-w-2xl">
			<div className="mb-2 flex items-center justify-between gap-3">
				<h2 className="font-semibold text-sm">Recent repositories</h2>
				<span className="text-muted-foreground text-xs">
					{filtered.length} of {repositories.length}
				</span>
			</div>
			<label className="relative mb-3 block" htmlFor="repository-search">
				<span className="sr-only">Search repositories</span>
				<Search
					aria-hidden="true"
					className="absolute left-2 top-1/2 size-4 -translate-y-1/2 text-muted-foreground"
				/>
				<Input
					autoComplete="off"
					className="pl-8"
					id="repository-search"
					onChange={(event) => setQuery(event.currentTarget.value)}
					onInput={(event) => setQuery(event.currentTarget.value)}
					placeholder="Search by name or path"
					spellCheck={false}
					value={query}
				/>
			</label>
			<div className="grid gap-1">
				{filtered.length > 0 ? (
					filtered.map((repository) => (
						<RepositoryRow
							key={repository.id}
							onOpen={() => void setCurrentRepositoryId(repository.id)}
							repository={repository}
						/>
					))
				) : (
					<p className="rounded-md border border-dashed px-3 py-4 text-muted-foreground text-sm">
						No repositories match this search.
					</p>
				)}
			</div>
		</section>
	);
}

function RepositoryRow({
	onOpen,
	repository,
}: {
	onOpen: () => void;
	repository: KnownGitRepository;
}) {
	const path = repository.path ?? "";
	const label = repository.name || pathTail(path) || "Repository";
	return (
		<Button
			className="h-auto min-w-0 justify-start px-3 py-2"
			onClick={onOpen}
			title={path || label}
			type="button"
			variant="ghost"
		>
			<span className="grid min-w-0 text-left">
				<span className="truncate font-medium">{label}</span>
				<span className="truncate font-mono text-muted-foreground text-xs">
					{path}
				</span>
			</span>
		</Button>
	);
}

function pathTail(path: string) {
	const normalized = path.replaceAll("\\", "/").replace(/\/+$/, "");
	const slashIndex = normalized.lastIndexOf("/");
	return slashIndex >= 0 ? normalized.slice(slashIndex + 1) : normalized;
}
