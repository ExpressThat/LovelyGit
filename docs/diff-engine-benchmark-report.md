# Diff Engine Benchmark Report

Generated: 2026-06-29T08:46:10.9848571+01:00
Iterations: 1
Per-run timeout: 5000 ms
Synthetic line counts: 10, 100, 1,000, 10,000, 100,000, 1,000,000
Real Chromium repo: `C:\Projects\chromium-tessting`
Real Chromium files are included as additional benchmark rows.
Runtime: Native AOT published `win-x64` benchmark binary

## Candidates

| Candidate | Category | Max lines | Notes |
|---|---:|---:|---|
| DiffPlex | managed | 1,000,000 | full requested sweep |
| spkl.Diffs | managed | 1,000,000 | full requested sweep |
| MyersDiff | managed | 1,000,000 | full requested sweep |
| Diff4Net | managed/netfx | 1,000,000 | full requested sweep |
| NGitDiff Myers | managed/netfx | 1,000,000 | full requested sweep |
| NGitDiff Histogram | managed/netfx | 1,000,000 | full requested sweep |
| CSharpDiff | managed | 1,000,000 | full requested sweep |
| DiffMatchPatch | text-sync | 1,000,000 | full requested sweep |
| Git CLI | reference/patch-output | 1,000,000 | full requested sweep |
| LovelyGit Prototype | prototype | 1,000,000 | full requested sweep |

## Implementation Decision

- Keep DiffPlex as the safe production baseline while replacing large add/delete and simple-edit paths with LovelyGit-owned fast paths.
- Do not switch wholesale to Git CLI, DiffMatchPatch, CSharpDiff, Diff4Net, or NGitDiff; they are either reference-only, not Git-style line engines, older netfx packages, or slower on key large/repeated cases.
- Continue benchmarking `spkl.Diffs`, `MyersDiff`, and the LovelyGit prototype for the general middle algorithm before replacing the full DiffPlex path.
- The measured bottleneck for very large files is no longer only diff algorithm time; JSON payload size and serialization become product-level costs that need streaming or a compact backend-owned model.

## Fastest Measured Result By Scenario

| Scenario | Lines | View | Candidate | Diff ms | Serialize ms | Payload | Memory | Rows |
|---|---:|---|---|---:|---:|---:|---:|---:|
| added | 10 | Combined | LovelyGit Prototype | 0.003 | 0.016 | 528 | 5,083,136 | 10 |
| added | 10 | SideBySide | LovelyGit Prototype | 0.004 | 0.02 | 580 | 4,857,856 | 10 |
| added | 100 | Combined | LovelyGit Prototype | 0.002 | 0.022 | 3,409 | 12,877,824 | 100 |
| added | 100 | SideBySide | LovelyGit Prototype | 0.004 | 0.026 | 3,911 | 5,021,696 | 100 |
| added | 1,000 | Combined | LovelyGit Prototype | 0.004 | 0.076 | 33,110 | 4,866,048 | 1,000 |
| added | 1,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.077 | 38,112 | 4,890,624 | 1,000 |
| added | 10,000 | Combined | LovelyGit Prototype | 0.004 | 0.677 | 339,111 | 5,218,304 | 10,000 |
| added | 10,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.594 | 389,113 | 5,160,960 | 10,000 |
| added | 100,000 | Combined | LovelyGit Prototype | 0.003 | 4.799 | 3,489,112 | 79,011,840 | 100,000 |
| added | 100,000 | SideBySide | LovelyGit Prototype | 0.003 | 5.605 | 3,989,114 | 85,319,680 | 100,000 |
| added | 1,000,000 | Combined | LovelyGit Prototype | 0.003 | 49.562 | 35,889,113 | 541,589,504 | 1,000,000 |
| added | 1,000,000 | SideBySide | LovelyGit Prototype | 0.004 | 57.087 | 40,889,115 | 556,064,768 | 1,000,000 |
| chromium-actions-xml-edit | 57,018 | Combined | LovelyGit Prototype | 0.004 | 5.868 | 3,066,933 | 329,302,016 | 57,020 |
| chromium-actions-xml-edit | 57,018 | SideBySide | LovelyGit Prototype | 0.004 | 6.188 | 3,066,931 | 345,292,800 | 57,018 |
| chromium-cpp-simdutf-edit | 71,113 | Combined | LovelyGit Prototype | 0.003 | 7.859 | 4,198,754 | 323,100,672 | 71,115 |
| chromium-cpp-simdutf-edit | 71,113 | SideBySide | LovelyGit Prototype | 0.003 | 7.725 | 4,198,752 | 295,530,496 | 71,113 |
| chromium-gn-xnnpack-edit | 66,699 | Combined | LovelyGit Prototype | 0.004 | 5.537 | 3,277,890 | 299,921,408 | 66,701 |
| chromium-gn-xnnpack-edit | 66,699 | SideBySide | LovelyGit Prototype | 0.004 | 6.292 | 3,277,912 | 297,607,168 | 66,699 |
| chromium-header-normalization-edit | 138,073 | Combined | LovelyGit Prototype | 0.004 | 29.671 | 10,985,061 | 279,932,928 | 138,075 |
| chromium-header-normalization-edit | 138,073 | SideBySide | LovelyGit Prototype | 0.004 | 29.746 | 10,985,059 | 362,250,240 | 138,073 |
| chromium-js-pdf-worker-edit | 56,199 | Combined | LovelyGit Prototype | 0.006 | 5.531 | 2,648,280 | 324,874,240 | 56,201 |
| chromium-js-pdf-worker-edit | 56,199 | SideBySide | LovelyGit Prototype | 0.004 | 5.886 | 2,648,278 | 344,256,512 | 56,199 |
| chromium-json-manifest-edit | 922,640 | Combined | LovelyGit Prototype | 0.004 | 70.423 | 39,131,695 | 625,729,536 | 922,642 |
| chromium-json-manifest-edit | 922,640 | SideBySide | LovelyGit Prototype | 0.004 | 71.975 | 39,131,693 | 598,716,416 | 922,640 |
| chromium-luci-cfg-edit | 139,072 | Combined | LovelyGit Prototype | 0.004 | 12.524 | 8,288,522 | 297,762,816 | 139,074 |
| chromium-luci-cfg-edit | 139,072 | SideBySide | LovelyGit Prototype | 0.004 | 12.624 | 8,288,520 | 330,162,176 | 139,072 |
| chromium-xml-cdata-edit | 156,982 | Combined | LovelyGit Prototype | 0.004 | 15.362 | 14,726,082 | 332,308,480 | 156,984 |
| chromium-xml-cdata-edit | 156,982 | SideBySide | LovelyGit Prototype | 0.003 | 15.41 | 14,726,080 | 371,273,728 | 156,982 |
| deleted | 10 | Combined | LovelyGit Prototype | 0.004 | 0.016 | 518 | 5,349,376 | 10 |
| deleted | 10 | SideBySide | LovelyGit Prototype | 0.003 | 0.017 | 570 | 4,866,048 | 10 |
| deleted | 100 | Combined | LovelyGit Prototype | 0.003 | 0.022 | 3,309 | 5,353,472 | 100 |
| deleted | 100 | SideBySide | LovelyGit Prototype | 0.004 | 0.026 | 3,811 | 11,657,216 | 100 |
| deleted | 1,000 | Combined | LovelyGit Prototype | 0.003 | 0.069 | 32,110 | 11,698,176 | 1,000 |
| deleted | 1,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.08 | 37,112 | 4,866,048 | 1,000 |
| deleted | 10,000 | Combined | LovelyGit Prototype | 0.003 | 0.516 | 329,111 | 5,353,472 | 10,000 |
| deleted | 10,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.563 | 379,113 | 5,349,376 | 10,000 |
| deleted | 100,000 | Combined | LovelyGit Prototype | 0.004 | 5.28 | 3,389,112 | 83,484,672 | 100,000 |
| deleted | 100,000 | SideBySide | LovelyGit Prototype | 0.004 | 5.4 | 3,889,114 | 77,295,616 | 100,000 |
| deleted | 1,000,000 | Combined | LovelyGit Prototype | 0.004 | 50.25 | 34,889,113 | 549,773,312 | 1,000,000 |
| deleted | 1,000,000 | SideBySide | LovelyGit Prototype | 0.004 | 52.167 | 39,889,115 | 541,593,600 | 1,000,000 |
| lovelygit-billion-synthetic | 1,000,000,000 | Combined | LovelyGit Prototype | 0.003 | 0.009 | 107,777,777,998 | 5,095,424 | 1,000,000,000 |
| lovelygit-billion-synthetic | 1,000,000,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.008 | 107,777,778,000 | 5,373,952 | 1,000,000,000 |
| modified-bottom | 10 | Combined | LovelyGit Prototype | 0.004 | 0.018 | 520 | 5,349,376 | 11 |
| modified-bottom | 10 | SideBySide | LovelyGit Prototype | 0.003 | 0.018 | 508 | 4,890,624 | 10 |
| modified-bottom | 100 | Combined | LovelyGit Prototype | 0.003 | 0.027 | 3,132 | 4,866,048 | 101 |
| modified-bottom | 100 | SideBySide | LovelyGit Prototype | 0.003 | 0.026 | 3,120 | 5,283,840 | 100 |
| modified-bottom | 1,000 | Combined | LovelyGit Prototype | 0.004 | 0.112 | 31,034 | 5,353,472 | 1,001 |
| modified-bottom | 1,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.181 | 31,022 | 14,155,776 | 1,000 |
| modified-bottom | 10,000 | Combined | LovelyGit Prototype | 0.004 | 0.593 | 328,036 | 4,866,048 | 10,001 |
| modified-bottom | 10,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.584 | 328,024 | 5,349,376 | 10,000 |
| modified-bottom | 100,000 | Combined | LovelyGit Prototype | 0.003 | 5.849 | 3,478,038 | 77,283,328 | 100,001 |
| modified-bottom | 100,000 | SideBySide | LovelyGit Prototype | 0.003 | 6.037 | 3,478,026 | 77,316,096 | 100,000 |
| modified-bottom | 1,000,000 | Combined | LovelyGit Prototype | 0.004 | 60.013 | 36,778,040 | 547,463,168 | 1,000,001 |
| modified-bottom | 1,000,000 | SideBySide | LovelyGit Prototype | 0.004 | 59.727 | 36,778,028 | 555,999,232 | 1,000,000 |
| modified-middle | 10 | Combined | LovelyGit Prototype | 0.003 | 0.019 | 520 | 4,894,720 | 11 |
| modified-middle | 10 | SideBySide | LovelyGit Prototype | 0.004 | 0.018 | 508 | 4,866,048 | 10 |
| modified-middle | 100 | Combined | LovelyGit Prototype | 0.004 | 0.028 | 3,132 | 5,349,376 | 101 |
| modified-middle | 100 | SideBySide | LovelyGit Prototype | 0.004 | 0.027 | 3,120 | 8,011,776 | 100 |
| modified-middle | 1,000 | Combined | LovelyGit Prototype | 0.004 | 0.087 | 31,034 | 5,349,376 | 1,001 |
| modified-middle | 1,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.08 | 31,022 | 5,349,376 | 1,000 |
| modified-middle | 10,000 | Combined | LovelyGit Prototype | 0.003 | 0.6 | 328,036 | 5,353,472 | 10,001 |
| modified-middle | 10,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.807 | 328,024 | 5,349,376 | 10,000 |
| modified-middle | 100,000 | Combined | LovelyGit Prototype | 0.005 | 5.968 | 3,478,038 | 62,697,472 | 100,001 |
| modified-middle | 100,000 | SideBySide | LovelyGit Prototype | 0.003 | 6.3 | 3,478,026 | 77,352,960 | 100,000 |
| modified-middle | 1,000,000 | Combined | LovelyGit Prototype | 0.003 | 59.141 | 36,778,040 | 555,954,176 | 1,000,001 |
| modified-middle | 1,000,000 | SideBySide | LovelyGit Prototype | 0.003 | 58.284 | 36,778,028 | 547,090,432 | 1,000,000 |
| modified-top | 10 | Combined | LovelyGit Prototype | 0.004 | 0.018 | 517 | 4,866,048 | 11 |
| modified-top | 10 | SideBySide | LovelyGit Prototype | 0.004 | 0.019 | 505 | 13,254,656 | 10 |
| modified-top | 100 | Combined | LovelyGit Prototype | 0.004 | 0.029 | 3,129 | 10,117,120 | 101 |
| modified-top | 100 | SideBySide | LovelyGit Prototype | 0.004 | 0.043 | 3,117 | 13,344,768 | 100 |
| modified-top | 1,000 | Combined | LovelyGit Prototype | 0.004 | 0.081 | 31,031 | 5,349,376 | 1,001 |
| modified-top | 1,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.078 | 31,019 | 5,349,376 | 1,000 |
| modified-top | 10,000 | Combined | LovelyGit Prototype | 0.004 | 0.565 | 328,033 | 5,353,472 | 10,001 |
| modified-top | 10,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.603 | 328,021 | 4,866,048 | 10,000 |
| modified-top | 100,000 | Combined | LovelyGit Prototype | 0.004 | 5.695 | 3,478,035 | 64,131,072 | 100,001 |
| modified-top | 100,000 | SideBySide | LovelyGit Prototype | 0.004 | 5.828 | 3,478,023 | 67,796,992 | 100,000 |
| modified-top | 1,000,000 | Combined | LovelyGit Prototype | 0.004 | 58.713 | 36,778,037 | 541,569,024 | 1,000,001 |
| modified-top | 1,000,000 | SideBySide | LovelyGit Prototype | 0.004 | 58.576 | 36,778,025 | 537,448,448 | 1,000,000 |
| repeated | 10 | Combined | LovelyGit Prototype | 0.003 | 0.018 | 441 | 5,148,672 | 11 |
| repeated | 10 | SideBySide | LovelyGit Prototype | 0.002 | 0.016 | 429 | 5,427,200 | 10 |
| repeated | 100 | Combined | LovelyGit Prototype | 0.004 | 0.028 | 2,393 | 5,132,288 | 101 |
| repeated | 100 | SideBySide | LovelyGit Prototype | 0.006 | 0.037 | 2,381 | 4,866,048 | 100 |
| repeated | 1,000 | Combined | LovelyGit Prototype | 0.003 | 0.081 | 23,941 | 11,710,464 | 1,010 |
| repeated | 1,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.092 | 23,803 | 8,454,144 | 1,000 |
| repeated | 10,000 | Combined | LovelyGit Prototype | 0.004 | 0.582 | 257,493 | 5,353,472 | 10,100 |
| repeated | 10,000 | SideBySide | LovelyGit Prototype | 0.003 | 0.565 | 256,095 | 5,353,472 | 10,000 |
| repeated | 100,000 | Combined | LovelyGit Prototype | 0.004 | 5.895 | 2,773,895 | 72,384,512 | 101,000 |
| repeated | 100,000 | SideBySide | LovelyGit Prototype | 0.004 | 5.839 | 2,759,897 | 77,303,808 | 100,000 |
| repeated | 1,000,000 | Combined | LovelyGit Prototype | 0.004 | 61.699 | 29,746,897 | 552,681,472 | 1,010,000 |
| repeated | 1,000,000 | SideBySide | LovelyGit Prototype | 0.003 | 58.539 | 29,606,899 | 561,512,448 | 1,000,000 |
| unicode-modified | 10 | Combined | LovelyGit Prototype | 0.004 | 0.018 | 495 | 5,353,472 | 11 |
| unicode-modified | 10 | SideBySide | LovelyGit Prototype | 0.003 | 0.019 | 483 | 5,349,376 | 10 |
| unicode-modified | 100 | Combined | LovelyGit Prototype | 0.003 | 0.03 | 2,928 | 5,349,376 | 101 |
| unicode-modified | 100 | SideBySide | LovelyGit Prototype | 0.004 | 0.026 | 2,916 | 5,349,376 | 100 |
| unicode-modified | 1,000 | Combined | LovelyGit Prototype | 0.004 | 0.09 | 29,931 | 5,070,848 | 1,001 |
| unicode-modified | 1,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.092 | 29,919 | 5,349,376 | 1,000 |
| unicode-modified | 10,000 | Combined | LovelyGit Prototype | 0.004 | 0.65 | 326,934 | 21,381,120 | 10,001 |
| unicode-modified | 10,000 | SideBySide | LovelyGit Prototype | 0.004 | 0.639 | 326,922 | 5,349,376 | 10,000 |
| unicode-modified | 100,000 | Combined | LovelyGit Prototype | 0.004 | 6.724 | 3,566,937 | 83,410,944 | 100,001 |
| unicode-modified | 100,000 | SideBySide | LovelyGit Prototype | 0.003 | 6.581 | 3,566,925 | 70,041,600 | 100,000 |
| unicode-modified | 1,000,000 | Combined | LovelyGit Prototype | 0.004 | 65.903 | 38,666,940 | 551,755,776 | 1,000,001 |
| unicode-modified | 1,000,000 | SideBySide | LovelyGit Prototype | 0.003 | 66.519 | 38,666,928 | 547,573,760 | 1,000,000 |

