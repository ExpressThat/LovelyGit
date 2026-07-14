import {
	type ComponentPropsWithoutRef,
	type ForwardRefExoticComponent,
	forwardRef,
	type RefAttributes,
} from "react";
import iconSpriteMarkup from "@/assets/lovely-icons.svg?raw";
import { cn } from "@/lib/utils";

const iconSpriteHostId = "lovely-icon-sprite";

export const lovelyIconNames = [
	"added-file",
	"amend-commit-message",
	"appearance-settings",
	"apply-patch-file",
	"apply-stash",
	"auto-configure",
	"bisect-bad",
	"bisect-good",
	"close",
	"collapse-left",
	"collapse-unchanged-lines",
	"command-palette",
	"commit-information",
	"compare-revisions",
	"confirm",
	"conflicted-file",
	"continue-operation",
	"copy-to-clipboard",
	"create-new",
	"create-pull-request",
	"dangerous-operation",
	"delete-permanently",
	"deleted-file",
	"discard-diff-hunk",
	"disconnect-submodule",
	"download-from-remote",
	"expand-down",
	"expand-right",
	"expand-up",
	"fetch-remote-branch",
	"file-diff",
	"file-history",
	"file",
	"full-file-view",
	"git-bisect",
	"git-branch",
	"git-commit",
	"git-identity",
	"git-lfs",
	"git-tag",
	"git-worktree",
	"hide-unmodified-lines",
	"incoming-commits",
	"initialize-submodule",
	"inspect-file",
	"interactive-rebase",
	"lfs-tracked-file",
	"local-worktree",
	"lock-worktree",
	"merge-branches",
	"move-down",
	"move-up",
	"omit-change",
	"open-external-link",
	"open-merge-tool",
	"open-repository",
	"open-selected-result",
	"open-terminal",
	"operation-in-progress",
	"outgoing-commits",
	"publish-to-remote",
	"pull-from-remote",
	"push-to-remote",
	"reflog-entry",
	"reflog-history",
	"refresh-repository",
	"remote-actions",
	"remote-fork",
	"remote-reference",
	"remove-file-content",
	"remove-line",
	"remove-upstream",
	"rename-or-edit",
	"reset-modes",
	"reset-revision",
	"resize-horizontally",
	"resize-vertically",
	"restore-default",
	"restore-stash",
	"save-changes",
	"search",
	"set-upstream",
	"settings",
	"show-whitespace",
	"side-by-side-diff",
	"skip-bisect-commit",
	"stage-changes",
	"stage-diff-hunk",
	"stash",
	"submodule",
	"submodules",
	"toggle-refs-panel",
	"undo-last-action",
	"unified-diff",
	"unknown-file",
	"unlock-worktree",
	"unstage-changes",
	"verified-signature",
	"warning-triangle",
	"wrap-long-lines",
] as const;

export type LovelyIconName = (typeof lovelyIconNames)[number];
export type LovelyIconProps = Omit<
	ComponentPropsWithoutRef<"svg">,
	"children"
> & {
	size?: number | string;
	strokeWidth?: number | string;
	absoluteStrokeWidth?: boolean;
};
export type LucideIcon = ForwardRefExoticComponent<
	LovelyIconProps & RefAttributes<SVGSVGElement>
>;

export function getLovelyIconUrl(name: LovelyIconName): string {
	return `#lovely-${name}`;
}

export function installLovelyIconSprite(ownerDocument: Document): void {
	if (ownerDocument.getElementById(iconSpriteHostId)) return;
	const host = ownerDocument.createElement("div");
	host.id = iconSpriteHostId;
	host.ariaHidden = "true";
	host.style.cssText =
		"position:absolute;width:0;height:0;overflow:hidden;pointer-events:none";
	host.innerHTML = iconSpriteMarkup;
	ownerDocument.body.prepend(host);
}

export function createLovelyIcon(
	name: LovelyIconName,
	displayName: string,
): LucideIcon {
	const href = getLovelyIconUrl(name);
	const Icon = forwardRef<SVGSVGElement, LovelyIconProps>(function LovelyIcon(
		{ className, size, style, strokeWidth = 2, absoluteStrokeWidth, ...props },
		ref,
	) {
		const labelled =
			props["aria-label"] != null || props["aria-labelledby"] != null;
		const resolvedStrokeWidth =
			absoluteStrokeWidth && typeof size === "number"
				? (Number(strokeWidth) * 24) / size
				: strokeWidth;
		return (
			<svg
				aria-hidden={labelled ? undefined : true}
				fill="none"
				role={labelled ? "img" : undefined}
				stroke="currentColor"
				strokeLinecap="round"
				strokeLinejoin="round"
				strokeWidth={resolvedStrokeWidth}
				viewBox="0 0 24 24"
				className={cn("inline-block size-6 shrink-0 align-middle", className)}
				ref={ref}
				style={{
					...(size == null ? {} : { height: size, width: size }),
					...style,
				}}
				{...props}
			>
				<title>{displayName}</title>
				<use href={href} />
			</svg>
		);
	});
	Icon.displayName = displayName;
	return Icon;
}
