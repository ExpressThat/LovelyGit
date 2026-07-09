export function inferCloneDirectoryName(remoteUrl: string) {
	const withoutQuery = remoteUrl
		.trim()
		.split(/[?#]/, 1)[0]
		.replace(/[\\/]+$/, "");
	const separatorIndex = Math.max(
		withoutQuery.lastIndexOf("/"),
		withoutQuery.lastIndexOf(":"),
	);
	let name =
		separatorIndex >= 0 ? withoutQuery.slice(separatorIndex + 1) : withoutQuery;
	if (name.toLocaleLowerCase().endsWith(".git")) {
		name = name.slice(0, -4);
	}

	try {
		name = decodeURIComponent(name);
	} catch {
		// Keep the original segment when it contains malformed URL escapes.
	}

	return name.trim();
}