## Scenario: added

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 10 | Combined | Measured | 0.073 | 0.378 | 468 | 5,296,128 | 1 | new file |
| CSharpDiff | 10 | SideBySide | Measured | 0.098 | 0.383 | 468 | 5,292,032 | 1 | new file |
| CSharpDiff | 100 | Combined | Measured | 0.18 | 0.354 | 2,178 | 5,296,128 | 1 | new file |
| CSharpDiff | 100 | SideBySide | Measured | 0.181 | 0.326 | 2,178 | 5,300,224 | 1 | new file |
| CSharpDiff | 1,000 | Combined | Measured | 6.899 | 0.416 | 19,278 | 4,812,800 | 1 | new file |
| CSharpDiff | 1,000 | SideBySide | Measured | 6.655 | 0.448 | 19,278 | 5,296,128 | 1 | new file |
| CSharpDiff | 10,000 | Combined | Measured | 628.181 | 1.716 | 190,278 | 22,552,576 | 1 | new file |
| CSharpDiff | 10,000 | SideBySide | Measured | 617.331 | 1.419 | 190,278 | 22,532,096 | 1 | new file |
| CSharpDiff | 100,000 | Combined | ReusedSlow |  |  |  | 63,352,832 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 100,000 | SideBySide | ReusedSlow |  |  |  | 63,021,056 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 1,000,000 | Combined | ReusedSlow |  |  |  | 515,895,296 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 1,000,000 | SideBySide | ReusedSlow |  |  |  | 484,646,912 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| Diff4Net | 10 | Combined | Measured | 0.025 | 0.429 | 1,394 | 5,296,128 | 10 | new file |
| Diff4Net | 10 | SideBySide | Measured | 0.034 | 0.342 | 1,394 | 5,296,128 | 10 | new file |
| Diff4Net | 100 | Combined | Measured | 0.045 | 0.463 | 12,465 | 5,296,128 | 100 | new file |
| Diff4Net | 100 | SideBySide | Measured | 0.038 | 0.354 | 12,465 | 5,296,128 | 100 | new file |
| Diff4Net | 1,000 | Combined | Measured | 0.167 | 0.744 | 124,066 | 5,296,128 | 1,000 | new file |
| Diff4Net | 1,000 | SideBySide | Measured | 0.16 | 0.541 | 124,066 | 5,296,128 | 1,000 | new file |
| Diff4Net | 10,000 | Combined | Measured | 1.383 | 2.573 | 1,249,067 | 8,172,096 | 10,000 | new file |
| Diff4Net | 10,000 | SideBySide | Measured | 1.315 | 2.584 | 1,249,067 | 8,170,008 | 10,000 | new file |
| Diff4Net | 100,000 | Combined | Measured | 18.75 | 19.771 | 12,589,068 | 72,653,000 | 100,000 | new file |
| Diff4Net | 100,000 | SideBySide | Measured | 18.131 | 19.383 | 12,589,068 | 72,653,000 | 100,000 | new file |
| Diff4Net | 1,000,000 | Combined | Measured | 284.333 | 186.946 | 126,889,069 | 901,517,312 | 1,000,000 | new file |
| Diff4Net | 1,000,000 | SideBySide | Measured | 273.58 | 181.043 | 126,889,069 | 723,492,864 | 1,000,000 | new file |
| DiffMatchPatch | 10 | Combined | Measured | 1.862 | 0.343 | 1,400 | 4,812,800 | 10 | new file |
| DiffMatchPatch | 10 | SideBySide | Measured | 1.142 | 0.317 | 1,400 | 5,296,128 | 10 | new file |
| DiffMatchPatch | 100 | Combined | Measured | 1.183 | 0.319 | 12,471 | 5,300,224 | 100 | new file |
| DiffMatchPatch | 100 | SideBySide | Measured | 1.48 | 0.391 | 12,471 | 5,296,128 | 100 | new file |
| DiffMatchPatch | 1,000 | Combined | Measured | 1.877 | 0.582 | 124,072 | 5,300,224 | 1,000 | new file |
| DiffMatchPatch | 1,000 | SideBySide | Measured | 1.251 | 0.778 | 124,072 | 5,296,128 | 1,000 | new file |
| DiffMatchPatch | 10,000 | Combined | Measured | 1.958 | 2.506 | 1,249,073 | 8,184,560 | 10,000 | new file |
| DiffMatchPatch | 10,000 | SideBySide | Measured | 1.928 | 2.415 | 1,249,073 | 8,184,560 | 10,000 | new file |
| DiffMatchPatch | 100,000 | Combined | Measured | 6.454 | 19.723 | 12,589,074 | 75,233,480 | 100,000 | new file |
| DiffMatchPatch | 100,000 | SideBySide | Measured | 6.321 | 19.116 | 12,589,074 | 75,233,480 | 100,000 | new file |
| DiffMatchPatch | 1,000,000 | Combined | Measured | 159.203 | 196.062 | 126,889,075 | 793,788,416 | 1,000,000 | new file |
| DiffMatchPatch | 1,000,000 | SideBySide | Measured | 157.68 | 183.499 | 126,889,075 | 913,969,152 | 1,000,000 | new file |
| DiffPlex | 10 | Combined | Measured | 0.059 | 0.336 | 1,394 | 5,300,224 | 10 | new file |
| DiffPlex | 10 | SideBySide | Measured | 0.041 | 0.447 | 1,394 | 5,271,552 | 10 | new file |
| DiffPlex | 100 | Combined | Measured | 0.082 | 0.358 | 12,465 | 5,296,128 | 100 | new file |
| DiffPlex | 100 | SideBySide | Measured | 0.054 | 0.367 | 12,465 | 5,300,224 | 100 | new file |
| DiffPlex | 1,000 | Combined | Measured | 0.405 | 0.531 | 124,066 | 5,292,032 | 1,000 | new file |
| DiffPlex | 1,000 | SideBySide | Measured | 0.404 | 0.597 | 124,066 | 4,812,800 | 1,000 | new file |
| DiffPlex | 10,000 | Combined | Measured | 2.353 | 2.797 | 1,249,067 | 8,370,128 | 10,000 | new file |
| DiffPlex | 10,000 | SideBySide | Measured | 2.443 | 2.877 | 1,249,067 | 8,374,448 | 10,000 | new file |
| DiffPlex | 100,000 | Combined | Measured | 23.565 | 18.732 | 12,589,068 | 73,806,248 | 100,000 | new file |
| DiffPlex | 100,000 | SideBySide | Measured | 34.913 | 19.514 | 12,589,068 | 78,512,584 | 100,000 | new file |
| DiffPlex | 1,000,000 | Combined | Measured | 438.268 | 189.629 | 126,889,069 | 821,682,176 | 1,000,000 | new file |
| DiffPlex | 1,000,000 | SideBySide | Measured | 474.192 | 192.99 | 126,889,069 | 995,241,984 | 1,000,000 | new file |
| Git CLI | 10 | Combined | Measured | 65.545 | 0.365 | 1,402 | 12,832,768 | 12 | new file |
| Git CLI | 10 | SideBySide | Measured | 48.377 | 0.374 | 1,378 | 13,246,464 | 12 | new file |
| Git CLI | 100 | Combined | Measured | 45.748 | 0.353 | 10,768 | 13,004,800 | 102 | new file |
| Git CLI | 100 | SideBySide | Measured | 75.926 | 0.355 | 10,564 | 12,935,168 | 102 | new file |
| Git CLI | 1,000 | Combined | Measured | 60.087 | 0.502 | 106,174 | 13,598,720 | 1,002 | new file |
| Git CLI | 1,000 | SideBySide | Measured | 48.614 | 0.521 | 104,170 | 13,586,432 | 1,002 | new file |
| Git CLI | 10,000 | Combined | Measured | 53.617 | 2.425 | 1,078,180 | 16,576,512 | 10,002 | new file |
| Git CLI | 10,000 | SideBySide | Measured | 49.989 | 2.536 | 1,058,176 | 16,568,320 | 10,002 | new file |
| Git CLI | 100,000 | Combined | Measured | 82.105 | 17.77 | 10,978,186 | 63,829,632 | 100,002 | new file |
| Git CLI | 100,000 | SideBySide | Measured | 98.605 | 18.972 | 10,778,182 | 101,601,280 | 100,002 | new file |
| Git CLI | 1,000,000 | Combined | Measured | 488.926 | 170.441 | 111,778,192 | 800,354,304 | 1,000,002 | new file |
| Git CLI | 1,000,000 | SideBySide | Measured | 503.113 | 176.455 | 109,778,188 | 695,865,344 | 1,000,002 | new file |
| LovelyGit Prototype | 10 | Combined | Measured | 0.003 | 0.016 | 528 | 5,083,136 | 10 | new file |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.004 | 0.02 | 580 | 4,857,856 | 10 | new file |
| LovelyGit Prototype | 100 | Combined | Measured | 0.002 | 0.022 | 3,409 | 12,877,824 | 100 | new file |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.004 | 0.026 | 3,911 | 5,021,696 | 100 | new file |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.004 | 0.076 | 33,110 | 4,866,048 | 1,000 | new file |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.004 | 0.077 | 38,112 | 4,890,624 | 1,000 | new file |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.004 | 0.677 | 339,111 | 5,218,304 | 10,000 | new file |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.003 | 0.594 | 389,113 | 5,160,960 | 10,000 | new file |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.003 | 4.799 | 3,489,112 | 79,011,840 | 100,000 | new file |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.003 | 5.605 | 3,989,114 | 85,319,680 | 100,000 | new file |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.003 | 49.562 | 35,889,113 | 541,589,504 | 1,000,000 | new file |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.004 | 57.087 | 40,889,115 | 556,064,768 | 1,000,000 | new file |
| MyersDiff | 10 | Combined | Measured | 0.015 | 0.366 | 1,395 | 5,296,128 | 10 | new file |
| MyersDiff | 10 | SideBySide | Measured | 0.02 | 0.35 | 1,395 | 5,296,128 | 10 | new file |
| MyersDiff | 100 | Combined | Measured | 0.025 | 0.377 | 12,466 | 5,300,224 | 100 | new file |
| MyersDiff | 100 | SideBySide | Measured | 0.024 | 0.354 | 12,466 | 5,292,032 | 100 | new file |
| MyersDiff | 1,000 | Combined | Measured | 0.116 | 0.592 | 124,067 | 5,296,128 | 1,000 | new file |
| MyersDiff | 1,000 | SideBySide | Measured | 0.121 | 0.583 | 124,067 | 5,296,128 | 1,000 | new file |
| MyersDiff | 10,000 | Combined | Measured | 1.021 | 2.561 | 1,249,068 | 8,172,048 | 10,000 | new file |
| MyersDiff | 10,000 | SideBySide | Measured | 1.018 | 2.761 | 1,249,068 | 8,172,048 | 10,000 | new file |
| MyersDiff | 100,000 | Combined | Measured | 11.224 | 19.548 | 12,589,069 | 72,649,800 | 100,000 | new file |
| MyersDiff | 100,000 | SideBySide | Measured | 11.011 | 19.811 | 12,589,069 | 72,649,800 | 100,000 | new file |
| MyersDiff | 1,000,000 | Combined | Measured | 197.688 | 182.129 | 126,889,070 | 805,023,744 | 1,000,000 | new file |
| MyersDiff | 1,000,000 | SideBySide | Measured | 198.635 | 188.969 | 126,889,070 | 743,555,072 | 1,000,000 | new file |
| NGitDiff Histogram | 10 | Combined | Measured | 0.058 | 0.344 | 1,404 | 5,296,128 | 10 | new file |
| NGitDiff Histogram | 10 | SideBySide | Measured | 0.057 | 0.366 | 1,404 | 4,812,800 | 10 | new file |
| NGitDiff Histogram | 100 | Combined | Measured | 0.072 | 0.361 | 12,475 | 5,296,128 | 100 | new file |
| NGitDiff Histogram | 100 | SideBySide | Measured | 0.074 | 0.382 | 12,475 | 5,300,224 | 100 | new file |
| NGitDiff Histogram | 1,000 | Combined | Measured | 0.208 | 0.544 | 124,076 | 5,300,224 | 1,000 | new file |
| NGitDiff Histogram | 1,000 | SideBySide | Measured | 0.207 | 0.535 | 124,076 | 5,300,224 | 1,000 | new file |
| NGitDiff Histogram | 10,000 | Combined | Measured | 1.649 | 2.549 | 1,249,077 | 10,736,832 | 10,000 | new file |
| NGitDiff Histogram | 10,000 | SideBySide | Measured | 1.679 | 3.084 | 1,249,077 | 10,425,368 | 10,000 | new file |
| NGitDiff Histogram | 100,000 | Combined | Measured | 17.568 | 19.526 | 12,589,078 | 71,602,440 | 100,000 | new file |
| NGitDiff Histogram | 100,000 | SideBySide | Measured | 17.209 | 19.122 | 12,589,078 | 71,602,440 | 100,000 | new file |
| NGitDiff Histogram | 1,000,000 | Combined | Measured | 315.182 | 189.453 | 126,889,079 | 802,959,360 | 1,000,000 | new file |
| NGitDiff Histogram | 1,000,000 | SideBySide | Measured | 302.613 | 184.885 | 126,889,079 | 818,671,616 | 1,000,000 | new file |
| NGitDiff Myers | 10 | Combined | Measured | 0.057 | 0.374 | 1,400 | 4,812,800 | 10 | new file |
| NGitDiff Myers | 10 | SideBySide | Measured | 0.068 | 0.365 | 1,400 | 5,296,128 | 10 | new file |
| NGitDiff Myers | 100 | Combined | Measured | 0.072 | 0.381 | 12,471 | 5,296,128 | 100 | new file |
| NGitDiff Myers | 100 | SideBySide | Measured | 0.077 | 0.354 | 12,471 | 4,812,800 | 100 | new file |
| NGitDiff Myers | 1,000 | Combined | Measured | 0.206 | 0.539 | 124,072 | 5,296,128 | 1,000 | new file |
| NGitDiff Myers | 1,000 | SideBySide | Measured | 0.213 | 0.558 | 124,072 | 5,296,128 | 1,000 | new file |
| NGitDiff Myers | 10,000 | Combined | Measured | 1.596 | 2.564 | 1,249,073 | 8,044,528 | 10,000 | new file |
| NGitDiff Myers | 10,000 | SideBySide | Measured | 1.576 | 2.776 | 1,249,073 | 8,041,984 | 10,000 | new file |
| NGitDiff Myers | 100,000 | Combined | Measured | 15.181 | 19.212 | 12,589,074 | 71,602,344 | 100,000 | new file |
| NGitDiff Myers | 100,000 | SideBySide | Measured | 17.207 | 19.62 | 12,589,074 | 71,602,344 | 100,000 | new file |
| NGitDiff Myers | 1,000,000 | Combined | Measured | 312.909 | 197.622 | 126,889,075 | 927,952,896 | 1,000,000 | new file |
| NGitDiff Myers | 1,000,000 | SideBySide | Measured | 309.433 | 191.95 | 126,889,075 | 823,005,184 | 1,000,000 | new file |
| spkl.Diffs | 10 | Combined | Measured | 0.023 | 0.451 | 1,396 | 5,296,128 | 10 | new file |
| spkl.Diffs | 10 | SideBySide | Measured | 0.022 | 0.4 | 1,396 | 4,952,064 | 10 | new file |
| spkl.Diffs | 100 | Combined | Measured | 0.024 | 0.388 | 12,467 | 5,296,128 | 100 | new file |
| spkl.Diffs | 100 | SideBySide | Measured | 0.025 | 0.371 | 12,467 | 5,296,128 | 100 | new file |
| spkl.Diffs | 1,000 | Combined | Measured | 0.113 | 0.568 | 124,068 | 5,296,128 | 1,000 | new file |
| spkl.Diffs | 1,000 | SideBySide | Measured | 0.119 | 0.57 | 124,068 | 5,296,128 | 1,000 | new file |
| spkl.Diffs | 10,000 | Combined | Measured | 0.87 | 2.514 | 1,249,069 | 8,172,048 | 10,000 | new file |
| spkl.Diffs | 10,000 | SideBySide | Measured | 0.916 | 2.638 | 1,249,069 | 8,172,048 | 10,000 | new file |
| spkl.Diffs | 100,000 | Combined | Measured | 6.425 | 20.74 | 12,589,070 | 75,261,728 | 100,000 | new file |
| spkl.Diffs | 100,000 | SideBySide | Measured | 6.497 | 19.65 | 12,589,070 | 75,261,728 | 100,000 | new file |
| spkl.Diffs | 1,000,000 | Combined | Measured | 186.545 | 194.056 | 126,889,071 | 712,859,648 | 1,000,000 | new file |
| spkl.Diffs | 1,000,000 | SideBySide | Measured | 186.69 | 187.365 | 126,889,071 | 842,080,256 | 1,000,000 | new file |

## Scenario: chromium-actions-xml-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 57,018 | Combined | Measured | 32.082 | 5 | 2,762,445 | 323,178,496 | 8 | real Chromium file: tools\metrics\actions\actions.xml |
| CSharpDiff | 57,018 | SideBySide | Measured | 31.283 | 7.915 | 5,523,748 | 311,848,960 | 8 | real Chromium file: tools\metrics\actions\actions.xml |
| Diff4Net | 57,018 | Combined | Measured | 15.478 | 16.597 | 8,954,573 | 311,988,224 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| Diff4Net | 57,018 | SideBySide | Measured | 14.766 | 23.834 | 11,487,822 | 326,230,016 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| DiffMatchPatch | 57,018 | Combined | Measured | 15.394 | 15.207 | 8,955,132 | 296,738,816 | 57,025 | real Chromium file: tools\metrics\actions\actions.xml |
| DiffMatchPatch | 57,018 | SideBySide | Measured | 26.943 | 21.965 | 11,488,373 | 311,861,248 | 57,025 | real Chromium file: tools\metrics\actions\actions.xml |
| DiffPlex | 57,018 | Combined | Measured | 19.321 | 16.757 | 8,954,573 | 311,767,040 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| DiffPlex | 57,018 | SideBySide | Measured | 20.442 | 23.523 | 11,487,714 | 322,904,064 | 57,019 | real Chromium file: tools\metrics\actions\actions.xml |
| Git CLI | 57,018 | Combined | Measured | 71.987 | 0.307 | 2,650 | 294,383,616 | 24 | real Chromium file: tools\metrics\actions\actions.xml |
| Git CLI | 57,018 | SideBySide | Measured | 77.591 | 0.3 | 2,602 | 294,301,696 | 24 | real Chromium file: tools\metrics\actions\actions.xml |
| LovelyGit Prototype | 57,018 | Combined | Measured | 0.004 | 5.868 | 3,066,933 | 329,302,016 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| LovelyGit Prototype | 57,018 | SideBySide | Measured | 0.004 | 6.188 | 3,066,931 | 345,292,800 | 57,018 | real Chromium file: tools\metrics\actions\actions.xml |
| MyersDiff | 57,018 | Combined | Measured | 10.47 | 16.293 | 8,954,574 | 355,565,568 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| MyersDiff | 57,018 | SideBySide | Measured | 9.835 | 23.372 | 11,487,823 | 322,449,408 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| NGitDiff Histogram | 57,018 | Combined | Measured | 26.01 | 17.038 | 8,954,583 | 297,218,048 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| NGitDiff Histogram | 57,018 | SideBySide | Measured | 25.401 | 23.584 | 11,487,832 | 322,834,432 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| NGitDiff Myers | 57,018 | Combined | Measured | 26.566 | 19.053 | 8,954,579 | 306,008,064 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| NGitDiff Myers | 57,018 | SideBySide | Measured | 27.344 | 26.071 | 11,487,828 | 308,015,104 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| spkl.Diffs | 57,018 | Combined | Measured | 6.905 | 17.345 | 8,954,575 | 333,631,488 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |
| spkl.Diffs | 57,018 | SideBySide | Measured | 7.093 | 24.095 | 11,487,824 | 331,108,352 | 57,020 | real Chromium file: tools\metrics\actions\actions.xml |

