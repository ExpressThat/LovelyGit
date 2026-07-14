import path from "node:path";
import babel from "@rolldown/plugin-babel";
import react, { reactCompilerPreset } from "@vitejs/plugin-react";
import { defineConfig } from "vitest/config";

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
		setupFiles: ["./src/test/setup.ts"],
	},
});
