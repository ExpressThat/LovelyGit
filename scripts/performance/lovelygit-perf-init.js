(function () {
	if (window.__lovelyGitPerfInstalled) {
		return;
	}

	window.__lovelyGitPerfInstalled = true;

	const thresholds = {
		startupToUsableMs: 4000,
		firstGraphPageMs: 1500,
		scrollPagingMs: 500,
		commitDetailsMs: 500,
		commandRoundTripMs: 200,
		processMemoryBytes: 200 * 1024 * 1024,
	};
	const state = {
		errors: [],
		firstCommitRowAt: null,
		firstDetailsAt: null,
		marks: {},
		samples: [],
		thresholds,
	};

	function messageId() {
		return crypto.randomUUID();
	}

	function metric(metrics, camelName, pascalName) {
		return metrics?.[camelName] ?? metrics?.[pascalName] ?? 0;
	}

	function responseField(response, camelName, pascalName) {
		return response?.[camelName] ?? response?.[pascalName];
	}

	function markDomMilestones() {
		if (
			state.firstCommitRowAt === null &&
			document.querySelector('[data-lg-perf="commit-row"]')
		) {
			state.firstCommitRowAt = performance.now();
		}
		if (
			state.firstDetailsAt === null &&
			document.querySelector('[data-lg-perf="commit-details"]')
		) {
			state.firstDetailsAt = performance.now();
		}
	}

	function maxMemoryBytes() {
		return state.samples.reduce((max, sample) => {
			const metrics = sample.metrics ?? {};
			return Math.max(
				max,
				metric(metrics, "managedMemoryBytes", "ManagedMemoryBytes"),
				metric(metrics, "workingSetBytes", "WorkingSetBytes"),
				metric(metrics, "privateMemoryBytes", "PrivateMemoryBytes"),
			);
		}, 0);
	}

	function requestNative(messageType, body) {
		const messaging = window.infiniframe?.messaging;
		if (
			!messaging ||
			typeof messaging.sendMessageToHost !== "function" ||
			typeof messaging.assignMessageReceivedHandler !== "function"
		) {
			throw new Error("LovelyGit native messaging is unavailable.");
		}

		const id = messageId();
		const startedAt = performance.now();
		const payload = {
			body: body ?? {},
			messageId: id,
		};

		return new Promise((resolve, reject) => {
			const timeoutId = window.setTimeout(() => {
				reject(new Error(`Timed out waiting for ${messageType}.`));
			}, 15000);

			messaging.assignMessageReceivedHandler(messageType, (rawPayload) => {
				let response;
				try {
					response =
						typeof rawPayload === "string"
							? JSON.parse(rawPayload)
							: rawPayload;
				} catch (error) {
					window.clearTimeout(timeoutId);
					reject(error);
					return;
				}

				if (responseField(response, "messageId", "MessageId") !== id) {
					return;
				}

				window.clearTimeout(timeoutId);
				const metrics = responseField(response, "metrics", "Metrics") ?? null;
				const sample = {
					messageId: id,
					messageType,
					metrics,
					receivedAt: performance.now(),
					roundTripMs: Math.ceil(performance.now() - startedAt),
					success: Boolean(responseField(response, "success", "Success")),
				};
				state.samples.push(sample);

				if (!sample.success) {
					reject(
						new Error(
							responseField(response, "error", "Error") ??
								`${messageType} failed.`,
						),
					);
					return;
				}

				resolve(responseField(response, "body", "Body"));
			});

			messaging.sendMessageToHost(messageType, payload);
		});
	}

	function firstSample(messageType) {
		return state.samples.find((sample) => sample.messageType === messageType) ?? null;
	}

	function sampleAfter(messageType, markName) {
		const mark = state.marks[markName];
		if (typeof mark !== "number") {
			return null;
		}
		return (
			state.samples.find(
				(sample) => sample.messageType === messageType && sample.receivedAt >= mark,
			) ?? null
		);
	}

	function buildSummary() {
		markDomMilestones();
		const firstGraphPage = firstSample("CommitGraph");
		const scrollPaging = sampleAfter("CommitGraph", "beforeScrollPage");
		const commitDetails = sampleAfter("GetCommitDetails", "beforeDirectDetails");
		const startupToUsableMs =
			state.firstCommitRowAt === null ? null : Math.ceil(state.firstCommitRowAt);
		const commitDetailsUiMs =
			state.firstDetailsAt === null ||
			typeof state.marks.beforeDetailsClick !== "number"
				? null
				: Math.ceil(state.firstDetailsAt - state.marks.beforeDetailsClick);
		const memoryBytes = maxMemoryBytes();
		const checks = [
			{
				actual: startupToUsableMs,
				limit: thresholds.startupToUsableMs,
				name: "startup-to-usable",
				unit: "ms",
			},
			{
				actual: firstGraphPage?.roundTripMs ?? null,
				limit: thresholds.firstGraphPageMs,
				name: "first-graph-page",
				unit: "ms",
			},
			{
				actual: scrollPaging?.roundTripMs ?? null,
				limit: thresholds.scrollPagingMs,
				name: "scroll-paging",
				unit: "ms",
			},
			{
				actual: commitDetails?.roundTripMs ?? commitDetailsUiMs,
				limit: thresholds.commitDetailsMs,
				name: "commit-details",
				unit: "ms",
			},
			{
				actual: memoryBytes || null,
				limit: thresholds.processMemoryBytes,
				name: "process-memory",
				unit: "bytes",
			},
		];
		const commandMisses = state.samples.filter(
			(sample) =>
				sample.roundTripMs > thresholds.commandRoundTripMs ||
				metric(sample.metrics, "durationMs", "DurationMs") >
					thresholds.commandRoundTripMs,
		);
		const misses = checks.filter(
			(check) => check.actual === null || check.actual > check.limit,
		);

		return {
			checks,
			commandMisses,
			errors: state.errors,
			metricsVersion: 1,
			misses,
			samples: state.samples,
			thresholds,
			timings: {
				commitDetailsUiMs,
				startupToUsableMs,
			},
		};
	}

	window.__lovelyGitPerf = {
		mark(name) {
			state.marks[name] = performance.now();
			if (name === "beforeDetailsClick") {
				state.firstDetailsAt = null;
			}
		},
		async runGuardrail() {
			markDomMilestones();
			const settings = await requestNative("GetAllSettings", {});
			const repositoryId = settings?.CurrentGitRepositoryId;
			if (!repositoryId) {
				state.errors.push("CurrentGitRepositoryId is not set.");
				return buildSummary();
			}

			const firstGraphPage = await requestNative("CommitGraph", {
				cursor: null,
				knownRepositoryId: repositoryId,
				limit: 400,
			});
			const firstRow = firstGraphPage?.rows?.[0];
			const commitHash = firstRow?.commit?.hash;
			state.marks.beforeScrollPage = performance.now();
			await requestNative("CommitGraph", {
				cursor: firstGraphPage?.nextCursor ?? null,
				knownRepositoryId: repositoryId,
				limit: 400,
			});

			if (commitHash) {
				state.marks.beforeDirectDetails = performance.now();
				await requestNative("GetCommitDetails", {
					commitHash,
					repositoryId,
				});
			} else {
				state.errors.push("First graph page did not contain a commit row.");
			}

			return buildSummary();
		},
		summary: buildSummary,
	};

	const observer = new MutationObserver(markDomMilestones);
	if (document.documentElement) {
		observer.observe(document.documentElement, {
			childList: true,
			subtree: true,
		});
	}
	markDomMilestones();
})();