## Scenario: chromium-cpp-simdutf-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 71,113 | Combined | Measured | 48.849 | 4.398 | 3,197,975 | 342,659,072 | 8 | real Chromium file: third_party\simdutf\simdutf.cpp |
| CSharpDiff | 71,113 | SideBySide | Measured | 56.622 | 9.189 | 6,394,763 | 341,835,776 | 8 | real Chromium file: third_party\simdutf\simdutf.cpp |
| Diff4Net | 71,113 | Combined | Measured | 14.71 | 14.978 | 10,926,458 | 346,636,288 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| Diff4Net | 71,113 | SideBySide | Measured | 14.884 | 18.379 | 13,838,812 | 341,430,272 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| DiffMatchPatch | 71,113 | Combined | Measured | 21.489 | 16.236 | 10,926,982 | 325,611,520 | 71,120 | real Chromium file: third_party\simdutf\simdutf.cpp |
| DiffMatchPatch | 71,113 | SideBySide | Measured | 22.271 | 22.655 | 13,839,363 | 338,292,736 | 71,120 | real Chromium file: third_party\simdutf\simdutf.cpp |
| DiffPlex | 71,113 | Combined | Measured | 26.012 | 15.088 | 10,926,458 | 338,673,664 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| DiffPlex | 71,113 | SideBySide | Measured | 30.947 | 19.365 | 13,838,704 | 333,647,872 | 71,114 | real Chromium file: third_party\simdutf\simdutf.cpp |
| Git CLI | 71,113 | Combined | Measured | 84.631 | 0.308 | 2,650 | 338,644,992 | 24 | real Chromium file: third_party\simdutf\simdutf.cpp |
| Git CLI | 71,113 | SideBySide | Measured | 83.198 | 0.311 | 2,602 | 338,063,360 | 24 | real Chromium file: third_party\simdutf\simdutf.cpp |
| LovelyGit Prototype | 71,113 | Combined | Measured | 0.003 | 7.859 | 4,198,754 | 323,100,672 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| LovelyGit Prototype | 71,113 | SideBySide | Measured | 0.003 | 7.725 | 4,198,752 | 295,530,496 | 71,113 | real Chromium file: third_party\simdutf\simdutf.cpp |
| MyersDiff | 71,113 | Combined | Measured | 13.199 | 15.431 | 10,926,459 | 346,660,864 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| MyersDiff | 71,113 | SideBySide | Measured | 12.279 | 17.821 | 13,838,813 | 345,706,496 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| NGitDiff Histogram | 71,113 | Combined | Measured | 51.493 | 18.674 | 10,926,468 | 348,327,936 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| NGitDiff Histogram | 71,113 | SideBySide | Measured | 47.21 | 18.849 | 13,838,822 | 343,633,920 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| NGitDiff Myers | 71,113 | Combined | Measured | 50.965 | 15.555 | 10,926,464 | 342,392,832 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| NGitDiff Myers | 71,113 | SideBySide | Measured | 44.326 | 18.465 | 13,838,818 | 341,872,640 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| spkl.Diffs | 71,113 | Combined | Measured | 13.319 | 14.324 | 10,926,460 | 337,842,176 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |
| spkl.Diffs | 71,113 | SideBySide | Measured | 13.919 | 18.449 | 13,838,814 | 350,199,808 | 71,115 | real Chromium file: third_party\simdutf\simdutf.cpp |

## Scenario: chromium-gn-xnnpack-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 66,699 | Combined | Measured | 46.046 | 4.29 | 2,490,079 | 308,342,784 | 9 | real Chromium file: third_party\xnnpack\BUILD.gn |
| CSharpDiff | 66,699 | SideBySide | Measured | 46.436 | 8.09 | 4,978,779 | 307,458,048 | 9 | real Chromium file: third_party\xnnpack\BUILD.gn |
| Diff4Net | 66,699 | Combined | Measured | 15.557 | 16.5 | 9,737,333 | 307,990,528 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| Diff4Net | 66,699 | SideBySide | Measured | 15.915 | 20.402 | 11,959,257 | 308,211,712 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| DiffMatchPatch | 66,699 | Combined | Measured | 11.912 | 15.74 | 9,737,813 | 308,060,160 | 66,706 | real Chromium file: third_party\xnnpack\BUILD.gn |
| DiffMatchPatch | 66,699 | SideBySide | Measured | 12.191 | 20.018 | 11,959,808 | 315,883,520 | 66,706 | real Chromium file: third_party\xnnpack\BUILD.gn |
| DiffPlex | 66,699 | Combined | Measured | 19.774 | 16.186 | 9,737,333 | 307,482,624 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| DiffPlex | 66,699 | SideBySide | Measured | 21.485 | 21.073 | 11,959,149 | 307,470,336 | 66,700 | real Chromium file: third_party\xnnpack\BUILD.gn |
| Git CLI | 66,699 | Combined | Measured | 75.969 | 0.285 | 2,650 | 308,465,664 | 24 | real Chromium file: third_party\xnnpack\BUILD.gn |
| Git CLI | 66,699 | SideBySide | Measured | 69.951 | 0.302 | 2,602 | 307,478,528 | 24 | real Chromium file: third_party\xnnpack\BUILD.gn |
| LovelyGit Prototype | 66,699 | Combined | Measured | 0.004 | 5.537 | 3,277,890 | 299,921,408 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| LovelyGit Prototype | 66,699 | SideBySide | Measured | 0.004 | 6.292 | 3,277,912 | 297,607,168 | 66,699 | real Chromium file: third_party\xnnpack\BUILD.gn |
| MyersDiff | 66,699 | Combined | Measured | 10.33 | 16.318 | 9,737,334 | 313,757,696 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| MyersDiff | 66,699 | SideBySide | Measured | 10.542 | 20.365 | 11,959,258 | 311,328,768 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| NGitDiff Histogram | 66,699 | Combined | Measured | 43.057 | 17.385 | 9,737,343 | 345,796,608 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| NGitDiff Histogram | 66,699 | SideBySide | Measured | 40.846 | 20.19 | 11,959,267 | 308,006,912 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| NGitDiff Myers | 66,699 | Combined | Measured | 37.753 | 17.034 | 9,737,339 | 336,683,008 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| NGitDiff Myers | 66,699 | SideBySide | Measured | 40.224 | 21.731 | 11,959,263 | 319,983,616 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| spkl.Diffs | 66,699 | Combined | Measured | 11.279 | 16.638 | 9,737,335 | 308,228,096 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |
| spkl.Diffs | 66,699 | SideBySide | Measured | 11.821 | 21.685 | 11,959,259 | 327,524,352 | 66,701 | real Chromium file: third_party\xnnpack\BUILD.gn |

## Scenario: chromium-header-normalization-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 138,073 | Combined | Measured | 129.476 | 15.838 | 9,364,272 | 314,601,472 | 8 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| CSharpDiff | 138,073 | SideBySide | Measured | 139.871 | 28.799 | 18,727,391 | 298,835,968 | 8 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| Diff4Net | 138,073 | Combined | Measured | 70.595 | 36.216 | 24,467,543 | 314,535,936 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| Diff4Net | 138,073 | SideBySide | Measured | 53.815 | 50.505 | 33,278,388 | 333,463,552 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| DiffMatchPatch | 138,073 | Combined | Measured | 61.396 | 39.314 | 24,468,111 | 315,187,200 | 138,080 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| DiffMatchPatch | 138,073 | SideBySide | Measured | 82.597 | 53.055 | 33,278,948 | 366,866,432 | 138,080 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| DiffPlex | 138,073 | Combined | Measured | 82.151 | 33.927 | 24,467,543 | 334,462,976 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| DiffPlex | 138,073 | SideBySide | Measured | 107.103 | 48.371 | 33,278,280 | 293,335,040 | 138,074 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| Git CLI | 138,073 | Combined | Measured | 147.192 | 0.321 | 2,650 | 286,339,072 | 24 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| Git CLI | 138,073 | SideBySide | Measured | 136.419 | 0.298 | 2,602 | 293,859,328 | 24 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| LovelyGit Prototype | 138,073 | Combined | Measured | 0.004 | 29.671 | 10,985,061 | 279,932,928 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| LovelyGit Prototype | 138,073 | SideBySide | Measured | 0.004 | 29.746 | 10,985,059 | 362,250,240 | 138,073 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| MyersDiff | 138,073 | Combined | Measured | 24.576 | 36.021 | 24,467,544 | 334,688,256 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| MyersDiff | 138,073 | SideBySide | Measured | 25.528 | 45.072 | 33,278,389 | 299,659,264 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| NGitDiff Histogram | 138,073 | Combined | Measured | 115.228 | 37.763 | 24,467,553 | 311,414,784 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| NGitDiff Histogram | 138,073 | SideBySide | Measured | 90.906 | 48.32 | 33,278,398 | 344,502,272 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| NGitDiff Myers | 138,073 | Combined | Measured | 106.224 | 34.769 | 24,467,549 | 316,653,568 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| NGitDiff Myers | 138,073 | SideBySide | Measured | 105.217 | 46.664 | 33,278,394 | 310,034,432 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| spkl.Diffs | 138,073 | Combined | Measured | 29.475 | 34.141 | 24,467,545 | 356,261,888 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |
| spkl.Diffs | 138,073 | SideBySide | Measured | 26.257 | 47.42 | 33,278,390 | 333,869,056 | 138,075 | real Chromium file: third_party\sentencepiece\src\src\normalization_rule.h |

## Scenario: chromium-js-pdf-worker-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 56,199 | Combined | Measured | 31.681 | 3.901 | 1,958,806 | 322,695,168 | 8 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| CSharpDiff | 56,199 | SideBySide | Measured | 32.062 | 6.558 | 3,916,483 | 300,679,168 | 8 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| Diff4Net | 56,199 | Combined | Measured | 15.91 | 13.387 | 8,061,663 | 291,155,968 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| Diff4Net | 56,199 | SideBySide | Measured | 15.078 | 16.721 | 9,794,562 | 345,927,680 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| DiffMatchPatch | 56,199 | Combined | Measured | 12.744 | 13.687 | 8,062,220 | 321,531,904 | 56,206 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| DiffMatchPatch | 56,199 | SideBySide | Measured | 13.859 | 17.647 | 9,795,113 | 311,361,536 | 56,206 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| DiffPlex | 56,199 | Combined | Measured | 18.779 | 13.301 | 8,061,663 | 288,989,184 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| DiffPlex | 56,199 | SideBySide | Measured | 19.861 | 17.271 | 9,794,454 | 322,777,088 | 56,200 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| Git CLI | 56,199 | Combined | Measured | 68.527 | 0.282 | 2,650 | 319,877,120 | 24 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| Git CLI | 56,199 | SideBySide | Measured | 69.824 | 0.304 | 2,602 | 322,424,832 | 24 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| LovelyGit Prototype | 56,199 | Combined | Measured | 0.006 | 5.531 | 2,648,280 | 324,874,240 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| LovelyGit Prototype | 56,199 | SideBySide | Measured | 0.004 | 5.886 | 2,648,278 | 344,256,512 | 56,199 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| MyersDiff | 56,199 | Combined | Measured | 6.44 | 13.446 | 8,061,664 | 300,613,632 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| MyersDiff | 56,199 | SideBySide | Measured | 6.634 | 17.551 | 9,794,563 | 328,343,552 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| NGitDiff Histogram | 56,199 | Combined | Measured | 24.058 | 13.205 | 8,061,673 | 318,201,856 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| NGitDiff Histogram | 56,199 | SideBySide | Measured | 23.314 | 15.836 | 9,794,572 | 318,627,840 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| NGitDiff Myers | 56,199 | Combined | Measured | 24.741 | 13.737 | 8,061,669 | 328,278,016 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| NGitDiff Myers | 56,199 | SideBySide | Measured | 25.252 | 15.68 | 9,794,568 | 351,997,952 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| spkl.Diffs | 56,199 | Combined | Measured | 7.062 | 12.1 | 8,061,665 | 340,824,064 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |
| spkl.Diffs | 56,199 | SideBySide | Measured | 6.781 | 15.937 | 9,794,564 | 334,000,128 | 56,201 | real Chromium file: third_party\wpt_tools\wpt\tools\third_party\pdf_js\pdf.worker.js |

## Scenario: chromium-json-manifest-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 922,640 | Combined | Measured | 766.836 | 41.924 | 24,575,799 | 612,073,472 | 8 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| CSharpDiff | 922,640 | SideBySide | Measured | 817.782 | 85.984 | 49,150,414 | 630,673,408 | 8 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| Diff4Net | 922,640 | Combined | Measured | 412.609 | 213.489 | 126,766,007 | 910,274,560 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| Diff4Net | 922,640 | SideBySide | Measured | 397.841 | 255.282 | 147,650,080 | 1,168,703,488 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| DiffMatchPatch | 922,640 | Combined | Measured | 354.746 | 204.3 | 126,766,546 | 1,012,625,408 | 922,647 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| DiffMatchPatch | 922,640 | SideBySide | Measured | 328.046 | 261.254 | 147,650,640 | 1,256,112,128 | 922,647 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| DiffPlex | 922,640 | Combined | Measured | 461.23 | 210.023 | 126,766,007 | 1,033,641,984 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| DiffPlex | 922,640 | SideBySide | Measured | 553.098 | 273.049 | 147,649,972 | 1,257,021,440 | 922,641 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| Git CLI | 922,640 | Combined | Measured | 335.991 | 0.319 | 2,650 | 612,421,632 | 24 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| Git CLI | 922,640 | SideBySide | Measured | 332.791 | 0.307 | 2,602 | 607,350,784 | 24 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| LovelyGit Prototype | 922,640 | Combined | Measured | 0.004 | 70.423 | 39,131,695 | 625,729,536 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| LovelyGit Prototype | 922,640 | SideBySide | Measured | 0.004 | 71.975 | 39,131,693 | 598,716,416 | 922,640 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| MyersDiff | 922,640 | Combined | Measured | 323.437 | 202.947 | 126,766,008 | 1,126,014,976 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| MyersDiff | 922,640 | SideBySide | Measured | 332.152 | 280.508 | 147,650,081 | 1,235,070,976 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| NGitDiff Histogram | 922,640 | Combined | Measured | 656.24 | 199.419 | 126,766,017 | 1,095,811,072 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| NGitDiff Histogram | 922,640 | SideBySide | Measured | 642.524 | 267.929 | 147,650,090 | 1,080,688,640 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| NGitDiff Myers | 922,640 | Combined | Measured | 659.319 | 203.73 | 126,766,013 | 997,552,128 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| NGitDiff Myers | 922,640 | SideBySide | Measured | 642.801 | 268.938 | 147,650,086 | 1,185,234,944 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| spkl.Diffs | 922,640 | Combined | Measured | 294.204 | 207.074 | 126,766,009 | 922,894,336 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |
| spkl.Diffs | 922,640 | SideBySide | Measured | 286.679 | 280.771 | 147,650,082 | 1,095,864,320 | 922,642 | real Chromium file: third_party\blink\web_tests\external\WPT_BASE_MANIFEST_8.json |

## Scenario: chromium-luci-cfg-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 139,072 | Combined | Measured | 131.064 | 14.548 | 7,116,718 | 305,463,296 | 8 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| CSharpDiff | 139,072 | SideBySide | Measured | 126.395 | 26.971 | 14,232,224 | 293,396,480 | 8 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| Diff4Net | 139,072 | Combined | Measured | 71.42 | 35.386 | 22,330,878 | 313,372,672 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| Diff4Net | 139,072 | SideBySide | Measured | 69.806 | 45.927 | 28,890,114 | 334,794,752 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| DiffMatchPatch | 139,072 | Combined | Measured | 81.404 | 35.497 | 22,331,446 | 314,236,928 | 139,079 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| DiffMatchPatch | 139,072 | SideBySide | Measured | 82.028 | 46.342 | 28,890,674 | 334,839,808 | 139,079 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| DiffPlex | 139,072 | Combined | Measured | 63.396 | 35.076 | 22,330,878 | 299,372,544 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| DiffPlex | 139,072 | SideBySide | Measured | 84.263 | 47.744 | 28,890,006 | 334,159,872 | 139,073 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| Git CLI | 139,072 | Combined | Measured | 132.566 | 0.299 | 2,650 | 334,778,368 | 24 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| Git CLI | 139,072 | SideBySide | Measured | 105.024 | 0.3 | 2,602 | 283,832,320 | 24 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| LovelyGit Prototype | 139,072 | Combined | Measured | 0.004 | 12.524 | 8,288,522 | 297,762,816 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| LovelyGit Prototype | 139,072 | SideBySide | Measured | 0.004 | 12.624 | 8,288,520 | 330,162,176 | 139,072 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| MyersDiff | 139,072 | Combined | Measured | 28.049 | 34.634 | 22,330,879 | 335,302,656 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| MyersDiff | 139,072 | SideBySide | Measured | 27.344 | 45.616 | 28,890,115 | 334,336,000 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| NGitDiff Histogram | 139,072 | Combined | Measured | 111.513 | 35.815 | 22,330,888 | 334,983,168 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| NGitDiff Histogram | 139,072 | SideBySide | Measured | 93.978 | 46.873 | 28,890,124 | 311,148,544 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| NGitDiff Myers | 139,072 | Combined | Measured | 115.715 | 37.871 | 22,330,884 | 306,647,040 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| NGitDiff Myers | 139,072 | SideBySide | Measured | 126.757 | 49.343 | 28,890,120 | 348,921,856 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| spkl.Diffs | 139,072 | Combined | Measured | 29.375 | 35.077 | 22,330,880 | 334,143,488 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |
| spkl.Diffs | 139,072 | SideBySide | Measured | 27.979 | 45.874 | 28,890,116 | 334,635,008 | 139,074 | real Chromium file: infra\config\generated\luci\cr-buildbucket.cfg |

