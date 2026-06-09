import {
	Bot,
	Columns2,
	Cpu,
	FileText,
	Gpu,
	ListCollapse,
	Minus,
	Plus,
	Rows3,
	Settings,
	WrapText,
} from "lucide-react";
import { useEffect, useState } from "react";
import {
	Accordion,
	AccordionContent,
	AccordionItem,
	AccordionTrigger,
} from "@/components/ui/accordion";
import { Button } from "@/components/ui/button";
import {
	Dialog,
	DialogContent,
	DialogDescription,
	DialogHeader,
	DialogTitle,
	DialogTrigger,
} from "@/components/ui/dialog";
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuGroup,
	DropdownMenuLabel,
	DropdownMenuRadioGroup,
	DropdownMenuRadioItem,
	DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { Slider } from "@/components/ui/slider";
import type { AiModelLicenseInfo } from "@/generated/ExpressThat.LovelyGit.Services.Ai.Models";
import type { CommitDiffViewMode } from "@/generated/ExpressThat.LovelyGit.Services.Git.CommitGraph.Models";
import type {
	AiComputeDevice,
	AiModel,
	CommitDiffLineDisplayMode,
} from "@/generated/ExpressThat.LovelyGit.Services.Settings";
import { sendRequestWithResponse } from "@/lib/registerSignalR";
import { setSetting, useSetting } from "@/lib/settings/settingsStore";

type SettingsCategory = "fileDiffView" | "ai";

const categories: Array<{
	description: string;
	icon: typeof FileText;
	id: SettingsCategory;
	label: string;
}> = [
	{
		description: "Diff layout, context, and line wrapping",
		icon: FileText,
		id: "fileDiffView",
		label: "File Diff View",
	},
	{
		description: "AI configuration",
		icon: Bot,
		id: "ai",
		label: "AI",
	},
];

const computeDeviceOptions: Array<{
	icon: typeof Cpu;
	label: string;
	value: AiComputeDevice;
}> = [
	{ icon: Gpu, label: "GPU", value: "Gpu" },
	{ icon: Cpu, label: "CPU", value: "Cpu" },
];

const aiModelOptions: Array<{
	label: string;
	value: AiModel;
}> = [
	{ label: "llama3.2 (1B)", value: "Llama32_1B" },
	{ label: "llama3.2 (3B)", value: "Llama32_3B" },
	{ label: "Gemma 4 E2B IT (Q8)", value: "Gemma4_E2B" },
	{ label: "Gemma 4 E4B IT (Q4_K_M)", value: "Gemma4_E4B" },
];

const aiContextSizes = [
	1024,
	2048,
	4096,
	8192,
	16384,
	32768,
	65536,
	131072,
	250000,
] as const;

export function SettingsDialog() {
	const [open, setOpen] = useState(false);
	const [activeCategory, setActiveCategory] =
		useState<SettingsCategory>("fileDiffView");
	const active = categories.find((category) => category.id === activeCategory);

	return (
		<Dialog open={open} onOpenChange={setOpen}>
			<DialogTrigger
				render={
					<Button
						aria-label="Open settings"
						size="icon-sm"
						title="Settings"
						variant="ghost"
					/>
				}
			>
				<Settings aria-hidden="true" className="size-4" />
			</DialogTrigger>
			<DialogContent className="grid h-[min(560px,calc(100vh-2rem))] max-w-[min(920px,calc(100vw-2rem))] grid-rows-[auto_minmax(0,1fr)] gap-0 overflow-hidden p-0 sm:max-w-[min(920px,calc(100vw-2rem))]">
				<DialogHeader className="border-b px-5 py-4">
					<DialogTitle>Settings</DialogTitle>
					<DialogDescription>
						{active?.description ?? "Application preferences"}
					</DialogDescription>
				</DialogHeader>
				<div className="grid min-h-0 grid-cols-[220px_minmax(0,1fr)]">
					<nav className="border-r bg-card/50 p-2">
						{categories.map((category) => (
							<CategoryButton
								category={category}
								isActive={activeCategory === category.id}
								key={category.id}
								onClick={() => setActiveCategory(category.id)}
							/>
						))}
					</nav>
					<section className="custom-scrollbar min-h-0 overflow-y-auto p-5">
						{activeCategory === "fileDiffView" ? <FileDiffViewSettings /> : null}
						{activeCategory === "ai" ? <AiSettings /> : null}
					</section>
				</div>
			</DialogContent>
		</Dialog>
	);
}

