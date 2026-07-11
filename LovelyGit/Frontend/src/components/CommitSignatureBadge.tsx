import { BadgeCheck } from "@/components/icons/lovelyIcons";
import type { CommitSignatureKind } from "@/generated/types";

export function CommitSignatureBadge({
	compact = false,
	kind,
}: {
	compact?: boolean;
	kind: CommitSignatureKind;
}) {
	if (kind === "None") return null;
	const label =
		kind === "Unknown" ? "Signed commit" : `Signed commit (${labelFor(kind)})`;
	return (
		<span
			aria-label={label}
			className="inline-flex shrink-0 items-center gap-1 rounded-full border border-primary/25 bg-primary/10 px-1.5 py-0.5 text-[10px] font-medium text-primary"
			title={`${label}. Signature presence has not been cryptographically verified.`}
			role="img"
		>
			<BadgeCheck aria-hidden="true" className="size-3" />
			{compact ? null : (
				<span>{kind === "Unknown" ? "Signed" : labelFor(kind)}</span>
			)}
		</span>
	);
}

function labelFor(kind: Exclude<CommitSignatureKind, "None" | "Unknown">) {
	return kind === "OpenPgp" ? "OpenPGP" : kind === "Ssh" ? "SSH" : "X.509";
}