## Scenario: chromium-xml-cdata-edit

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 156,982 | Combined | Measured | 190.274 | 21.654 | 11,970,178 | 360,583,168 | 8 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| CSharpDiff | 156,982 | SideBySide | Measured | 169.756 | 36.618 | 23,939,086 | 360,755,200 | 8 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| Diff4Net | 156,982 | Combined | Measured | 72.583 | 33.927 | 29,172,348 | 309,207,040 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| Diff4Net | 156,982 | SideBySide | Measured | 70.837 | 42.967 | 40,513,346 | 365,285,376 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| DiffMatchPatch | 156,982 | Combined | Measured | 3845.695 | 36.884 | 29,172,866 | 578,764,800 | 156,989 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| DiffMatchPatch | 156,982 | SideBySide | Measured | 3815.503 | 50.642 | 40,513,906 | 582,561,792 | 156,989 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| DiffPlex | 156,982 | Combined | Measured | 99.113 | 31.251 | 29,172,348 | 363,974,656 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| DiffPlex | 156,982 | SideBySide | Measured | 121.631 | 48.004 | 40,513,238 | 467,324,928 | 156,983 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| Git CLI | 156,982 | Combined | Measured | 175.123 | 0.299 | 2,650 | 361,385,984 | 24 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| Git CLI | 156,982 | SideBySide | Measured | 151.633 | 0.3 | 2,602 | 294,985,728 | 24 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| LovelyGit Prototype | 156,982 | Combined | Measured | 0.004 | 15.362 | 14,726,082 | 332,308,480 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| LovelyGit Prototype | 156,982 | SideBySide | Measured | 0.003 | 15.41 | 14,726,080 | 371,273,728 | 156,982 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| MyersDiff | 156,982 | Combined | Measured | 63.642 | 31.237 | 29,172,349 | 362,233,856 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| MyersDiff | 156,982 | SideBySide | Measured | 55.152 | 47.474 | 40,513,347 | 455,122,944 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| NGitDiff Histogram | 156,982 | Combined | Measured | 164.434 | 34.623 | 29,172,245 | 306,139,136 | 156,983 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| NGitDiff Histogram | 156,982 | SideBySide | Measured | 152.988 | 47.707 | 40,513,245 | 464,404,480 | 156,983 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| NGitDiff Myers | 156,982 | Combined | Measured | 180.058 | 35.402 | 29,172,241 | 405,635,072 | 156,983 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| NGitDiff Myers | 156,982 | SideBySide | Measured | 179.921 | 48.379 | 40,513,241 | 339,677,184 | 156,983 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| spkl.Diffs | 156,982 | Combined | Measured | 56.418 | 34.194 | 29,172,350 | 357,466,112 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |
| spkl.Diffs | 156,982 | SideBySide | Measured | 46.218 | 45.439 | 40,513,348 | 352,997,376 | 156,984 | real Chromium file: third_party\blink\web_tests\external\wpt\xml\xslt\resources\large_CDATA.xml |

## Scenario: deleted

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 10 | Combined | Measured | 0.071 | 0.338 | 457 | 5,296,128 | 1 | deleted file |
| CSharpDiff | 10 | SideBySide | Measured | 0.071 | 0.34 | 457 | 5,296,128 | 1 | deleted file |
| CSharpDiff | 100 | Combined | Measured | 0.186 | 0.349 | 2,077 | 5,296,128 | 1 | deleted file |
| CSharpDiff | 100 | SideBySide | Measured | 0.178 | 0.37 | 2,077 | 5,292,032 | 1 | deleted file |
| CSharpDiff | 1,000 | Combined | Measured | 6.752 | 0.398 | 18,277 | 5,296,128 | 1 | deleted file |
| CSharpDiff | 1,000 | SideBySide | Measured | 7.125 | 0.446 | 18,277 | 5,300,224 | 1 | deleted file |
| CSharpDiff | 10,000 | Combined | Measured | 626.937 | 1.186 | 180,277 | 23,801,856 | 1 | deleted file |
| CSharpDiff | 10,000 | SideBySide | Measured | 643.245 | 1.165 | 180,277 | 23,818,240 | 1 | deleted file |
| CSharpDiff | 100,000 | Combined | ReusedSlow |  |  |  | 64,622,592 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 100,000 | SideBySide | ReusedSlow |  |  |  | 68,280,320 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 1,000,000 | Combined | ReusedSlow |  |  |  | 544,231,424 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 1,000,000 | SideBySide | ReusedSlow |  |  |  | 543,211,520 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| Diff4Net | 10 | Combined | Measured | 0.021 | 0.393 | 1,374 | 5,296,128 | 10 | deleted file |
| Diff4Net | 10 | SideBySide | Measured | 0.022 | 0.368 | 1,374 | 5,296,128 | 10 | deleted file |
| Diff4Net | 100 | Combined | Measured | 0.039 | 0.395 | 12,265 | 5,296,128 | 100 | deleted file |
| Diff4Net | 100 | SideBySide | Measured | 0.037 | 0.386 | 12,265 | 5,296,128 | 100 | deleted file |
| Diff4Net | 1,000 | Combined | Measured | 0.181 | 0.571 | 122,066 | 5,296,128 | 1,000 | deleted file |
| Diff4Net | 1,000 | SideBySide | Measured | 0.155 | 0.554 | 122,066 | 5,296,128 | 1,000 | deleted file |
| Diff4Net | 10,000 | Combined | Measured | 1.563 | 2.515 | 1,229,067 | 10,312,912 | 10,000 | deleted file |
| Diff4Net | 10,000 | SideBySide | Measured | 1.418 | 2.603 | 1,229,067 | 8,130,008 | 10,000 | deleted file |
| Diff4Net | 100,000 | Combined | Measured | 19.208 | 19.813 | 12,389,068 | 72,253,000 | 100,000 | deleted file |
| Diff4Net | 100,000 | SideBySide | Measured | 18.81 | 19.35 | 12,389,068 | 72,253,000 | 100,000 | deleted file |
| Diff4Net | 1,000,000 | Combined | Measured | 274.198 | 176.847 | 124,889,069 | 712,413,184 | 1,000,000 | deleted file |
| Diff4Net | 1,000,000 | SideBySide | Measured | 276.745 | 172.534 | 124,889,069 | 665,600,000 | 1,000,000 | deleted file |
| DiffMatchPatch | 10 | Combined | Measured | 1.298 | 0.319 | 1,380 | 5,296,128 | 10 | deleted file |
| DiffMatchPatch | 10 | SideBySide | Measured | 1.16 | 0.563 | 1,380 | 4,812,800 | 10 | deleted file |
| DiffMatchPatch | 100 | Combined | Measured | 1.236 | 0.391 | 12,271 | 5,296,128 | 100 | deleted file |
| DiffMatchPatch | 100 | SideBySide | Measured | 1.239 | 0.334 | 12,271 | 5,296,128 | 100 | deleted file |
| DiffMatchPatch | 1,000 | Combined | Measured | 1.316 | 0.541 | 122,072 | 5,296,128 | 1,000 | deleted file |
| DiffMatchPatch | 1,000 | SideBySide | Measured | 1.279 | 0.521 | 122,072 | 5,292,032 | 1,000 | deleted file |
| DiffMatchPatch | 10,000 | Combined | Measured | 1.945 | 2.31 | 1,229,073 | 8,144,560 | 10,000 | deleted file |
| DiffMatchPatch | 10,000 | SideBySide | Measured | 1.939 | 2.423 | 1,229,073 | 8,144,560 | 10,000 | deleted file |
| DiffMatchPatch | 100,000 | Combined | Measured | 12.357 | 20.919 | 12,389,074 | 74,833,536 | 100,000 | deleted file |
| DiffMatchPatch | 100,000 | SideBySide | Measured | 6.974 | 20.138 | 12,389,074 | 74,833,480 | 100,000 | deleted file |
| DiffMatchPatch | 1,000,000 | Combined | Measured | 163.627 | 190.141 | 124,889,075 | 845,996,032 | 1,000,000 | deleted file |
| DiffMatchPatch | 1,000,000 | SideBySide | Measured | 165.597 | 183.986 | 124,889,075 | 836,362,240 | 1,000,000 | deleted file |
| DiffPlex | 10 | Combined | Measured | 0.064 | 0.347 | 1,374 | 5,296,128 | 10 | deleted file |
| DiffPlex | 10 | SideBySide | Measured | 0.023 | 0.354 | 1,374 | 5,296,128 | 10 | deleted file |
| DiffPlex | 100 | Combined | Measured | 0.081 | 0.355 | 12,265 | 5,296,128 | 100 | deleted file |
| DiffPlex | 100 | SideBySide | Measured | 0.045 | 0.363 | 12,265 | 5,300,224 | 100 | deleted file |
| DiffPlex | 1,000 | Combined | Measured | 0.4 | 0.524 | 122,066 | 5,296,128 | 1,000 | deleted file |
| DiffPlex | 1,000 | SideBySide | Measured | 0.377 | 0.585 | 122,066 | 5,296,128 | 1,000 | deleted file |
| DiffPlex | 10,000 | Combined | Measured | 2.314 | 2.686 | 1,229,067 | 8,330,128 | 10,000 | deleted file |
| DiffPlex | 10,000 | SideBySide | Measured | 2.438 | 2.832 | 1,229,067 | 12,392,864 | 10,000 | deleted file |
| DiffPlex | 100,000 | Combined | Measured | 23.406 | 19.749 | 12,389,068 | 73,402,360 | 100,000 | deleted file |
| DiffPlex | 100,000 | SideBySide | Measured | 31.857 | 18.262 | 12,389,068 | 78,112,592 | 100,000 | deleted file |
| DiffPlex | 1,000,000 | Combined | Measured | 410.699 | 189.385 | 124,889,069 | 1,011,785,728 | 1,000,000 | deleted file |
| DiffPlex | 1,000,000 | SideBySide | Measured | 458.935 | 186.152 | 124,889,069 | 807,780,352 | 1,000,000 | deleted file |
| Git CLI | 10 | Combined | Measured | 45.594 | 0.338 | 1,402 | 12,824,576 | 12 | deleted file |
| Git CLI | 10 | SideBySide | Measured | 51.13 | 0.385 | 1,378 | 12,836,864 | 12 | deleted file |
| Git CLI | 100 | Combined | Measured | 46.831 | 0.344 | 10,768 | 13,017,088 | 102 | deleted file |
| Git CLI | 100 | SideBySide | Measured | 47.768 | 0.365 | 10,564 | 13,017,088 | 102 | deleted file |
| Git CLI | 1,000 | Combined | Measured | 58.278 | 0.537 | 106,174 | 13,574,144 | 1,002 | deleted file |
| Git CLI | 1,000 | SideBySide | Measured | 91.928 | 0.528 | 104,170 | 13,193,216 | 1,002 | deleted file |
| Git CLI | 10,000 | Combined | Measured | 50.47 | 2.259 | 1,078,180 | 16,871,424 | 10,002 | deleted file |
| Git CLI | 10,000 | SideBySide | Measured | 62.792 | 2.351 | 1,058,176 | 16,527,360 | 10,002 | deleted file |
| Git CLI | 100,000 | Combined | Measured | 80.574 | 18.077 | 10,978,186 | 63,829,632 | 100,002 | deleted file |
| Git CLI | 100,000 | SideBySide | Measured | 78.432 | 17.515 | 10,778,182 | 63,429,624 | 100,002 | deleted file |
| Git CLI | 1,000,000 | Combined | Measured | 504.647 | 174.75 | 111,778,192 | 673,808,384 | 1,000,002 | deleted file |
| Git CLI | 1,000,000 | SideBySide | Measured | 479.801 | 179.686 | 109,778,188 | 747,937,792 | 1,000,002 | deleted file |
| LovelyGit Prototype | 10 | Combined | Measured | 0.004 | 0.016 | 518 | 5,349,376 | 10 | deleted file |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.003 | 0.017 | 570 | 4,866,048 | 10 | deleted file |
| LovelyGit Prototype | 100 | Combined | Measured | 0.003 | 0.022 | 3,309 | 5,353,472 | 100 | deleted file |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.004 | 0.026 | 3,811 | 11,657,216 | 100 | deleted file |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.003 | 0.069 | 32,110 | 11,698,176 | 1,000 | deleted file |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.004 | 0.08 | 37,112 | 4,866,048 | 1,000 | deleted file |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.003 | 0.516 | 329,111 | 5,353,472 | 10,000 | deleted file |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.003 | 0.563 | 379,113 | 5,349,376 | 10,000 | deleted file |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.004 | 5.28 | 3,389,112 | 83,484,672 | 100,000 | deleted file |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.004 | 5.4 | 3,889,114 | 77,295,616 | 100,000 | deleted file |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.004 | 50.25 | 34,889,113 | 549,773,312 | 1,000,000 | deleted file |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.004 | 52.167 | 39,889,115 | 541,593,600 | 1,000,000 | deleted file |
| MyersDiff | 10 | Combined | Measured | 0.012 | 0.338 | 1,375 | 5,296,128 | 10 | deleted file |
| MyersDiff | 10 | SideBySide | Measured | 0.012 | 0.359 | 1,375 | 5,296,128 | 10 | deleted file |
| MyersDiff | 100 | Combined | Measured | 0.027 | 0.398 | 12,266 | 5,296,128 | 100 | deleted file |
| MyersDiff | 100 | SideBySide | Measured | 0.024 | 0.417 | 12,266 | 5,300,224 | 100 | deleted file |
| MyersDiff | 1,000 | Combined | Measured | 0.113 | 0.607 | 122,067 | 5,296,128 | 1,000 | deleted file |
| MyersDiff | 1,000 | SideBySide | Measured | 0.101 | 0.545 | 122,067 | 5,292,032 | 1,000 | deleted file |
| MyersDiff | 10,000 | Combined | Measured | 0.911 | 2.535 | 1,229,068 | 8,132,048 | 10,000 | deleted file |
| MyersDiff | 10,000 | SideBySide | Measured | 1.063 | 2.692 | 1,229,068 | 8,132,048 | 10,000 | deleted file |
| MyersDiff | 100,000 | Combined | Measured | 9.958 | 18.828 | 12,389,069 | 72,249,800 | 100,000 | deleted file |
| MyersDiff | 100,000 | SideBySide | Measured | 10.985 | 19.41 | 12,389,069 | 72,249,800 | 100,000 | deleted file |
| MyersDiff | 1,000,000 | Combined | Measured | 200.376 | 182.512 | 124,889,070 | 946,417,664 | 1,000,000 | deleted file |
| MyersDiff | 1,000,000 | SideBySide | Measured | 197.94 | 181.273 | 124,889,070 | 791,781,376 | 1,000,000 | deleted file |
| NGitDiff Histogram | 10 | Combined | Measured | 0.057 | 0.378 | 1,384 | 5,296,128 | 10 | deleted file |
| NGitDiff Histogram | 10 | SideBySide | Measured | 0.055 | 0.659 | 1,384 | 5,300,224 | 10 | deleted file |
| NGitDiff Histogram | 100 | Combined | Measured | 0.071 | 0.35 | 12,275 | 5,296,128 | 100 | deleted file |
| NGitDiff Histogram | 100 | SideBySide | Measured | 0.073 | 0.396 | 12,275 | 4,812,800 | 100 | deleted file |
| NGitDiff Histogram | 1,000 | Combined | Measured | 0.205 | 0.544 | 122,076 | 5,296,128 | 1,000 | deleted file |
| NGitDiff Histogram | 1,000 | SideBySide | Measured | 0.206 | 0.555 | 122,076 | 5,296,128 | 1,000 | deleted file |
| NGitDiff Histogram | 10,000 | Combined | Measured | 1.572 | 2.505 | 1,229,077 | 8,005,888 | 10,000 | deleted file |
| NGitDiff Histogram | 10,000 | SideBySide | Measured | 1.675 | 2.552 | 1,229,077 | 8,005,888 | 10,000 | deleted file |
| NGitDiff Histogram | 100,000 | Combined | Measured | 16.572 | 19.157 | 12,389,078 | 71,202,440 | 100,000 | deleted file |
| NGitDiff Histogram | 100,000 | SideBySide | Measured | 15.859 | 18.618 | 12,389,078 | 71,202,440 | 100,000 | deleted file |
| NGitDiff Histogram | 1,000,000 | Combined | Measured | 313.627 | 185.921 | 124,889,079 | 728,023,040 | 1,000,000 | deleted file |
| NGitDiff Histogram | 1,000,000 | SideBySide | Measured | 308.53 | 185.196 | 124,889,079 | 918,732,800 | 1,000,000 | deleted file |
| NGitDiff Myers | 10 | Combined | Measured | 0.06 | 0.347 | 1,380 | 4,812,800 | 10 | deleted file |
| NGitDiff Myers | 10 | SideBySide | Measured | 0.053 | 0.339 | 1,380 | 5,300,224 | 10 | deleted file |
| NGitDiff Myers | 100 | Combined | Measured | 0.071 | 0.364 | 12,271 | 5,296,128 | 100 | deleted file |
| NGitDiff Myers | 100 | SideBySide | Measured | 0.071 | 0.365 | 12,271 | 5,296,128 | 100 | deleted file |
| NGitDiff Myers | 1,000 | Combined | Measured | 0.21 | 0.562 | 122,072 | 5,296,128 | 1,000 | deleted file |
| NGitDiff Myers | 1,000 | SideBySide | Measured | 0.21 | 0.536 | 122,072 | 5,296,128 | 1,000 | deleted file |
| NGitDiff Myers | 10,000 | Combined | Measured | 1.563 | 2.525 | 1,229,073 | 8,005,488 | 10,000 | deleted file |
| NGitDiff Myers | 10,000 | SideBySide | Measured | 2.096 | 2.556 | 1,229,073 | 10,682,160 | 10,000 | deleted file |
| NGitDiff Myers | 100,000 | Combined | Measured | 16.05 | 18.968 | 12,389,074 | 71,202,344 | 100,000 | deleted file |
| NGitDiff Myers | 100,000 | SideBySide | Measured | 16.955 | 18.188 | 12,389,074 | 71,202,344 | 100,000 | deleted file |
| NGitDiff Myers | 1,000,000 | Combined | Measured | 283.986 | 188.622 | 124,889,075 | 857,366,528 | 1,000,000 | deleted file |
| NGitDiff Myers | 1,000,000 | SideBySide | Measured | 314.241 | 181.937 | 124,889,075 | 827,805,696 | 1,000,000 | deleted file |
| spkl.Diffs | 10 | Combined | Measured | 0.014 | 0.393 | 1,376 | 5,296,128 | 10 | deleted file |
| spkl.Diffs | 10 | SideBySide | Measured | 0.013 | 0.579 | 1,376 | 5,296,128 | 10 | deleted file |
| spkl.Diffs | 100 | Combined | Measured | 0.023 | 0.377 | 12,267 | 5,296,128 | 100 | deleted file |
| spkl.Diffs | 100 | SideBySide | Measured | 0.025 | 0.386 | 12,267 | 5,300,224 | 100 | deleted file |
| spkl.Diffs | 1,000 | Combined | Measured | 0.127 | 0.781 | 122,068 | 5,296,128 | 1,000 | deleted file |
| spkl.Diffs | 1,000 | SideBySide | Measured | 0.12 | 0.57 | 122,068 | 5,296,128 | 1,000 | deleted file |
| spkl.Diffs | 10,000 | Combined | Measured | 0.861 | 2.371 | 1,229,069 | 8,132,048 | 10,000 | deleted file |
| spkl.Diffs | 10,000 | SideBySide | Measured | 0.928 | 2.398 | 1,229,069 | 8,132,048 | 10,000 | deleted file |
| spkl.Diffs | 100,000 | Combined | Measured | 6.265 | 20.303 | 12,389,070 | 74,861,728 | 100,000 | deleted file |
| spkl.Diffs | 100,000 | SideBySide | Measured | 6.562 | 20.48 | 12,389,070 | 74,861,728 | 100,000 | deleted file |
| spkl.Diffs | 1,000,000 | Combined | Measured | 170.568 | 180.617 | 124,889,071 | 890,523,648 | 1,000,000 | deleted file |
| spkl.Diffs | 1,000,000 | SideBySide | Measured | 179.765 | 181.952 | 124,889,071 | 804,564,992 | 1,000,000 | deleted file |