function CategoryButton({
	category,
	isActive,
	onClick,
}: {
	category: (typeof categories)[number];
	isActive: boolean;
	onClick: () => void;
}) {
	return (
		<Button
			className="mb-1 h-auto w-full justify-start gap-2 px-2 py-2 text-left"
			onClick={onClick}
			variant={isActive ? "secondary" : "ghost"}
		>
			<category.icon aria-hidden="true" className="size-4" />
			<span className="min-w-0">
				<span className="block truncate text-sm font-medium">
					{category.label}
				</span>
				<span className="block truncate text-xs text-muted-foreground">
					{category.description}
				</span>
			</span>
		</Button>
	);
}

function FileDiffViewSettings() {
	const viewMode = useSetting("CommitDiffViewMode");
	const lineDisplayMode = useSetting("CommitDiffLineDisplayMode");
	const contextLines = useSetting("CommitDiffContextLines");
	const wrapLines = useSetting("CommitDiffWrapLines");

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Choose how file changes are arranged."
				title="Layout"
			>
				<SegmentedControl>
					<SegmentedButton
						icon={<Columns2 aria-hidden="true" className="size-4" />}
						isActive={viewMode === "SideBySide"}
						label="Side by side"
						onClick={() =>
							void setSetting(
								"CommitDiffViewMode",
								"SideBySide" satisfies CommitDiffViewMode,
							)
						}
					/>
					<SegmentedButton
						icon={<Rows3 aria-hidden="true" className="size-4" />}
						isActive={viewMode === "Combined"}
						label="Combined"
						onClick={() =>
							void setSetting(
								"CommitDiffViewMode",
								"Combined" satisfies CommitDiffViewMode,
							)
						}
					/>
				</SegmentedControl>
			</SettingGroup>

			<SettingGroup
				description="Switch between changed hunks and the whole file."
				title="Line Display"
			>
				<SegmentedControl>
					<SegmentedButton
						icon={<ListCollapse aria-hidden="true" className="size-4" />}
						isActive={lineDisplayMode === "Changes"}
						label="Changes"
						onClick={() =>
							void setSetting(
								"CommitDiffLineDisplayMode",
								"Changes" satisfies CommitDiffLineDisplayMode,
							)
						}
					/>
					<SegmentedButton
						icon={<FileText aria-hidden="true" className="size-4" />}
						isActive={lineDisplayMode === "FullFile"}
						label="Full file"
						onClick={() =>
							void setSetting(
								"CommitDiffLineDisplayMode",
								"FullFile" satisfies CommitDiffLineDisplayMode,
							)
						}
					/>
				</SegmentedControl>
			</SettingGroup>

			<SettingGroup
				description="Set how many unchanged lines surround each change."
				title="Context Lines"
			>
				<div className="inline-flex h-9 overflow-hidden rounded-lg border bg-background">
					<Button
						aria-label="Decrease context lines"
						className="h-full rounded-none border-0"
						disabled={contextLines <= 0}
						onClick={() => updateContextLines(contextLines - 1)}
						size="icon-sm"
						variant="ghost"
					>
						<Minus aria-hidden="true" className="size-4" />
					</Button>
					<div className="flex min-w-12 items-center justify-center border-x px-3 font-mono text-sm">
						{contextLines}
					</div>
					<Button
						aria-label="Increase context lines"
						className="h-full rounded-none border-0"
						disabled={contextLines >= 99}
						onClick={() => updateContextLines(contextLines + 1)}
						size="icon-sm"
						variant="ghost"
					>
						<Plus aria-hidden="true" className="size-4" />
					</Button>
				</div>
			</SettingGroup>

			<SettingGroup
				description="Wrap long diff lines inside the viewport."
				title="Line Wrapping"
			>
				<Button
					onClick={() => void setSetting("CommitDiffWrapLines", !wrapLines)}
					variant={wrapLines ? "secondary" : "outline"}
				>
					<WrapText aria-hidden="true" className="size-4" />
					{wrapLines ? "Wrapping on" : "Wrapping off"}
				</Button>
			</SettingGroup>
		</div>
	);
}

