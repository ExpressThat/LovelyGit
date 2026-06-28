import { toast } from "sonner";

export async function copyToClipboard(value: string, label: string) {
	try {
		await navigator.clipboard.writeText(value);
		toast.success(`${label} copied`);
	} catch {
		toast.error(`Could not copy ${label.toLowerCase()}`);
	}
}