## Scenario: lovelygit-billion-synthetic

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| LovelyGit Prototype | 1,000,000,000 | Combined | Measured | 0.003 | 0.009 | 107,777,777,998 | 5,095,424 | 1,000,000,000 | LovelyGit-only virtual one-object payload count; rows are not materialized. |
| LovelyGit Prototype | 1,000,000,000 | SideBySide | Measured | 0.003 | 0.008 | 107,777,778,000 | 5,373,952 | 1,000,000,000 | LovelyGit-only virtual one-object payload count; rows are not materialized. |

## Scenario: modified-bottom

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 10 | Combined | Measured | 0.152 | 0.353 | 679 | 5,296,128 | 3 | one bottom edit |
| CSharpDiff | 10 | SideBySide | Measured | 0.161 | 0.369 | 839 | 5,296,128 | 3 | one bottom edit |
| CSharpDiff | 100 | Combined | Measured | 0.258 | 0.395 | 2,299 | 5,292,032 | 3 | one bottom edit |
| CSharpDiff | 100 | SideBySide | Measured | 0.179 | 0.35 | 4,079 | 5,599,232 | 3 | one bottom edit |
| CSharpDiff | 1,000 | Combined | Measured | 0.595 | 0.41 | 18,499 | 5,296,128 | 3 | one bottom edit |
| CSharpDiff | 1,000 | SideBySide | Measured | 0.652 | 0.507 | 36,479 | 4,812,800 | 3 | one bottom edit |
| CSharpDiff | 10,000 | Combined | Measured | 4.111 | 1.096 | 180,499 | 11,890,896 | 3 | one bottom edit |
| CSharpDiff | 10,000 | SideBySide | Measured | 4.164 | 1.336 | 360,479 | 12,250,904 | 3 | one bottom edit |
| CSharpDiff | 100,000 | Combined | Measured | 65.651 | 3.56 | 1,800,499 | 105,231,816 | 3 | one bottom edit |
| CSharpDiff | 100,000 | SideBySide | Measured | 67.074 | 5.038 | 3,600,479 | 94,670,848 | 3 | one bottom edit |
| CSharpDiff | 1,000,000 | Combined | Measured | 755.063 | 29.706 | 18,000,499 | 680,288,832 | 3 | one bottom edit |
| CSharpDiff | 1,000,000 | SideBySide | Measured | 728.5 | 56.071 | 36,000,479 | 631,861,248 | 3 | one bottom edit |
| Diff4Net | 10 | Combined | Measured | 0.021 | 0.362 | 1,485 | 5,300,224 | 11 | one bottom edit |
| Diff4Net | 10 | SideBySide | Measured | 0.023 | 0.378 | 1,611 | 5,296,128 | 11 | one bottom edit |
| Diff4Net | 100 | Combined | Measured | 0.04 | 0.373 | 12,377 | 5,300,224 | 101 | one bottom edit |
| Diff4Net | 100 | SideBySide | Measured | 0.041 | 0.389 | 13,763 | 5,300,224 | 101 | one bottom edit |
| Diff4Net | 1,000 | Combined | Measured | 0.198 | 0.561 | 123,079 | 4,812,800 | 1,001 | one bottom edit |
| Diff4Net | 1,000 | SideBySide | Measured | 0.226 | 0.635 | 137,065 | 5,296,128 | 1,001 | one bottom edit |
| Diff4Net | 10,000 | Combined | Measured | 1.935 | 2.685 | 1,248,081 | 11,554,600 | 10,001 | one bottom edit |
| Diff4Net | 10,000 | SideBySide | Measured | 1.924 | 2.592 | 1,388,067 | 9,010,120 | 10,001 | one bottom edit |
| Diff4Net | 100,000 | Combined | Measured | 22.834 | 18.677 | 12,678,083 | 81,216,336 | 100,001 | one bottom edit |
| Diff4Net | 100,000 | SideBySide | Measured | 24.197 | 21.833 | 14,078,069 | 89,616,248 | 100,001 | one bottom edit |
| Diff4Net | 1,000,000 | Combined | Measured | 407.218 | 192.547 | 128,778,085 | 959,029,248 | 1,000,001 | one bottom edit |
| Diff4Net | 1,000,000 | SideBySide | Measured | 401.207 | 242.3 | 142,778,071 | 1,014,886,720 | 1,000,001 | one bottom edit |
| DiffMatchPatch | 10 | Combined | Measured | 1.254 | 0.356 | 1,596 | 5,296,128 | 12 | one bottom edit |
| DiffMatchPatch | 10 | SideBySide | Measured | 1.17 | 0.324 | 1,720 | 5,296,128 | 12 | one bottom edit |
| DiffMatchPatch | 100 | Combined | Measured | 1.082 | 0.322 | 12,490 | 5,300,224 | 102 | one bottom edit |
| DiffMatchPatch | 100 | SideBySide | Measured | 1.111 | 0.345 | 13,874 | 5,296,128 | 102 | one bottom edit |
| DiffMatchPatch | 1,000 | Combined | Measured | 1.192 | 0.509 | 123,194 | 5,300,224 | 1,002 | one bottom edit |
| DiffMatchPatch | 1,000 | SideBySide | Measured | 1.768 | 0.609 | 137,178 | 5,296,128 | 1,002 | one bottom edit |
| DiffMatchPatch | 10,000 | Combined | Measured | 2.127 | 2.354 | 1,248,198 | 8,183,016 | 10,002 | one bottom edit |
| DiffMatchPatch | 10,000 | SideBySide | Measured | 2.028 | 2.651 | 1,388,182 | 8,462,984 | 10,002 | one bottom edit |
| DiffMatchPatch | 100,000 | Combined | Measured | 7.225 | 17.556 | 12,678,202 | 81,874,944 | 100,002 | one bottom edit |
| DiffMatchPatch | 100,000 | SideBySide | Measured | 8.377 | 20.285 | 14,078,186 | 78,203,984 | 100,002 | one bottom edit |
| DiffMatchPatch | 1,000,000 | Combined | Measured | 188.815 | 186.92 | 128,778,206 | 814,845,952 | 1,000,002 | one bottom edit |
| DiffMatchPatch | 1,000,000 | SideBySide | Measured | 180.546 | 232.584 | 142,778,190 | 1,024,909,312 | 1,000,002 | one bottom edit |
| DiffPlex | 10 | Combined | Measured | 0.077 | 0.34 | 1,485 | 5,296,128 | 11 | one bottom edit |
| DiffPlex | 10 | SideBySide | Measured | 0.031 | 0.374 | 1,504 | 5,296,128 | 10 | one bottom edit |
| DiffPlex | 100 | Combined | Measured | 0.075 | 0.367 | 12,377 | 5,296,128 | 101 | one bottom edit |
| DiffPlex | 100 | SideBySide | Measured | 0.06 | 0.357 | 13,656 | 5,296,128 | 100 | one bottom edit |
| DiffPlex | 1,000 | Combined | Measured | 0.449 | 0.512 | 123,079 | 5,300,224 | 1,001 | one bottom edit |
| DiffPlex | 1,000 | SideBySide | Measured | 0.468 | 0.59 | 136,958 | 5,296,128 | 1,000 | one bottom edit |
| DiffPlex | 10,000 | Combined | Measured | 3.15 | 2.921 | 1,248,081 | 11,291,856 | 10,001 | one bottom edit |
| DiffPlex | 10,000 | SideBySide | Measured | 3.135 | 2.684 | 1,387,960 | 13,485,288 | 10,000 | one bottom edit |
| DiffPlex | 100,000 | Combined | Measured | 30.718 | 19.429 | 12,678,083 | 78,086,624 | 100,001 | one bottom edit |
| DiffPlex | 100,000 | SideBySide | Measured | 43.907 | 21.47 | 14,077,962 | 135,094,272 | 100,000 | one bottom edit |
| DiffPlex | 1,000,000 | Combined | Measured | 468.36 | 191.348 | 128,778,085 | 926,445,568 | 1,000,001 | one bottom edit |
| DiffPlex | 1,000,000 | SideBySide | Measured | 569.017 | 228.572 | 142,777,964 | 1,247,129,600 | 1,000,000 | one bottom edit |
| Git CLI | 10 | Combined | Measured | 45.178 | 0.348 | 886 | 13,717,504 | 7 | one bottom edit |
| Git CLI | 10 | SideBySide | Measured | 42.999 | 0.337 | 872 | 12,427,264 | 7 | one bottom edit |
| Git CLI | 100 | Combined | Measured | 48.32 | 0.315 | 886 | 12,967,936 | 7 | one bottom edit |
| Git CLI | 100 | SideBySide | Measured | 58.422 | 0.324 | 872 | 12,943,360 | 7 | one bottom edit |
| Git CLI | 1,000 | Combined | Measured | 54.266 | 0.341 | 886 | 13,565,952 | 7 | one bottom edit |
| Git CLI | 1,000 | SideBySide | Measured | 63.239 | 0.354 | 872 | 13,565,952 | 7 | one bottom edit |
| Git CLI | 10,000 | Combined | Measured | 51.789 | 0.339 | 886 | 18,960,384 | 7 | one bottom edit |
| Git CLI | 10,000 | SideBySide | Measured | 50.863 | 0.356 | 872 | 18,411,520 | 7 | one bottom edit |
| Git CLI | 100,000 | Combined | Measured | 68.429 | 0.296 | 886 | 67,543,040 | 7 | one bottom edit |
| Git CLI | 100,000 | SideBySide | Measured | 80.121 | 0.292 | 872 | 67,596,288 | 7 | one bottom edit |
| Git CLI | 1,000,000 | Combined | Measured | 251.702 | 0.309 | 886 | 488,886,272 | 7 | one bottom edit |
| Git CLI | 1,000,000 | SideBySide | Measured | 262.752 | 0.283 | 872 | 488,968,192 | 7 | one bottom edit |
| LovelyGit Prototype | 10 | Combined | Measured | 0.004 | 0.018 | 520 | 5,349,376 | 11 | one bottom edit |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.003 | 0.018 | 508 | 4,890,624 | 10 | one bottom edit |
| LovelyGit Prototype | 100 | Combined | Measured | 0.003 | 0.027 | 3,132 | 4,866,048 | 101 | one bottom edit |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.003 | 0.026 | 3,120 | 5,283,840 | 100 | one bottom edit |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.004 | 0.112 | 31,034 | 5,353,472 | 1,001 | one bottom edit |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.004 | 0.181 | 31,022 | 14,155,776 | 1,000 | one bottom edit |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.004 | 0.593 | 328,036 | 4,866,048 | 10,001 | one bottom edit |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.003 | 0.584 | 328,024 | 5,349,376 | 10,000 | one bottom edit |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.003 | 5.849 | 3,478,038 | 77,283,328 | 100,001 | one bottom edit |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.003 | 6.037 | 3,478,026 | 77,316,096 | 100,000 | one bottom edit |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.004 | 60.013 | 36,778,040 | 547,463,168 | 1,000,001 | one bottom edit |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.004 | 59.727 | 36,778,028 | 555,999,232 | 1,000,000 | one bottom edit |
| MyersDiff | 10 | Combined | Measured | 0.158 | 0.367 | 1,486 | 5,300,224 | 11 | one bottom edit |
| MyersDiff | 10 | SideBySide | Measured | 0.148 | 0.373 | 1,612 | 5,300,224 | 11 | one bottom edit |
| MyersDiff | 100 | Combined | Measured | 0.167 | 0.389 | 12,378 | 5,296,128 | 101 | one bottom edit |
| MyersDiff | 100 | SideBySide | Measured | 0.157 | 0.36 | 13,764 | 5,296,128 | 101 | one bottom edit |
| MyersDiff | 1,000 | Combined | Measured | 0.287 | 0.57 | 123,080 | 4,812,800 | 1,001 | one bottom edit |
| MyersDiff | 1,000 | SideBySide | Measured | 0.273 | 0.603 | 137,066 | 5,296,128 | 1,001 | one bottom edit |
| MyersDiff | 10,000 | Combined | Measured | 1.329 | 2.267 | 1,248,082 | 8,180,672 | 10,001 | one bottom edit |
| MyersDiff | 10,000 | SideBySide | Measured | 1.498 | 2.733 | 1,388,068 | 9,020,584 | 10,001 | one bottom edit |
| MyersDiff | 100,000 | Combined | Measured | 15.292 | 22.18 | 12,678,084 | 81,829,888 | 100,001 | one bottom edit |
| MyersDiff | 100,000 | SideBySide | Measured | 14.548 | 19.911 | 14,078,070 | 81,238,392 | 100,001 | one bottom edit |
| MyersDiff | 1,000,000 | Combined | Measured | 301.601 | 172.341 | 128,778,086 | 927,903,744 | 1,000,001 | one bottom edit |
| MyersDiff | 1,000,000 | SideBySide | Measured | 302.149 | 214.902 | 142,778,072 | 1,014,895,136 | 1,000,001 | one bottom edit |
| NGitDiff Histogram | 10 | Combined | Measured | 0.099 | 0.362 | 1,495 | 5,296,128 | 11 | one bottom edit |
| NGitDiff Histogram | 10 | SideBySide | Measured | 0.096 | 0.358 | 1,621 | 5,296,128 | 11 | one bottom edit |
| NGitDiff Histogram | 100 | Combined | Measured | 0.124 | 0.445 | 12,387 | 5,296,128 | 101 | one bottom edit |
| NGitDiff Histogram | 100 | SideBySide | Measured | 0.136 | 0.376 | 13,773 | 5,296,128 | 101 | one bottom edit |
| NGitDiff Histogram | 1,000 | Combined | Measured | 0.354 | 0.567 | 123,089 | 5,296,128 | 1,001 | one bottom edit |
| NGitDiff Histogram | 1,000 | SideBySide | Measured | 0.36 | 0.584 | 137,075 | 5,296,128 | 1,001 | one bottom edit |
| NGitDiff Histogram | 10,000 | Combined | Measured | 2.826 | 2.542 | 1,248,091 | 13,007,816 | 10,001 | one bottom edit |
| NGitDiff Histogram | 10,000 | SideBySide | Measured | 2.782 | 2.728 | 1,388,077 | 13,287,784 | 10,001 | one bottom edit |
| NGitDiff Histogram | 100,000 | Combined | Measured | 33.081 | 19.051 | 12,678,093 | 77,475,840 | 100,001 | one bottom edit |
| NGitDiff Histogram | 100,000 | SideBySide | Measured | 36.149 | 21.228 | 14,078,079 | 115,101,696 | 100,001 | one bottom edit |
| NGitDiff Histogram | 1,000,000 | Combined | Measured | 526.452 | 189.909 | 128,778,095 | 974,905,344 | 1,000,001 | one bottom edit |
| NGitDiff Histogram | 1,000,000 | SideBySide | Measured | 538.383 | 233.662 | 142,778,081 | 1,155,153,920 | 1,000,001 | one bottom edit |
| NGitDiff Myers | 10 | Combined | Measured | 0.1 | 0.381 | 1,491 | 5,300,224 | 11 | one bottom edit |
| NGitDiff Myers | 10 | SideBySide | Measured | 0.128 | 0.441 | 1,617 | 5,296,128 | 11 | one bottom edit |
| NGitDiff Myers | 100 | Combined | Measured | 0.117 | 0.368 | 12,383 | 5,292,032 | 101 | one bottom edit |
| NGitDiff Myers | 100 | SideBySide | Measured | 0.115 | 0.367 | 13,769 | 5,300,224 | 101 | one bottom edit |
| NGitDiff Myers | 1,000 | Combined | Measured | 0.381 | 0.586 | 123,085 | 5,296,128 | 1,001 | one bottom edit |
| NGitDiff Myers | 1,000 | SideBySide | Measured | 0.375 | 0.654 | 137,071 | 5,300,224 | 1,001 | one bottom edit |
| NGitDiff Myers | 10,000 | Combined | Measured | 2.771 | 2.712 | 1,248,087 | 8,041,664 | 10,001 | one bottom edit |
| NGitDiff Myers | 10,000 | SideBySide | Measured | 2.816 | 2.62 | 1,388,073 | 13,287,704 | 10,001 | one bottom edit |
| NGitDiff Myers | 100,000 | Combined | Measured | 37.612 | 18.563 | 12,678,089 | 77,496,320 | 100,001 | one bottom edit |
| NGitDiff Myers | 100,000 | SideBySide | Measured | 32.432 | 21.003 | 14,078,075 | 80,181,872 | 100,001 | one bottom edit |
| NGitDiff Myers | 1,000,000 | Combined | Measured | 552.654 | 180.895 | 128,778,091 | 876,703,744 | 1,000,001 | one bottom edit |
| NGitDiff Myers | 1,000,000 | SideBySide | Measured | 550.911 | 226.233 | 142,778,077 | 1,079,382,016 | 1,000,001 | one bottom edit |
| spkl.Diffs | 10 | Combined | Measured | 0.016 | 0.431 | 1,487 | 5,300,224 | 11 | one bottom edit |
| spkl.Diffs | 10 | SideBySide | Measured | 0.028 | 0.49 | 1,613 | 5,296,128 | 11 | one bottom edit |
| spkl.Diffs | 100 | Combined | Measured | 0.032 | 0.398 | 12,379 | 5,292,032 | 101 | one bottom edit |
| spkl.Diffs | 100 | SideBySide | Measured | 0.028 | 0.361 | 13,765 | 5,296,128 | 101 | one bottom edit |
| spkl.Diffs | 1,000 | Combined | Measured | 0.161 | 0.577 | 123,081 | 5,296,128 | 1,001 | one bottom edit |
| spkl.Diffs | 1,000 | SideBySide | Measured | 0.179 | 0.641 | 137,067 | 5,292,032 | 1,001 | one bottom edit |
| spkl.Diffs | 10,000 | Combined | Measured | 1.259 | 2.593 | 1,248,083 | 8,170,208 | 10,001 | one bottom edit |
| spkl.Diffs | 10,000 | SideBySide | Measured | 1.466 | 2.991 | 1,388,069 | 9,010,120 | 10,001 | one bottom edit |
| spkl.Diffs | 100,000 | Combined | Measured | 13.842 | 19.62 | 12,678,085 | 79,384,576 | 100,001 | one bottom edit |
| spkl.Diffs | 100,000 | SideBySide | Measured | 13.911 | 21.308 | 14,078,071 | 81,551,360 | 100,001 | one bottom edit |
| spkl.Diffs | 1,000,000 | Combined | Measured | 286.617 | 185.079 | 128,778,087 | 962,842,624 | 1,000,001 | one bottom edit |
| spkl.Diffs | 1,000,000 | SideBySide | Measured | 287.647 | 237.893 | 142,778,073 | 1,118,433,280 | 1,000,001 | one bottom edit |