function AiSettings() {
	const aiFeaturesEnabled = useSetting("AiFeaturesEnabled");
	const computeDevice = useSetting("AiComputeDevice");
	const aiModel = useSetting("AiModel");
	const aiContextSize = normalizeAiContextSize(useSetting("AiContextSize"));
	const llamaRawDiffContextPercent = normalizePromptBudgetPercent(
		useSetting("AiLlamaRawDiffContextPercent"),
	);
	const gemmaRawDiffContextPercent = normalizePromptBudgetPercent(
		useSetting("AiGemmaRawDiffContextPercent"),
	);
	const summaryContextPercent = normalizePromptBudgetPercent(
		useSetting("AiSummaryContextPercent"),
	);
	const [modelLicenses, setModelLicenses] = useState<AiModelLicenseInfo[]>([]);
	const [licenseError, setLicenseError] = useState<string | null>(null);
	const [isLoadingLicenses, setIsLoadingLicenses] = useState(false);
	const aiContextSizeIndex = aiContextSizes.findIndex(
		(size) => size === aiContextSize,
	);
	const computeDeviceOption =
		computeDeviceOptions.find((option) => option.value === computeDevice) ??
		computeDeviceOptions[0];
	const modelOption =
		aiModelOptions.find((option) => option.value === aiModel) ??
		aiModelOptions[0];
	const isGemmaModel = aiModel.startsWith("Gemma4_");
	const rawDiffContextPercent = isGemmaModel
		? gemmaRawDiffContextPercent
		: llamaRawDiffContextPercent;
	const rawDiffSetting = isGemmaModel
		? "AiGemmaRawDiffContextPercent"
		: "AiLlamaRawDiffContextPercent";

	useEffect(() => {
		let isMounted = true;

		const loadLicenses = async () => {
			setIsLoadingLicenses(true);
			setLicenseError(null);
			try {
				const response = await sendRequestWithResponse({
					commandType: "GetAiModelLicenses",
					arguments: {},
				});

				if (isMounted) {
					setModelLicenses(response?.licenses ?? []);
				}
			} catch (error) {
				if (isMounted) {
					setLicenseError(
						error instanceof Error
							? error.message
							: "Failed to load model licenses.",
					);
				}
			} finally {
				if (isMounted) {
					setIsLoadingLicenses(false);
				}
			}
		};

		void loadLicenses();
		return () => {
			isMounted = false;
		};
	}, []);

	return (
		<div className="space-y-5">
			<SettingGroup
				description="Enable AI-assisted features across LovelyGit."
				title="Enable AI Features"
			>
				<Button
					onClick={() =>
						void setSetting("AiFeaturesEnabled", !aiFeaturesEnabled)
					}
					variant={aiFeaturesEnabled ? "secondary" : "outline"}
				>
					<Bot aria-hidden="true" className="size-4" />
					{aiFeaturesEnabled ? "Enabled" : "Disabled"}
				</Button>
			</SettingGroup>

			{aiFeaturesEnabled ? (
				<>
					<SettingGroup
						description="Choose the compute device used for local AI work."
						title="Compute Device"
					>
						<DropdownMenu>
							<DropdownMenuTrigger
								render={
									<Button className="min-w-40 justify-start" variant="outline" />
								}
							>
								<computeDeviceOption.icon
									aria-hidden="true"
									className="size-4"
								/>
								{computeDeviceOption.label}
							</DropdownMenuTrigger>
							<DropdownMenuContent className="min-w-40" align="start">
								<DropdownMenuGroup>
									<DropdownMenuLabel>Compute Device</DropdownMenuLabel>
									<DropdownMenuRadioGroup
										value={computeDevice}
										onValueChange={(value) =>
											void setSetting(
												"AiComputeDevice",
												value as AiComputeDevice,
											)
										}
									>
										{computeDeviceOptions.map((option) => (
											<DropdownMenuRadioItem
												key={option.value}
												value={option.value}
											>
												<option.icon aria-hidden="true" className="size-4" />
												{option.label}
											</DropdownMenuRadioItem>
										))}
									</DropdownMenuRadioGroup>
								</DropdownMenuGroup>
							</DropdownMenuContent>
						</DropdownMenu>
					</SettingGroup>

					<SettingGroup
						description="Choose the local model used by AI features."
						title="Model"
					>
						<DropdownMenu>
							<DropdownMenuTrigger
								render={
									<Button className="min-w-44 justify-start" variant="outline" />
								}
							>
								{modelOption.label}
							</DropdownMenuTrigger>
							<DropdownMenuContent className="min-w-44" align="start">
								<DropdownMenuGroup>
									<DropdownMenuLabel>Model</DropdownMenuLabel>
									<DropdownMenuRadioGroup
										value={aiModel}
										onValueChange={(value) =>
											void setSetting("AiModel", value as AiModel)
										}
									>
										{aiModelOptions.map((option) => (
											<DropdownMenuRadioItem
												key={option.value}
												value={option.value}
											>
												{option.label}
											</DropdownMenuRadioItem>
										))}
									</DropdownMenuRadioGroup>
								</DropdownMenuGroup>
							</DropdownMenuContent>
						</DropdownMenu>
					</SettingGroup>

					<SettingGroup
						description="Control how much staged change text is sent to the model."
						title="Prompt Budget"
					>
						<div className="space-y-5">
							<PromptBudgetSlider
								contextSize={aiContextSize}
								description={
									isGemmaModel
										? "Gemma uses raw diff for title generation, then retries from summary if needed."
										: "Llama uses raw diff for title generation."
								}
								label={
									isGemmaModel
										? "Gemma raw diff budget"
										: "Llama raw diff budget"
								}
								onChange={(value) =>
									void setSetting(rawDiffSetting, value)
								}
								value={rawDiffContextPercent}
							/>
							<PromptBudgetSlider
								contextSize={aiContextSize}
								description="Used for commit bodies and summary-only retry prompts."
								label="Summary budget"
								onChange={(value) =>
									void setSetting("AiSummaryContextPercent", value)
								}
								value={summaryContextPercent}
							/>
						</div>
					</SettingGroup>

					<SettingGroup
						description="Review license text before downloading model files."
						title="Model Licenses"
					>
						{licenseError ? (
							<div className="rounded-md border border-destructive/40 bg-destructive/10 p-3 text-sm text-destructive">
								{licenseError}
							</div>
						) : null}
						{isLoadingLicenses && modelLicenses.length === 0 ? (
							<div className="rounded-md border bg-background p-3 text-sm text-muted-foreground">
								Loading model licenses...
							</div>
						) : null}
						{modelLicenses.length > 0 ? (
							<Accordion className="rounded-md border">
								{modelLicenses.map((license) => (
									<AccordionItem
										key={license.model}
										value={license.model}
										className="px-3"
									>
										<AccordionTrigger className="gap-3 py-3 text-left hover:no-underline">
											<span className="min-w-0 flex-1">
												<span className="block truncate text-sm font-medium">
													{license.displayName}
												</span>
												<span className="block truncate text-xs text-muted-foreground">
													{formatLicenseSummary(license)}
												</span>
											</span>
										</AccordionTrigger>
										<AccordionContent>
											<div className="space-y-3 pb-3">
												<div className="grid gap-1 text-xs text-muted-foreground">
													<div>Repository: {license.repositoryId}</div>
													{license.licenseUrl ? (
														<a
															className="text-sky-500 hover:underline"
															href={license.licenseUrl}
															rel="noreferrer"
															target="_blank"
														>
															{license.licenseUrl}
														</a>
													) : null}
												</div>
												<pre className="custom-scrollbar max-h-72 overflow-auto whitespace-pre-wrap rounded-md bg-background p-3 text-xs leading-relaxed text-foreground">
													{license.licenseText}
												</pre>
											</div>
										</AccordionContent>
									</AccordionItem>
								))}
							</Accordion>
						) : null}
					</SettingGroup>

					<SettingGroup
						description="Set the model context window. Larger values need more RAM or VRAM."
						title="Context Window"
					>
						<div className="space-y-3">
							<div className="flex items-center justify-between gap-3">
								<div className="font-mono text-sm text-foreground">
									{aiContextSize.toLocaleString()} tokens
								</div>
								<div className="text-xs text-muted-foreground">
									1024 to 250k
								</div>
							</div>
							<Slider
								max={aiContextSizes.length - 1}
								min={0}
								onValueChange={(value) =>
									updateAiContextSize(firstSliderValue(value, aiContextSizeIndex))
								}
								step={1}
								value={[aiContextSizeIndex]}
							/>
						</div>
					</SettingGroup>
				</>
			) : null}
		</div>
	);
}

