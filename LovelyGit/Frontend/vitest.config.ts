import path from "node:path";
import babel from "@rolldown/plugin-babel";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

const performanceTestFiles = [
	"src/components/CommitGraph/components/BranchComparisonContent.test.tsx",
	"src/components/CommitGraph/components/InteractiveRebaseDialog.test.tsx",
	"src/components/CommitGraph/components/RefCellUtils.test.ts",
	"src/components/CommitGraph/components/ReflogDialog.test.tsx",
	"src/components/CommitGraph/components/RefsPanelData.test.ts",
	"src/components/CommitGraph/hooks/useCommitMultiSelection.test.tsx",
	"src/components/ConflictResolution/conflictDisplayPerformance.test.ts",
	"src/components/ConflictResolution/conflictLineRanges.test.ts",
	"src/components/FileHistory/FileHistoryDialog.test.tsx",
	"src/components/WorkingChanges/OptimisticWorkingTreeChanges.test.ts",
	"src/components/WorkingChanges/OptimisticWorkingTreeIndex.test.ts",
	"src/components/WorkingChanges/WorkingChangesDiscardCommand.test.ts",
];

export default defineConfig({
	plugins: [react(), babel({ presets: [reactCompilerPreset()] })],
	resolve: {
		alias: {
			"@/lib/motion": path.resolve(__dirname, "./src/test/motion.ts"),
			"@": path.resolve(__dirname, "./src"),
		},
	},
	test: {
		coverage: {
			exclude: [
				"src/**/*.test.{ts,tsx}",
				"src/generated/**",
				"src/components/ui/**",
				"src/test/**",
			],
			include: ["src/**/*.{ts,tsx}"],
			provider: "v8",
			reporter: ["text", "json-summary", "html"],
			reportsDirectory: "../../artifacts/coverage/frontend",
			thresholds: {
				branches: 35,
				functions: 22,
				lines: 36,
				statements: 38,
			},
		},
		environment: "node",
		projects: [
			{
				extends: true,
				test: {
					exclude: performanceTestFiles,
					include: ["src/**/*.test.{ts,tsx}"],
					name: "unit",
					sequence: { groupOrder: 0 },
				},
			},
			{
				extends: true,
				test: {
					fileParallelism: false,
					include: performanceTestFiles,
					name: "performance",
					sequence: { groupOrder: 1 },
				},
			},
		],
		setupFiles: ["./src/test/setup.ts"],
	},
});