## Scenario: modified-middle

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 10 | Combined | Measured | 0.162 | 0.354 | 784 | 5,296,128 | 4 | one middle edit |
| CSharpDiff | 10 | SideBySide | Measured | 0.17 | 0.366 | 940 | 5,300,224 | 4 | one middle edit |
| CSharpDiff | 100 | Combined | Measured | 0.201 | 0.349 | 2,404 | 5,292,032 | 4 | one middle edit |
| CSharpDiff | 100 | SideBySide | Measured | 0.202 | 0.365 | 4,180 | 5,300,224 | 4 | one middle edit |
| CSharpDiff | 1,000 | Combined | Measured | 0.6 | 0.387 | 18,604 | 5,296,128 | 4 | one middle edit |
| CSharpDiff | 1,000 | SideBySide | Measured | 0.565 | 0.398 | 36,580 | 5,296,128 | 4 | one middle edit |
| CSharpDiff | 10,000 | Combined | Measured | 4.19 | 1.146 | 180,604 | 5,296,128 | 4 | one middle edit |
| CSharpDiff | 10,000 | SideBySide | Measured | 4.256 | 1.481 | 360,580 | 5,296,128 | 4 | one middle edit |
| CSharpDiff | 100,000 | Combined | Measured | 65.385 | 4.39 | 1,800,604 | 88,178,688 | 4 | one middle edit |
| CSharpDiff | 100,000 | SideBySide | Measured | 66.84 | 6.811 | 3,600,580 | 90,939,392 | 4 | one middle edit |
| CSharpDiff | 1,000,000 | Combined | Measured | 764.26 | 26.25 | 18,000,604 | 579,869,296 | 4 | one middle edit |
| CSharpDiff | 1,000,000 | SideBySide | Measured | 774.74 | 48.972 | 36,000,580 | 557,330,432 | 4 | one middle edit |
| Diff4Net | 10 | Combined | Measured | 0.023 | 0.368 | 1,485 | 5,292,032 | 11 | one middle edit |
| Diff4Net | 10 | SideBySide | Measured | 0.022 | 0.375 | 1,611 | 5,296,128 | 11 | one middle edit |
| Diff4Net | 100 | Combined | Measured | 0.041 | 0.38 | 12,377 | 5,296,128 | 101 | one middle edit |
| Diff4Net | 100 | SideBySide | Measured | 0.047 | 0.403 | 13,763 | 5,296,128 | 101 | one middle edit |
| Diff4Net | 1,000 | Combined | Measured | 0.208 | 0.571 | 123,079 | 5,296,128 | 1,001 | one middle edit |
| Diff4Net | 1,000 | SideBySide | Measured | 0.202 | 0.585 | 137,065 | 5,296,128 | 1,001 | one middle edit |
| Diff4Net | 10,000 | Combined | Measured | 1.935 | 2.451 | 1,248,081 | 11,554,664 | 10,001 | one middle edit |
| Diff4Net | 10,000 | SideBySide | Measured | 1.832 | 2.728 | 1,388,067 | 11,834,640 | 10,001 | one middle edit |
| Diff4Net | 100,000 | Combined | Measured | 22.39 | 18.952 | 12,678,083 | 81,216,368 | 100,001 | one middle edit |
| Diff4Net | 100,000 | SideBySide | Measured | 21.809 | 20.892 | 14,078,069 | 89,616,248 | 100,001 | one middle edit |
| Diff4Net | 1,000,000 | Combined | Measured | 426.546 | 191.985 | 128,778,085 | 961,613,824 | 1,000,001 | one middle edit |
| Diff4Net | 1,000,000 | SideBySide | Measured | 410.102 | 220.245 | 142,778,071 | 1,014,886,720 | 1,000,001 | one middle edit |
| DiffMatchPatch | 10 | Combined | Measured | 1.305 | 0.305 | 1,701 | 5,296,128 | 13 | one middle edit |
| DiffMatchPatch | 10 | SideBySide | Measured | 1.228 | 0.315 | 1,823 | 5,296,128 | 13 | one middle edit |
| DiffMatchPatch | 100 | Combined | Measured | 1.225 | 0.326 | 12,597 | 5,292,032 | 103 | one middle edit |
| DiffMatchPatch | 100 | SideBySide | Measured | 1.434 | 0.352 | 13,979 | 5,300,224 | 103 | one middle edit |
| DiffMatchPatch | 1,000 | Combined | Measured | 1.271 | 0.604 | 123,303 | 5,296,128 | 1,003 | one middle edit |
| DiffMatchPatch | 1,000 | SideBySide | Measured | 1.316 | 0.587 | 137,285 | 5,296,128 | 1,003 | one middle edit |
| DiffMatchPatch | 10,000 | Combined | Measured | 7.606 | 2.569 | 1,248,309 | 8,117,744 | 10,003 | one middle edit |
| DiffMatchPatch | 10,000 | SideBySide | Measured | 2.058 | 2.587 | 1,388,291 | 8,397,712 | 10,003 | one middle edit |
| DiffMatchPatch | 100,000 | Combined | Measured | 8.146 | 18.389 | 12,678,315 | 73,491,120 | 100,003 | one middle edit |
| DiffMatchPatch | 100,000 | SideBySide | Measured | 7.826 | 20.542 | 14,078,297 | 76,291,080 | 100,003 | one middle edit |
| DiffMatchPatch | 1,000,000 | Combined | Measured | 181.53 | 183.48 | 128,778,321 | 882,085,888 | 1,000,003 | one middle edit |
| DiffMatchPatch | 1,000,000 | SideBySide | Measured | 181.862 | 229.305 | 142,778,303 | 1,048,563,712 | 1,000,003 | one middle edit |
| DiffPlex | 10 | Combined | Measured | 0.055 | 0.334 | 1,485 | 5,296,128 | 11 | one middle edit |
| DiffPlex | 10 | SideBySide | Measured | 0.027 | 0.353 | 1,504 | 5,296,128 | 10 | one middle edit |
| DiffPlex | 100 | Combined | Measured | 0.088 | 0.38 | 12,377 | 5,296,128 | 101 | one middle edit |
| DiffPlex | 100 | SideBySide | Measured | 0.065 | 0.367 | 13,656 | 5,296,128 | 100 | one middle edit |
| DiffPlex | 1,000 | Combined | Measured | 0.481 | 0.546 | 123,079 | 5,296,128 | 1,001 | one middle edit |
| DiffPlex | 1,000 | SideBySide | Measured | 0.473 | 0.574 | 136,958 | 5,296,128 | 1,000 | one middle edit |
| DiffPlex | 10,000 | Combined | Measured | 2.823 | 2.537 | 1,248,081 | 8,372,536 | 10,001 | one middle edit |
| DiffPlex | 10,000 | SideBySide | Measured | 3.253 | 2.769 | 1,387,960 | 13,485,288 | 10,000 | one middle edit |
| DiffPlex | 100,000 | Combined | Measured | 28.651 | 19.801 | 12,678,083 | 74,226,152 | 100,001 | one middle edit |
| DiffPlex | 100,000 | SideBySide | Measured | 44.348 | 22.169 | 14,077,962 | 142,827,520 | 100,000 | one middle edit |
| DiffPlex | 1,000,000 | Combined | Measured | 471.052 | 192.993 | 128,778,085 | 966,885,376 | 1,000,001 | one middle edit |
| DiffPlex | 1,000,000 | SideBySide | Measured | 571.203 | 220.294 | 142,777,964 | 1,277,292,544 | 1,000,000 | one middle edit |
| Git CLI | 10 | Combined | Measured | 49.775 | 0.34 | 1,194 | 12,836,864 | 10 | one middle edit |
| Git CLI | 10 | SideBySide | Measured | 45.921 | 0.353 | 1,174 | 12,828,672 | 10 | one middle edit |
| Git CLI | 100 | Combined | Measured | 46.178 | 0.322 | 1,194 | 12,943,360 | 10 | one middle edit |
| Git CLI | 100 | SideBySide | Measured | 48.551 | 0.333 | 1,174 | 13,373,440 | 10 | one middle edit |
| Git CLI | 1,000 | Combined | Measured | 48.006 | 0.414 | 1,194 | 13,742,080 | 10 | one middle edit |
| Git CLI | 1,000 | SideBySide | Measured | 48.32 | 0.336 | 1,174 | 13,570,048 | 10 | one middle edit |
| Git CLI | 10,000 | Combined | Measured | 52.237 | 0.344 | 1,194 | 17,936,384 | 10 | one middle edit |
| Git CLI | 10,000 | SideBySide | Measured | 68.829 | 0.38 | 1,174 | 17,555,456 | 10 | one middle edit |
| Git CLI | 100,000 | Combined | Measured | 69.742 | 0.305 | 1,194 | 60,698,624 | 10 | one middle edit |
| Git CLI | 100,000 | SideBySide | Measured | 68.558 | 0.303 | 1,174 | 60,698,624 | 10 | one middle edit |
| Git CLI | 1,000,000 | Combined | Measured | 251.005 | 0.299 | 1,194 | 488,808,448 | 10 | one middle edit |
| Git CLI | 1,000,000 | SideBySide | Measured | 314.876 | 0.278 | 1,174 | 501,436,416 | 10 | one middle edit |
| LovelyGit Prototype | 10 | Combined | Measured | 0.003 | 0.019 | 520 | 4,894,720 | 11 | one middle edit |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.004 | 0.018 | 508 | 4,866,048 | 10 | one middle edit |
| LovelyGit Prototype | 100 | Combined | Measured | 0.004 | 0.028 | 3,132 | 5,349,376 | 101 | one middle edit |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.004 | 0.027 | 3,120 | 8,011,776 | 100 | one middle edit |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.004 | 0.087 | 31,034 | 5,349,376 | 1,001 | one middle edit |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.003 | 0.08 | 31,022 | 5,349,376 | 1,000 | one middle edit |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.003 | 0.6 | 328,036 | 5,353,472 | 10,001 | one middle edit |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.004 | 0.807 | 328,024 | 5,349,376 | 10,000 | one middle edit |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.005 | 5.968 | 3,478,038 | 62,697,472 | 100,001 | one middle edit |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.003 | 6.3 | 3,478,026 | 77,352,960 | 100,000 | one middle edit |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.003 | 59.141 | 36,778,040 | 555,954,176 | 1,000,001 | one middle edit |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.003 | 58.284 | 36,778,028 | 547,090,432 | 1,000,000 | one middle edit |
| MyersDiff | 10 | Combined | Measured | 0.163 | 0.385 | 1,486 | 5,296,128 | 11 | one middle edit |
| MyersDiff | 10 | SideBySide | Measured | 0.189 | 0.539 | 1,612 | 5,296,128 | 11 | one middle edit |
| MyersDiff | 100 | Combined | Measured | 0.167 | 0.372 | 12,378 | 5,296,128 | 101 | one middle edit |
| MyersDiff | 100 | SideBySide | Measured | 0.16 | 0.407 | 13,764 | 5,300,224 | 101 | one middle edit |
| MyersDiff | 1,000 | Combined | Measured | 0.284 | 0.549 | 123,080 | 5,296,128 | 1,001 | one middle edit |
| MyersDiff | 1,000 | SideBySide | Measured | 0.282 | 0.589 | 137,066 | 5,296,128 | 1,001 | one middle edit |
| MyersDiff | 10,000 | Combined | Measured | 1.376 | 2.353 | 1,248,082 | 8,180,672 | 10,001 | one middle edit |
| MyersDiff | 10,000 | SideBySide | Measured | 1.492 | 2.734 | 1,388,068 | 9,020,584 | 10,001 | one middle edit |
| MyersDiff | 100,000 | Combined | Measured | 14.462 | 18.612 | 12,678,084 | 76,759,040 | 100,001 | one middle edit |
| MyersDiff | 100,000 | SideBySide | Measured | 14.229 | 21.35 | 14,078,070 | 81,238,392 | 100,001 | one middle edit |
| MyersDiff | 1,000,000 | Combined | Measured | 309.746 | 180.937 | 128,778,086 | 1,004,249,088 | 1,000,001 | one middle edit |
| MyersDiff | 1,000,000 | SideBySide | Measured | 321.248 | 224.82 | 142,778,072 | 1,173,098,496 | 1,000,001 | one middle edit |
| NGitDiff Histogram | 10 | Combined | Measured | 0.15 | 0.376 | 1,495 | 5,296,128 | 11 | one middle edit |
| NGitDiff Histogram | 10 | SideBySide | Measured | 0.102 | 0.357 | 1,621 | 5,296,128 | 11 | one middle edit |
| NGitDiff Histogram | 100 | Combined | Measured | 0.121 | 0.573 | 12,387 | 5,296,128 | 101 | one middle edit |
| NGitDiff Histogram | 100 | SideBySide | Measured | 0.116 | 0.364 | 13,773 | 5,300,224 | 101 | one middle edit |
| NGitDiff Histogram | 1,000 | Combined | Measured | 0.365 | 0.577 | 123,089 | 5,300,224 | 1,001 | one middle edit |
| NGitDiff Histogram | 1,000 | SideBySide | Measured | 0.364 | 0.569 | 137,075 | 5,296,128 | 1,001 | one middle edit |
| NGitDiff Histogram | 10,000 | Combined | Measured | 2.778 | 2.565 | 1,248,091 | 13,007,816 | 10,001 | one middle edit |
| NGitDiff Histogram | 10,000 | SideBySide | Measured | 2.91 | 2.826 | 1,388,077 | 13,287,784 | 10,001 | one middle edit |
| NGitDiff Histogram | 100,000 | Combined | Measured | 35.367 | 18.883 | 12,678,093 | 71,782,248 | 100,001 | one middle edit |
| NGitDiff Histogram | 100,000 | SideBySide | Measured | 34.666 | 20.72 | 14,078,079 | 80,182,168 | 100,001 | one middle edit |
| NGitDiff Histogram | 1,000,000 | Combined | Measured | 558.359 | 186.628 | 128,778,095 | 856,596,480 | 1,000,001 | one middle edit |
| NGitDiff Histogram | 1,000,000 | SideBySide | Measured | 539.459 | 226.477 | 142,778,081 | 1,154,961,408 | 1,000,001 | one middle edit |
| NGitDiff Myers | 10 | Combined | Measured | 0.094 | 0.356 | 1,491 | 5,296,128 | 11 | one middle edit |
| NGitDiff Myers | 10 | SideBySide | Measured | 0.091 | 0.351 | 1,617 | 5,296,128 | 11 | one middle edit |
| NGitDiff Myers | 100 | Combined | Measured | 0.122 | 0.36 | 12,383 | 5,296,128 | 101 | one middle edit |
| NGitDiff Myers | 100 | SideBySide | Measured | 0.119 | 0.382 | 13,769 | 5,296,128 | 101 | one middle edit |
| NGitDiff Myers | 1,000 | Combined | Measured | 0.534 | 0.552 | 123,085 | 5,296,128 | 1,001 | one middle edit |
| NGitDiff Myers | 1,000 | SideBySide | Measured | 0.397 | 0.775 | 137,071 | 5,296,128 | 1,001 | one middle edit |
| NGitDiff Myers | 10,000 | Combined | Measured | 2.81 | 2.483 | 1,248,087 | 13,007,736 | 10,001 | one middle edit |
| NGitDiff Myers | 10,000 | SideBySide | Measured | 2.885 | 2.818 | 1,388,073 | 13,287,704 | 10,001 | one middle edit |
| NGitDiff Myers | 100,000 | Combined | Measured | 34.365 | 18.267 | 12,678,089 | 71,782,096 | 100,001 | one middle edit |
| NGitDiff Myers | 100,000 | SideBySide | Measured | 33.574 | 20.832 | 14,078,075 | 125,239,296 | 100,001 | one middle edit |
| NGitDiff Myers | 1,000,000 | Combined | Measured | 529.067 | 188.725 | 128,778,091 | 1,016,217,600 | 1,000,001 | one middle edit |
| NGitDiff Myers | 1,000,000 | SideBySide | Measured | 530.529 | 233.401 | 142,778,077 | 1,006,498,400 | 1,000,001 | one middle edit |
| spkl.Diffs | 10 | Combined | Measured | 0.012 | 0.34 | 1,487 | 5,484,544 | 11 | one middle edit |
| spkl.Diffs | 10 | SideBySide | Measured | 0.016 | 0.411 | 1,613 | 5,296,128 | 11 | one middle edit |
| spkl.Diffs | 100 | Combined | Measured | 0.029 | 0.372 | 12,379 | 5,296,128 | 101 | one middle edit |
| spkl.Diffs | 100 | SideBySide | Measured | 0.03 | 0.38 | 13,765 | 5,296,128 | 101 | one middle edit |
| spkl.Diffs | 1,000 | Combined | Measured | 0.161 | 0.579 | 123,081 | 5,296,128 | 1,001 | one middle edit |
| spkl.Diffs | 1,000 | SideBySide | Measured | 0.156 | 0.624 | 137,067 | 5,296,128 | 1,001 | one middle edit |
| spkl.Diffs | 10,000 | Combined | Measured | 1.269 | 2.584 | 1,248,083 | 8,170,208 | 10,001 | one middle edit |
| spkl.Diffs | 10,000 | SideBySide | Measured | 1.231 | 2.622 | 1,388,069 | 9,010,120 | 10,001 | one middle edit |
| spkl.Diffs | 100,000 | Combined | Measured | 13.393 | 19.401 | 12,678,085 | 77,897,728 | 100,001 | one middle edit |
| spkl.Diffs | 100,000 | SideBySide | Measured | 13.399 | 21.459 | 14,078,071 | 81,227,880 | 100,001 | one middle edit |
| spkl.Diffs | 1,000,000 | Combined | Measured | 291.626 | 183.833 | 128,778,087 | 957,259,776 | 1,000,001 | one middle edit |
| spkl.Diffs | 1,000,000 | SideBySide | Measured | 286.351 | 228.63 | 142,778,073 | 1,014,884,560 | 1,000,001 | one middle edit |