function SettingGroup({
	children,
	description,
	title,
}: {
	children: React.ReactNode;
	description: string;
	title: string;
}) {
	return (
		<section className="grid gap-3 border-b pb-5 last:border-b-0 last:pb-0">
			<div>
				<h3 className="text-sm font-semibold">{title}</h3>
				<p className="text-xs text-muted-foreground">{description}</p>
			</div>
			{children}
		</section>
	);
}

function PromptBudgetSlider({
	contextSize,
	description,
	label,
	onChange,
	value,
}: {
	contextSize: number;
	description: string;
	label: string;
	onChange: (value: number) => void;
	value: number;
}) {
	const approxCharacters = Math.max(
		4,
		Math.trunc((contextSize * value * 4) / 100),
	);

	return (
		<div className="space-y-3">
			<div className="flex items-start justify-between gap-3">
				<div>
					<div className="text-sm font-medium text-foreground">{label}</div>
					<div className="text-xs text-muted-foreground">{description}</div>
				</div>
				<div className="shrink-0 text-right">
					<div className="font-mono text-sm text-foreground">{value}%</div>
					<div className="text-xs text-muted-foreground">
						~{approxCharacters.toLocaleString()} chars
					</div>
				</div>
			</div>
			<Slider
				max={80}
				min={5}
				onValueChange={(nextValue) =>
					onChange(firstSliderValue(nextValue, value))
				}
				step={5}
				value={[value]}
			/>
		</div>
	);
}

