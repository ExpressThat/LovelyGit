import { type ClassValue, clsx } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
	return twMerge(clsx(inputs));
}

export function getPathTail(path: string): string {
	if (path.length === 0) {
		return "";
	}

	const trimmedPath = path.replace(/[\\/]+$/g, "");
	if (trimmedPath.length === 0) {
		return "";
	}

	const segments = trimmedPath.split(/[\\/]+/);
	return segments[segments.length - 1] ?? "";
}