## Scenario: modified-top

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 10 | Combined | Measured | 0.16 | 0.398 | 678 | 4,812,800 | 3 | one top edit |
| CSharpDiff | 10 | SideBySide | Measured | 0.179 | 0.4 | 836 | 5,296,128 | 3 | one top edit |
| CSharpDiff | 100 | Combined | Measured | 0.198 | 0.338 | 2,298 | 5,296,128 | 3 | one top edit |
| CSharpDiff | 100 | SideBySide | Measured | 0.203 | 0.353 | 4,076 | 5,296,128 | 3 | one top edit |
| CSharpDiff | 1,000 | Combined | Measured | 0.561 | 0.39 | 18,498 | 5,300,224 | 3 | one top edit |
| CSharpDiff | 1,000 | SideBySide | Measured | 0.561 | 0.397 | 36,476 | 5,296,128 | 3 | one top edit |
| CSharpDiff | 10,000 | Combined | Measured | 4.412 | 1.27 | 180,498 | 11,891,072 | 3 | one top edit |
| CSharpDiff | 10,000 | SideBySide | Measured | 4.387 | 1.41 | 360,476 | 12,251,008 | 3 | one top edit |
| CSharpDiff | 100,000 | Combined | Measured | 64.051 | 3.413 | 1,800,498 | 105,231,976 | 3 | one top edit |
| CSharpDiff | 100,000 | SideBySide | Measured | 66.869 | 5.25 | 3,600,476 | 77,045,760 | 3 | one top edit |
| CSharpDiff | 1,000,000 | Combined | Measured | 778.98 | 28.691 | 18,000,498 | 680,288,904 | 3 | one top edit |
| CSharpDiff | 1,000,000 | SideBySide | Measured | 767.553 | 56.317 | 36,000,476 | 591,618,048 | 3 | one top edit |
| Diff4Net | 10 | Combined | Measured | 0.022 | 0.343 | 1,482 | 5,296,128 | 11 | one top edit |
| Diff4Net | 10 | SideBySide | Measured | 0.033 | 0.387 | 1,608 | 5,296,128 | 11 | one top edit |
| Diff4Net | 100 | Combined | Measured | 0.042 | 0.366 | 12,374 | 5,296,128 | 101 | one top edit |
| Diff4Net | 100 | SideBySide | Measured | 0.048 | 0.402 | 13,760 | 5,296,128 | 101 | one top edit |
| Diff4Net | 1,000 | Combined | Measured | 0.198 | 0.573 | 123,076 | 5,296,128 | 1,001 | one top edit |
| Diff4Net | 1,000 | SideBySide | Measured | 0.201 | 0.573 | 137,062 | 5,292,032 | 1,001 | one top edit |
| Diff4Net | 10,000 | Combined | Measured | 1.842 | 2.392 | 1,248,078 | 11,554,592 | 10,001 | one top edit |
| Diff4Net | 10,000 | SideBySide | Measured | 1.793 | 2.719 | 1,388,064 | 9,010,104 | 10,001 | one top edit |
| Diff4Net | 100,000 | Combined | Measured | 22.085 | 20.574 | 12,678,080 | 81,216,344 | 100,001 | one top edit |
| Diff4Net | 100,000 | SideBySide | Measured | 21.988 | 22.116 | 14,078,066 | 89,616,240 | 100,001 | one top edit |
| Diff4Net | 1,000,000 | Combined | Measured | 402.936 | 185.604 | 128,778,082 | 1,025,454,080 | 1,000,001 | one top edit |
| Diff4Net | 1,000,000 | SideBySide | Measured | 402.978 | 236.661 | 142,778,068 | 1,060,921,344 | 1,000,001 | one top edit |
| DiffMatchPatch | 10 | Combined | Measured | 1.374 | 0.353 | 1,593 | 5,296,128 | 12 | one top edit |
| DiffMatchPatch | 10 | SideBySide | Measured | 1.169 | 0.326 | 1,717 | 5,300,224 | 12 | one top edit |
| DiffMatchPatch | 100 | Combined | Measured | 7.779 | 0.386 | 12,487 | 5,296,128 | 102 | one top edit |
| DiffMatchPatch | 100 | SideBySide | Measured | 1.128 | 0.335 | 13,871 | 5,300,224 | 102 | one top edit |
| DiffMatchPatch | 1,000 | Combined | Measured | 4.682 | 0.539 | 123,191 | 5,296,128 | 1,002 | one top edit |
| DiffMatchPatch | 1,000 | SideBySide | Measured | 1.229 | 0.546 | 137,175 | 5,296,128 | 1,002 | one top edit |
| DiffMatchPatch | 10,000 | Combined | Measured | 2.059 | 2.486 | 1,248,195 | 8,183,000 | 10,002 | one top edit |
| DiffMatchPatch | 10,000 | SideBySide | Measured | 2.106 | 2.566 | 1,388,179 | 8,462,968 | 10,002 | one top edit |
| DiffMatchPatch | 100,000 | Combined | Measured | 7.197 | 18.367 | 12,678,199 | 75,404,616 | 100,002 | one top edit |
| DiffMatchPatch | 100,000 | SideBySide | Measured | 8.304 | 22.589 | 14,078,183 | 78,204,640 | 100,002 | one top edit |
| DiffMatchPatch | 1,000,000 | Combined | Measured | 193.725 | 184.954 | 128,778,203 | 979,079,168 | 1,000,002 | one top edit |
| DiffMatchPatch | 1,000,000 | SideBySide | Measured | 194.522 | 244.055 | 142,778,187 | 1,121,542,144 | 1,000,002 | one top edit |
| DiffPlex | 10 | Combined | Measured | 0.064 | 0.367 | 1,482 | 5,296,128 | 11 | one top edit |
| DiffPlex | 10 | SideBySide | Measured | 0.032 | 0.389 | 1,501 | 5,300,224 | 10 | one top edit |
| DiffPlex | 100 | Combined | Measured | 0.083 | 0.346 | 12,374 | 4,870,144 | 101 | one top edit |
| DiffPlex | 100 | SideBySide | Measured | 0.059 | 0.386 | 13,653 | 5,296,128 | 100 | one top edit |
| DiffPlex | 1,000 | Combined | Measured | 0.433 | 0.509 | 123,076 | 5,292,032 | 1,001 | one top edit |
| DiffPlex | 1,000 | SideBySide | Measured | 0.467 | 0.567 | 136,955 | 5,296,128 | 1,000 | one top edit |
| DiffPlex | 10,000 | Combined | Measured | 3.238 | 2.553 | 1,248,078 | 8,372,528 | 10,001 | one top edit |
| DiffPlex | 10,000 | SideBySide | Measured | 3.174 | 2.894 | 1,387,957 | 11,274,288 | 10,000 | one top edit |
| DiffPlex | 100,000 | Combined | Measured | 29.583 | 22.459 | 12,678,080 | 77,034,368 | 100,001 | one top edit |
| DiffPlex | 100,000 | SideBySide | Measured | 44.651 | 22.973 | 14,077,959 | 116,858,880 | 100,000 | one top edit |
| DiffPlex | 1,000,000 | Combined | Measured | 468.714 | 184.096 | 128,778,082 | 999,530,496 | 1,000,001 | one top edit |
| DiffPlex | 1,000,000 | SideBySide | Measured | 553.733 | 218.779 | 142,777,961 | 1,273,421,824 | 1,000,000 | one top edit |
| Git CLI | 10 | Combined | Measured | 43.215 | 0.345 | 886 | 5,292,032 | 7 | one top edit |
| Git CLI | 10 | SideBySide | Measured | 43.974 | 0.345 | 872 | 13,725,696 | 7 | one top edit |
| Git CLI | 100 | Combined | Measured | 43.741 | 0.33 | 886 | 13,844,480 | 7 | one top edit |
| Git CLI | 100 | SideBySide | Measured | 49.837 | 0.324 | 872 | 12,963,840 | 7 | one top edit |
| Git CLI | 1,000 | Combined | Measured | 47.671 | 0.336 | 886 | 13,656,064 | 7 | one top edit |
| Git CLI | 1,000 | SideBySide | Measured | 57.155 | 0.349 | 872 | 13,561,856 | 7 | one top edit |
| Git CLI | 10,000 | Combined | Measured | 118.921 | 0.34 | 886 | 17,268,736 | 7 | one top edit |
| Git CLI | 10,000 | SideBySide | Measured | 49.002 | 0.342 | 872 | 17,231,872 | 7 | one top edit |
| Git CLI | 100,000 | Combined | Measured | 68.235 | 0.309 | 886 | 54,145,024 | 7 | one top edit |
| Git CLI | 100,000 | SideBySide | Measured | 67.912 | 0.292 | 872 | 53,907,456 | 7 | one top edit |
| Git CLI | 1,000,000 | Combined | Measured | 255.36 | 0.282 | 886 | 488,890,368 | 7 | one top edit |
| Git CLI | 1,000,000 | SideBySide | Measured | 255.621 | 0.293 | 872 | 488,910,848 | 7 | one top edit |
| LovelyGit Prototype | 10 | Combined | Measured | 0.004 | 0.018 | 517 | 4,866,048 | 11 | one top edit |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.004 | 0.019 | 505 | 13,254,656 | 10 | one top edit |
| LovelyGit Prototype | 100 | Combined | Measured | 0.004 | 0.029 | 3,129 | 10,117,120 | 101 | one top edit |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.004 | 0.043 | 3,117 | 13,344,768 | 100 | one top edit |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.004 | 0.081 | 31,031 | 5,349,376 | 1,001 | one top edit |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.004 | 0.078 | 31,019 | 5,349,376 | 1,000 | one top edit |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.004 | 0.565 | 328,033 | 5,353,472 | 10,001 | one top edit |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.003 | 0.603 | 328,021 | 4,866,048 | 10,000 | one top edit |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.004 | 5.695 | 3,478,035 | 64,131,072 | 100,001 | one top edit |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.004 | 5.828 | 3,478,023 | 67,796,992 | 100,000 | one top edit |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.004 | 58.713 | 36,778,037 | 541,569,024 | 1,000,001 | one top edit |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.004 | 58.576 | 36,778,025 | 537,448,448 | 1,000,000 | one top edit |
| MyersDiff | 10 | Combined | Measured | 0.16 | 0.382 | 1,483 | 4,812,800 | 11 | one top edit |
| MyersDiff | 10 | SideBySide | Measured | 0.145 | 0.443 | 1,609 | 5,300,224 | 11 | one top edit |
| MyersDiff | 100 | Combined | Measured | 0.167 | 0.375 | 12,375 | 5,296,128 | 101 | one top edit |
| MyersDiff | 100 | SideBySide | Measured | 0.164 | 0.405 | 13,761 | 5,296,128 | 101 | one top edit |
| MyersDiff | 1,000 | Combined | Measured | 0.395 | 0.62 | 123,077 | 5,292,032 | 1,001 | one top edit |
| MyersDiff | 1,000 | SideBySide | Measured | 0.278 | 0.667 | 137,063 | 5,296,128 | 1,001 | one top edit |
| MyersDiff | 10,000 | Combined | Measured | 1.372 | 2.559 | 1,248,079 | 8,180,656 | 10,001 | one top edit |
| MyersDiff | 10,000 | SideBySide | Measured | 1.37 | 2.678 | 1,388,065 | 9,020,568 | 10,001 | one top edit |
| MyersDiff | 100,000 | Combined | Measured | 14.391 | 19.485 | 12,678,081 | 72,838,456 | 100,001 | one top edit |
| MyersDiff | 100,000 | SideBySide | Measured | 14.194 | 21.21 | 14,078,067 | 81,238,376 | 100,001 | one top edit |
| MyersDiff | 1,000,000 | Combined | Measured | 288.331 | 204.402 | 128,778,083 | 1,051,852,800 | 1,000,001 | one top edit |
| MyersDiff | 1,000,000 | SideBySide | Measured | 294.151 | 245.471 | 142,778,069 | 1,151,782,912 | 1,000,001 | one top edit |
| NGitDiff Histogram | 10 | Combined | Measured | 0.09 | 0.34 | 1,492 | 5,296,128 | 11 | one top edit |
| NGitDiff Histogram | 10 | SideBySide | Measured | 0.094 | 0.362 | 1,618 | 5,296,128 | 11 | one top edit |
| NGitDiff Histogram | 100 | Combined | Measured | 0.118 | 0.369 | 12,384 | 5,296,128 | 101 | one top edit |
| NGitDiff Histogram | 100 | SideBySide | Measured | 0.13 | 0.387 | 13,770 | 5,300,224 | 101 | one top edit |
| NGitDiff Histogram | 1,000 | Combined | Measured | 0.355 | 0.547 | 123,086 | 5,296,128 | 1,001 | one top edit |
| NGitDiff Histogram | 1,000 | SideBySide | Measured | 0.372 | 0.622 | 137,072 | 5,296,128 | 1,001 | one top edit |
| NGitDiff Histogram | 10,000 | Combined | Measured | 2.853 | 2.666 | 1,248,088 | 13,007,800 | 10,001 | one top edit |
| NGitDiff Histogram | 10,000 | SideBySide | Measured | 2.76 | 2.719 | 1,388,074 | 13,287,776 | 10,001 | one top edit |
| NGitDiff Histogram | 100,000 | Combined | Measured | 35.669 | 19.881 | 12,678,090 | 71,782,240 | 100,001 | one top edit |
| NGitDiff Histogram | 100,000 | SideBySide | Measured | 33.783 | 21.925 | 14,078,076 | 80,182,152 | 100,001 | one top edit |
| NGitDiff Histogram | 1,000,000 | Combined | Measured | 529.614 | 180.112 | 128,778,092 | 1,034,428,416 | 1,000,001 | one top edit |
| NGitDiff Histogram | 1,000,000 | SideBySide | Measured | 527.416 | 236.549 | 142,778,078 | 1,006,498,800 | 1,000,001 | one top edit |
| NGitDiff Myers | 10 | Combined | Measured | 0.092 | 0.326 | 1,488 | 4,812,800 | 11 | one top edit |
| NGitDiff Myers | 10 | SideBySide | Measured | 0.108 | 0.375 | 1,614 | 4,648,960 | 11 | one top edit |
| NGitDiff Myers | 100 | Combined | Measured | 0.12 | 0.382 | 12,380 | 5,296,128 | 101 | one top edit |
| NGitDiff Myers | 100 | SideBySide | Measured | 0.119 | 0.377 | 13,766 | 5,296,128 | 101 | one top edit |
| NGitDiff Myers | 1,000 | Combined | Measured | 0.356 | 0.538 | 123,082 | 5,296,128 | 1,001 | one top edit |
| NGitDiff Myers | 1,000 | SideBySide | Measured | 0.408 | 0.627 | 137,068 | 5,296,128 | 1,001 | one top edit |
| NGitDiff Myers | 10,000 | Combined | Measured | 3.267 | 2.811 | 1,248,084 | 13,007,720 | 10,001 | one top edit |
| NGitDiff Myers | 10,000 | SideBySide | Measured | 2.755 | 2.659 | 1,388,070 | 13,287,696 | 10,001 | one top edit |
| NGitDiff Myers | 100,000 | Combined | Measured | 32.063 | 19.14 | 12,678,086 | 71,779,096 | 100,001 | one top edit |
| NGitDiff Myers | 100,000 | SideBySide | Measured | 33.575 | 20.872 | 14,078,072 | 80,181,800 | 100,001 | one top edit |
| NGitDiff Myers | 1,000,000 | Combined | Measured | 526.986 | 187.945 | 128,778,088 | 1,016,754,176 | 1,000,001 | one top edit |
| NGitDiff Myers | 1,000,000 | SideBySide | Measured | 537.323 | 233.611 | 142,778,074 | 1,120,731,136 | 1,000,001 | one top edit |
| spkl.Diffs | 10 | Combined | Measured | 0.018 | 0.479 | 1,484 | 5,300,224 | 11 | one top edit |
| spkl.Diffs | 10 | SideBySide | Measured | 0.017 | 0.384 | 1,610 | 5,300,224 | 11 | one top edit |
| spkl.Diffs | 100 | Combined | Measured | 0.027 | 0.371 | 12,376 | 5,296,128 | 101 | one top edit |
| spkl.Diffs | 100 | SideBySide | Measured | 0.03 | 0.363 | 13,762 | 5,296,128 | 101 | one top edit |
| spkl.Diffs | 1,000 | Combined | Measured | 0.164 | 0.528 | 123,078 | 5,296,128 | 1,001 | one top edit |
| spkl.Diffs | 1,000 | SideBySide | Measured | 0.153 | 0.579 | 137,064 | 5,296,128 | 1,001 | one top edit |
| spkl.Diffs | 10,000 | Combined | Measured | 1.267 | 2.801 | 1,248,080 | 8,170,192 | 10,001 | one top edit |
| spkl.Diffs | 10,000 | SideBySide | Measured | 1.238 | 2.781 | 1,388,066 | 9,010,112 | 10,001 | one top edit |
| spkl.Diffs | 100,000 | Combined | Measured | 13.14 | 19.445 | 12,678,082 | 72,827,952 | 100,001 | one top edit |
| spkl.Diffs | 100,000 | SideBySide | Measured | 13.629 | 21.857 | 14,078,068 | 81,227,864 | 100,001 | one top edit |
| spkl.Diffs | 1,000,000 | Combined | Measured | 273.139 | 183.611 | 128,778,084 | 1,010,675,712 | 1,000,001 | one top edit |
| spkl.Diffs | 1,000,000 | SideBySide | Measured | 304.271 | 246.702 | 142,778,070 | 1,014,884,552 | 1,000,001 | one top edit |