function SegmentedControl({ children }: { children: React.ReactNode }) {
	return (
		<div className="inline-flex rounded-lg border bg-background p-0.5">
			{children}
		</div>
	);
}

function SegmentedButton({
	icon,
	isActive,
	label,
	onClick,
}: {
	icon: React.ReactNode;
	isActive: boolean;
	label: string;
	onClick: () => void;
}) {
	return (
		<Button
			className="rounded-md"
			onClick={onClick}
			variant={isActive ? "secondary" : "ghost"}
		>
			{icon}
			{label}
		</Button>
	);
}

function formatLicenseSummary(license: AiModelLicenseInfo) {
	return `${license.licenseName || "License metadata"} · ${
		license.isCached ? "cached" : "remote"
	}`;
}

function updateContextLines(value: number) {
	const nextValue = Math.max(0, Math.min(99, Math.trunc(value)));
	void setSetting("CommitDiffContextLines", nextValue);
}

function normalizeAiContextSize(value: number) {
	if (aiContextSizes.includes(value as (typeof aiContextSizes)[number])) {
		return value;
	}

	let closest: number = aiContextSizes[0];
	let closestDistance = Math.abs(value - closest);
	for (const size of aiContextSizes) {
		const distance = Math.abs(value - size);
		if (distance < closestDistance) {
			closest = size;
			closestDistance = distance;
		}
	}

	return closest;
}

function normalizePromptBudgetPercent(value: number) {
	return Math.max(5, Math.min(80, Math.trunc(value)));
}

function updateAiContextSize(index: number) {
	const nextIndex = Math.max(
		0,
		Math.min(aiContextSizes.length - 1, Math.trunc(index)),
	);
	void setSetting("AiContextSize", aiContextSizes[nextIndex]);
}

function firstSliderValue(value: number | readonly number[], fallback: number) {
	return Array.isArray(value) ? (value[0] ?? fallback) : value;
}
