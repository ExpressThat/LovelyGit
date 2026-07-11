import { toast } from "sonner";

export async function copyToClipboard(value: string, label: string) {
	try {
		if (navigator.clipboard?.writeText) {
			await navigator.clipboard.writeText(value);
		} else if (!copyWithSelection(value)) {
			throw new Error("Clipboard is unavailable");
		}
		toast.success(`${label} copied`);
	} catch {
		if (copyWithSelection(value)) {
			toast.success(`${label} copied`);
		} else {
			toast.error(`Could not copy ${label.toLowerCase()}`);
		}
	}
}

function copyWithSelection(value: string) {
	const textarea = document.createElement("textarea");
	textarea.value = value;
	textarea.setAttribute("readonly", "");
	textarea.style.position = "fixed";
	textarea.style.opacity = "0";
	document.body.append(textarea);
	textarea.select();
	const copied = document.execCommand?.("copy") ?? false;
	textarea.remove();
	return copied;
}