## Scenario: repeated

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| CSharpDiff | 10 | Combined | Measured | 0.188 | 0.36 | 602 | 5,296,128 | 3 | repeated lines |
| CSharpDiff | 10 | SideBySide | Measured | 0.157 | 0.338 | 688 | 5,300,224 | 3 | repeated lines |
| CSharpDiff | 100 | Combined | Measured | 0.217 | 0.343 | 1,562 | 5,296,128 | 3 | repeated lines |
| CSharpDiff | 100 | SideBySide | Measured | 0.181 | 0.311 | 2,608 | 5,599,232 | 3 | repeated lines |
| CSharpDiff | 1,000 | Combined | Measured | 0.586 | 0.389 | 14,103 | 5,300,224 | 30 | repeated lines |
| CSharpDiff | 1,000 | SideBySide | Measured | 0.65 | 0.448 | 24,581 | 5,296,128 | 30 | repeated lines |
| CSharpDiff | 10,000 | Combined | Measured | 14.369 | 0.593 | 139,945 | 22,915,544 | 300 | repeated lines |
| CSharpDiff | 10,000 | SideBySide | Measured | 14.127 | 0.673 | 244,743 | 23,125,144 | 300 | repeated lines |
| CSharpDiff | 100,000 | Combined | ReusedSlow | 16791.883 | 2.357 | 1,402,847 | 152,182,784 | 3,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| CSharpDiff | 100,000 | SideBySide | ReusedSlow | 17033.642 | 4.412 | 2,450,845 | 155,803,648 | 3,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| CSharpDiff | 1,000,000 | Combined | ReusedSlow |  |  |  | 581,627,904 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| CSharpDiff | 1,000,000 | SideBySide | ReusedSlow |  |  |  | 574,566,400 | 0 | Reused previous TimedOut result: Exceeded 30000 ms timeout. |
| Diff4Net | 10 | Combined | Measured | 0.021 | 0.369 | 1,406 | 5,296,128 | 11 | repeated lines |
| Diff4Net | 10 | SideBySide | Measured | 0.022 | 0.34 | 1,460 | 5,300,224 | 11 | repeated lines |
| Diff4Net | 100 | Combined | Measured | 0.039 | 0.37 | 11,638 | 5,296,128 | 101 | repeated lines |
| Diff4Net | 100 | SideBySide | Measured | 0.044 | 0.386 | 12,292 | 5,296,128 | 101 | repeated lines |
| Diff4Net | 1,000 | Combined | Measured | 0.207 | 0.552 | 116,787 | 5,296,128 | 1,010 | repeated lines |
| Diff4Net | 1,000 | SideBySide | Measured | 0.2 | 0.543 | 123,327 | 5,496,832 | 1,010 | repeated lines |
| Diff4Net | 10,000 | Combined | Measured | 3.78 | 2.514 | 1,186,349 | 14,401,248 | 10,100 | repeated lines |
| Diff4Net | 10,000 | SideBySide | Measured | 3.594 | 2.527 | 1,251,749 | 14,532,048 | 10,100 | repeated lines |
| Diff4Net | 100,000 | Combined | Measured | 523.172 | 18.902 | 12,062,851 | 252,570,288 | 101,000 | repeated lines |
| Diff4Net | 100,000 | SideBySide | Measured | 549.54 | 20.078 | 12,716,851 | 286,547,968 | 101,000 | repeated lines |
| Diff4Net | 1,000,000 | Combined | ReusedSlow |  |  |  | 4,315,074,560 | 0 | Reused previous MemoryLimit result: Exceeded 4 GB memory limit. |
| Diff4Net | 1,000,000 | SideBySide | ReusedSlow |  |  |  | 4,325,720,064 | 0 | Reused previous MemoryLimit result: Exceeded 4 GB memory limit. |
| DiffMatchPatch | 10 | Combined | Measured | 1.89 | 0.341 | 1,515 | 5,296,128 | 12 | repeated lines |
| DiffMatchPatch | 10 | SideBySide | Measured | 3.186 | 0.335 | 1,569 | 4,812,800 | 12 | repeated lines |
| DiffMatchPatch | 100 | Combined | Measured | 1.251 | 0.35 | 11,749 | 5,296,128 | 102 | repeated lines |
| DiffMatchPatch | 100 | SideBySide | Measured | 1.164 | 0.318 | 12,403 | 5,300,224 | 102 | repeated lines |
| DiffMatchPatch | 1,000 | Combined | Measured | 1.371 | 0.538 | 118,853 | 5,296,128 | 1,029 | repeated lines |
| DiffMatchPatch | 1,000 | SideBySide | Measured | 1.611 | 0.597 | 125,366 | 5,296,128 | 1,029 | repeated lines |
| DiffMatchPatch | 10,000 | Combined | Measured | 4.451 | 2.316 | 1,208,343 | 13,761,776 | 10,299 | repeated lines |
| DiffMatchPatch | 10,000 | SideBySide | Measured | 4.583 | 2.583 | 1,273,446 | 13,891,984 | 10,299 | repeated lines |
| DiffMatchPatch | 100,000 | Combined | Measured | 44.88 | 24.447 | 12,287,743 | 88,580,096 | 102,999 | repeated lines |
| DiffMatchPatch | 100,000 | SideBySide | Measured | 44.866 | 26.377 | 12,938,746 | 76,972,032 | 102,999 | repeated lines |
| DiffMatchPatch | 1,000,000 | Combined | ReusedSlow | 1676.485 | 187.879 | 124,926,743 | 671,522,816 | 1,029,999 | Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| DiffMatchPatch | 1,000,000 | SideBySide | ReusedSlow | 1708.694 | 205.871 | 131,436,746 | 669,282,304 | 1,029,999 | Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| DiffPlex | 10 | Combined | Measured | 0.068 | 0.344 | 1,406 | 11,522,048 | 11 | repeated lines |
| DiffPlex | 10 | SideBySide | Measured | 0.027 | 0.396 | 1,353 | 5,300,224 | 10 | repeated lines |
| DiffPlex | 100 | Combined | Measured | 0.056 | 0.362 | 11,638 | 5,296,128 | 101 | repeated lines |
| DiffPlex | 100 | SideBySide | Measured | 0.056 | 0.355 | 12,185 | 5,296,128 | 100 | repeated lines |
| DiffPlex | 1,000 | Combined | Measured | 0.404 | 0.523 | 116,787 | 5,300,224 | 1,010 | repeated lines |
| DiffPlex | 1,000 | SideBySide | Measured | 0.408 | 0.568 | 122,248 | 5,300,224 | 1,000 | repeated lines |
| DiffPlex | 10,000 | Combined | Measured | 2.495 | 2.356 | 1,186,349 | 10,809,952 | 10,100 | repeated lines |
| DiffPlex | 10,000 | SideBySide | Measured | 2.995 | 2.812 | 1,240,950 | 12,223,544 | 10,000 | repeated lines |
| DiffPlex | 100,000 | Combined | Measured | 29.086 | 19.699 | 12,062,851 | 71,281,664 | 101,000 | repeated lines |
| DiffPlex | 100,000 | SideBySide | Measured | 42.202 | 20.815 | 12,608,852 | 92,839,936 | 100,000 | repeated lines |
| DiffPlex | 1,000,000 | Combined | ReusedSlow | 1052.429 | 175.515 | 122,636,853 | 657,145,856 | 1,010,000 | Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| DiffPlex | 1,000,000 | SideBySide | ReusedSlow | 1206.415 | 194.237 | 128,096,854 | 1,036,079,104 | 1,000,000 | Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| Git CLI | 10 | Combined | Measured | 45.31 | 0.348 | 886 | 12,824,576 | 7 | repeated lines |
| Git CLI | 10 | SideBySide | Measured | 63.036 | 0.331 | 872 | 12,840,960 | 7 | repeated lines |
| Git CLI | 100 | Combined | Measured | 54.84 | 0.339 | 886 | 12,959,744 | 7 | repeated lines |
| Git CLI | 100 | SideBySide | Measured | 47.68 | 0.352 | 872 | 12,951,552 | 7 | repeated lines |
| Git CLI | 1,000 | Combined | Measured | 48.503 | 0.346 | 8,370 | 13,582,336 | 79 | repeated lines |
| Git CLI | 1,000 | SideBySide | Measured | 45.121 | 0.348 | 8,212 | 13,565,952 | 79 | repeated lines |
| Git CLI | 10,000 | Combined | Measured | 66.76 | 0.453 | 84,650 | 18,853,888 | 799 | repeated lines |
| Git CLI | 10,000 | SideBySide | Measured | 62.901 | 0.443 | 83,052 | 18,472,960 | 799 | repeated lines |
| Git CLI | 100,000 | Combined | Measured | 73.86 | 1.642 | 861,850 | 35,676,160 | 7,999 | repeated lines |
| Git CLI | 100,000 | SideBySide | Measured | 74.486 | 1.72 | 845,852 | 40,292,352 | 7,999 | repeated lines |
| Git CLI | 1,000,000 | Combined | Measured | 314.173 | 12.209 | 8,777,850 | 488,853,504 | 79,999 | repeated lines |
| Git CLI | 1,000,000 | SideBySide | Measured | 315.441 | 12.432 | 8,617,852 | 506,531,840 | 79,999 | repeated lines |
| LovelyGit Prototype | 10 | Combined | Measured | 0.003 | 0.018 | 441 | 5,148,672 | 11 | repeated lines |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.002 | 0.016 | 429 | 5,427,200 | 10 | repeated lines |
| LovelyGit Prototype | 100 | Combined | Measured | 0.004 | 0.028 | 2,393 | 5,132,288 | 101 | repeated lines |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.006 | 0.037 | 2,381 | 4,866,048 | 100 | repeated lines |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.003 | 0.081 | 23,941 | 11,710,464 | 1,010 | repeated lines |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.003 | 0.092 | 23,803 | 8,454,144 | 1,000 | repeated lines |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.004 | 0.582 | 257,493 | 5,353,472 | 10,100 | repeated lines |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.003 | 0.565 | 256,095 | 5,353,472 | 10,000 | repeated lines |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.004 | 5.895 | 2,773,895 | 72,384,512 | 101,000 | repeated lines |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.004 | 5.839 | 2,759,897 | 77,303,808 | 100,000 | repeated lines |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.004 | 61.699 | 29,746,897 | 552,681,472 | 1,010,000 | repeated lines |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.003 | 58.539 | 29,606,899 | 561,512,448 | 1,000,000 | repeated lines |
| MyersDiff | 10 | Combined | Measured | 0.15 | 0.362 | 1,407 | 5,292,032 | 11 | repeated lines |
| MyersDiff | 10 | SideBySide | Measured | 0.151 | 0.354 | 1,461 | 5,296,128 | 11 | repeated lines |
| MyersDiff | 100 | Combined | Measured | 0.159 | 0.38 | 11,639 | 5,300,224 | 101 | repeated lines |
| MyersDiff | 100 | SideBySide | Measured | 0.17 | 0.408 | 12,293 | 5,296,128 | 101 | repeated lines |
| MyersDiff | 1,000 | Combined | Measured | 0.298 | 0.594 | 116,788 | 4,648,960 | 1,010 | repeated lines |
| MyersDiff | 1,000 | SideBySide | Measured | 0.306 | 0.598 | 123,328 | 5,296,128 | 1,010 | repeated lines |
| MyersDiff | 10,000 | Combined | Measured | 1.636 | 2.274 | 1,186,350 | 8,390,272 | 10,100 | repeated lines |
| MyersDiff | 10,000 | SideBySide | Measured | 1.694 | 2.454 | 1,251,750 | 8,917,072 | 10,100 | repeated lines |
| MyersDiff | 100,000 | Combined | Measured | 36.008 | 19.171 | 12,062,852 | 73,401,944 | 101,000 | repeated lines |
| MyersDiff | 100,000 | SideBySide | Measured | 37.363 | 21.617 | 12,716,852 | 110,350,336 | 101,000 | repeated lines |
| MyersDiff | 1,000,000 | Combined | ReusedSlow | 5165.227 | 224.07 | 122,636,854 | 813,715,472 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| MyersDiff | 1,000,000 | SideBySide | ReusedSlow | 5202.957 | 228.675 | 129,176,854 | 860,770,304 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| NGitDiff Histogram | 10 | Combined | Measured | 0.093 | 0.368 | 1,416 | 5,296,128 | 11 | repeated lines |
| NGitDiff Histogram | 10 | SideBySide | Measured | 0.1 | 0.393 | 1,470 | 5,689,344 | 11 | repeated lines |
| NGitDiff Histogram | 100 | Combined | Measured | 0.119 | 0.371 | 11,648 | 5,296,128 | 101 | repeated lines |
| NGitDiff Histogram | 100 | SideBySide | Measured | 0.115 | 0.336 | 12,302 | 5,296,128 | 101 | repeated lines |
| NGitDiff Histogram | 1,000 | Combined | Measured | 0.744 | 0.564 | 116,797 | 5,296,128 | 1,010 | repeated lines |
| NGitDiff Histogram | 1,000 | SideBySide | Measured | 0.725 | 0.532 | 123,337 | 5,296,128 | 1,010 | repeated lines |
| NGitDiff Histogram | 10,000 | Combined | Measured | 4.988 | 2.559 | 1,186,359 | 12,798,648 | 10,100 | repeated lines |
| NGitDiff Histogram | 10,000 | SideBySide | Measured | 4.84 | 2.632 | 1,251,759 | 12,929,448 | 10,100 | repeated lines |
| NGitDiff Histogram | 100,000 | Combined | Measured | 156.963 | 21.184 | 12,062,861 | 69,086,200 | 101,000 | repeated lines |
| NGitDiff Histogram | 100,000 | SideBySide | Measured | 155.73 | 21.795 | 12,716,861 | 74,357,016 | 101,000 | repeated lines |
| NGitDiff Histogram | 1,000,000 | Combined | ReusedSlow | 12518.29 | 194.643 | 122,636,863 | 722,710,528 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| NGitDiff Histogram | 1,000,000 | SideBySide | ReusedSlow | 11937.211 | 218.895 | 129,176,863 | 715,251,712 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| NGitDiff Myers | 10 | Combined | Measured | 0.093 | 0.334 | 1,412 | 5,300,224 | 11 | repeated lines |
| NGitDiff Myers | 10 | SideBySide | Measured | 0.101 | 0.377 | 1,466 | 5,296,128 | 11 | repeated lines |
| NGitDiff Myers | 100 | Combined | Measured | 0.114 | 0.336 | 11,644 | 5,296,128 | 101 | repeated lines |
| NGitDiff Myers | 100 | SideBySide | Measured | 0.115 | 0.348 | 12,298 | 5,296,128 | 101 | repeated lines |
| NGitDiff Myers | 1,000 | Combined | Measured | 0.658 | 0.779 | 116,793 | 4,648,960 | 1,010 | repeated lines |
| NGitDiff Myers | 1,000 | SideBySide | Measured | 0.404 | 0.557 | 123,333 | 5,296,128 | 1,010 | repeated lines |
| NGitDiff Myers | 10,000 | Combined | Measured | 4.671 | 2.401 | 1,186,355 | 12,629,544 | 10,100 | repeated lines |
| NGitDiff Myers | 10,000 | SideBySide | Measured | 4.593 | 2.659 | 1,251,755 | 12,760,344 | 10,100 | repeated lines |
| NGitDiff Myers | 100,000 | Combined | Measured | 151.164 | 19.287 | 12,062,857 | 69,084,744 | 101,000 | repeated lines |
| NGitDiff Myers | 100,000 | SideBySide | Measured | 150.82 | 20.401 | 12,716,857 | 74,355,376 | 101,000 | repeated lines |
| NGitDiff Myers | 1,000,000 | Combined | ReusedSlow | 11968.008 | 181.634 | 122,636,859 | 638,464,000 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| NGitDiff Myers | 1,000,000 | SideBySide | ReusedSlow | 12550.078 | 207.642 | 129,176,859 | 805,888,000 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| spkl.Diffs | 10 | Combined | Measured | 0.017 | 0.383 | 1,408 | 5,296,128 | 11 | repeated lines |
| spkl.Diffs | 10 | SideBySide | Measured | 0.016 | 0.39 | 1,462 | 5,296,128 | 11 | repeated lines |
| spkl.Diffs | 100 | Combined | Measured | 0.031 | 0.396 | 11,640 | 5,296,128 | 101 | repeated lines |
| spkl.Diffs | 100 | SideBySide | Measured | 0.032 | 0.395 | 12,294 | 5,296,128 | 101 | repeated lines |
| spkl.Diffs | 1,000 | Combined | Measured | 0.155 | 0.583 | 116,789 | 5,296,128 | 1,010 | repeated lines |
| spkl.Diffs | 1,000 | SideBySide | Measured | 0.171 | 0.595 | 123,329 | 5,296,128 | 1,010 | repeated lines |
| spkl.Diffs | 10,000 | Combined | Measured | 1.609 | 2.744 | 1,186,351 | 7,900,136 | 10,100 | repeated lines |
| spkl.Diffs | 10,000 | SideBySide | Measured | 1.705 | 2.604 | 1,251,751 | 8,427,600 | 10,100 | repeated lines |
| spkl.Diffs | 100,000 | Combined | Measured | 39.856 | 22.294 | 12,062,853 | 70,133,288 | 101,000 | repeated lines |
| spkl.Diffs | 100,000 | SideBySide | Measured | 39.487 | 21.68 | 12,716,853 | 104,779,776 | 101,000 | repeated lines |
| spkl.Diffs | 1,000,000 | Combined | ReusedSlow | 6549.724 | 185.666 | 122,636,855 | 822,176,992 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |
| spkl.Diffs | 1,000,000 | SideBySide | ReusedSlow | 6160.512 | 207.586 | 129,176,855 | 835,256,992 | 1,010,000 | Reused previous ReusedSlow result: Reused previous ReusedSlow result: Reused previous Measured result: repeated lines |

## Scenario: unicode-modified

| Candidate | Lines | View | Status | Diff ms | Serialize ms | Payload | Memory | Rows | Notes |
|---|---:|---|---|---:|---:|---:|---:|---:|---|
| LovelyGit Prototype | 10 | Combined | Measured | 0.004 | 0.018 | 495 | 5,353,472 | 11 | UTF-8 text with one edit |
| LovelyGit Prototype | 10 | SideBySide | Measured | 0.003 | 0.019 | 483 | 5,349,376 | 10 | UTF-8 text with one edit |
| LovelyGit Prototype | 100 | Combined | Measured | 0.003 | 0.03 | 2,928 | 5,349,376 | 101 | UTF-8 text with one edit |
| LovelyGit Prototype | 100 | SideBySide | Measured | 0.004 | 0.026 | 2,916 | 5,349,376 | 100 | UTF-8 text with one edit |
| LovelyGit Prototype | 1,000 | Combined | Measured | 0.004 | 0.09 | 29,931 | 5,070,848 | 1,001 | UTF-8 text with one edit |
| LovelyGit Prototype | 1,000 | SideBySide | Measured | 0.004 | 0.092 | 29,919 | 5,349,376 | 1,000 | UTF-8 text with one edit |
| LovelyGit Prototype | 10,000 | Combined | Measured | 0.004 | 0.65 | 326,934 | 21,381,120 | 10,001 | UTF-8 text with one edit |
| LovelyGit Prototype | 10,000 | SideBySide | Measured | 0.004 | 0.639 | 326,922 | 5,349,376 | 10,000 | UTF-8 text with one edit |
| LovelyGit Prototype | 100,000 | Combined | Measured | 0.004 | 6.724 | 3,566,937 | 83,410,944 | 100,001 | UTF-8 text with one edit |
| LovelyGit Prototype | 100,000 | SideBySide | Measured | 0.003 | 6.581 | 3,566,925 | 70,041,600 | 100,000 | UTF-8 text with one edit |
| LovelyGit Prototype | 1,000,000 | Combined | Measured | 0.004 | 65.903 | 38,666,940 | 551,755,776 | 1,000,001 | UTF-8 text with one edit |
| LovelyGit Prototype | 1,000,000 | SideBySide | Measured | 0.003 | 66.519 | 38,666,928 | 547,573,760 | 1,000,000 | UTF-8 text with one edit |

## Rejected Or Reference-Only Candidates

| Candidate | Reason |
|---|---|
| TextDiff.Sharp | Applies/processes unified diffs; docs recommend DiffPlex for generation. |
| ParseDiff | Unified diff parser, not a primary generator. |
| LibGit2Sharp | Native dependency and read-path policy risk; keep as future reference only. |
| google-diff-match-patch | Character/text sync API; not Git-style line hunk output. |

