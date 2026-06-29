using System.Globalization;
using System.Text;
using System.Text.Json;

namespace LovelyGit.DiffBenchmarks;

internal static class HtmlReportWriter
{
    public static void Write(
        string path,
        IReadOnlyList<BenchmarkResult> results,
        IReadOnlyList<BenchmarkCandidate> candidates,
        BenchmarkOptions options)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<!doctype html><html lang=\"en\"><head><meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine("<title>LovelyGit Diff Engine Benchmark</title>");
        AppendStyles(builder);
        builder.AppendLine("</head><body><main>");
        builder.AppendLine("<h1>Diff Engine Benchmark</h1>");
        builder.AppendLine($"<p>AOT run. Synthetic line counts: {Html(string.Join(", ", options.LineCounts.Select(FormatNumber)))}. Real Chromium files are included as additional rows.</p>");
        AppendControls(builder, options, results);
        builder.AppendLine("<section><h2>Fastest Library By Test</h2><div id=\"chart\" class=\"chart\"></div></section>");
        builder.AppendLine("<section><h2>LovelyGit Margin</h2><div id=\"margin\"></div></section>");
        builder.AppendLine("<section><h2>Pivot Table</h2><div id=\"table\"></div></section>");
        AppendCandidateTable(builder, candidates);
        AppendScript(builder, results, candidates);
        builder.AppendLine("</main></body></html>");
        using var writer = new StreamWriter(path, append: false, Encoding.UTF8);
        foreach (var chunk in builder.GetChunks())
        {
            writer.Write(chunk.Span);
        }
    }

    private static void AppendStyles(StringBuilder builder)
    {
        builder.AppendLine("<style>");
        builder.AppendLine(":root{color-scheme:dark;--bg:#09090b;--panel:#111113;--line:#27272a;--text:#f4f4f5;--muted:#a1a1aa;--good:#22c55e;--warn:#f59e0b;--bad:#ef4444;--accent:#38bdf8}");
        builder.AppendLine("body{margin:0;background:var(--bg);color:var(--text);font-family:Inter,Segoe UI,system-ui,sans-serif}main{max-width:1440px;margin:0 auto;padding:24px}h1{font-size:28px;margin:0 0 8px}h2{font-size:18px;margin:24px 0 12px}p{color:var(--muted)}");
        builder.AppendLine(".controls{display:flex;flex-wrap:wrap;gap:12px;padding:12px;background:var(--panel);border:1px solid var(--line);border-radius:8px;position:sticky;top:0;z-index:2}.field{display:grid;gap:4px}.field span{font-size:12px;color:var(--muted)}select{background:#18181b;color:var(--text);border:1px solid var(--line);border-radius:6px;padding:7px 10px}");
        builder.AppendLine("table{width:100%;border-collapse:collapse;background:var(--panel);border:1px solid var(--line)}th,td{border-bottom:1px solid var(--line);border-right:1px solid var(--line);padding:8px 10px;text-align:right;white-space:nowrap}th:first-child,td:first-child{text-align:left;position:sticky;left:0;background:var(--panel)}th{color:#d4d4d8;font-size:12px;text-transform:uppercase;letter-spacing:.04em}.best{color:var(--good);font-weight:700}.skip{color:var(--muted)}.fail{color:var(--bad)}");
        builder.AppendLine(".scroll{overflow:auto;border-radius:8px}.chart{display:grid;gap:8px;background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:12px}.barRow{display:grid;grid-template-columns:260px 1fr 110px;align-items:center;gap:12px}.barLabel{overflow:hidden;text-overflow:ellipsis;white-space:nowrap}.barTrack{height:14px;background:#18181b;border-radius:999px;overflow:hidden}.barFill{height:100%;background:linear-gradient(90deg,var(--accent),var(--good))}");
        builder.AppendLine(".meta{color:var(--muted);font-size:12px}.cards{display:grid;grid-template-columns:repeat(auto-fit,minmax(220px,1fr));gap:8px}.card{background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:10px}.card b{display:block;margin-bottom:4px}");
        builder.AppendLine("</style>");
    }

    private static void AppendControls(
        StringBuilder builder,
        BenchmarkOptions options,
        IReadOnlyList<BenchmarkResult> results)
    {
        builder.AppendLine("<div class=\"controls\">");
        AppendSelect(builder, "Metric", "metric", ["totalMs", "diffMs", "serializeMs", "payloadBytes", "memoryBytes", "rows"]);
        AppendSelect(builder, "View", "viewMode", ["Combined", "SideBySide"]);
        var lineCounts = results.Select(result => result.LineCount)
            .Concat(options.LineCounts)
            .Distinct()
            .Order()
            .Select(count => count.ToString(CultureInfo.InvariantCulture));
        AppendSelect(builder, "Lines", "lineCount", lineCounts);
        builder.AppendLine("</div>");
    }

    private static void AppendSelect(StringBuilder builder, string label, string id, IEnumerable<string> values)
    {
        builder.AppendLine($"<label class=\"field\"><span>{Html(label)}</span><select id=\"{Html(id)}\">");
        foreach (var value in values)
        {
            builder.AppendLine($"<option value=\"{Html(value)}\">{Html(Label(value))}</option>");
        }

        builder.AppendLine("</select></label>");
    }

    private static void AppendCandidateTable(StringBuilder builder, IReadOnlyList<BenchmarkCandidate> candidates)
    {
        builder.AppendLine("<section><h2>Candidates</h2><div class=\"cards\">");
        foreach (var candidate in candidates)
        {
            builder.AppendLine("<div class=\"card\">");
            builder.AppendLine($"<b>{Html(candidate.Name)}</b><span class=\"meta\">{Html(candidate.Category)} · max {FormatNumber(candidate.MaxLineCount)} lines</span>");
            builder.AppendLine("</div>");
        }

        builder.AppendLine("</div></section>");
    }

    private static void AppendScript(
        StringBuilder builder,
        IReadOnlyList<BenchmarkResult> results,
        IReadOnlyList<BenchmarkCandidate> candidates)
    {
        builder.AppendLine("<script>");
        builder.Append("const candidates=");
        builder.Append(JsonSerializer.Serialize(
            candidates.Select(candidate => candidate.Name).ToArray(),
            BenchmarkJsonContext.Default.StringArray));
        builder.AppendLine(";");
        builder.Append("const results=");
        builder.Append(JsonSerializer.Serialize(
            results.Select(ToReportRow).ToArray(),
            BenchmarkJsonContext.Default.ReportRowArray));
        builder.AppendLine(";");
        AppendClientScript(builder);
        builder.AppendLine("</script>");
    }

    private static ReportRow ToReportRow(BenchmarkResult result)
    {
        return new ReportRow(
            result.Candidate,
            result.CaseName,
            result.ViewMode,
            result.Status,
            result.Notes,
            result.LineCount,
            result.DiffMs,
            result.SerializeMs,
            result.PayloadBytes,
            result.MemoryBytes,
            result.Rows);
    }

    private static void AppendClientScript(StringBuilder builder)
    {
        builder.AppendLine("const byId=id=>document.getElementById(id);");
        builder.AppendLine("const fmt=new Intl.NumberFormat();");
        builder.AppendLine("const metricValue=(r,m)=>m==='totalMs'?r.diffMs+r.serializeMs:r[m];");
        builder.AppendLine("const metricLabel=m=>({totalMs:'total time (ms)',diffMs:'diff time (ms)',serializeMs:'serialize time (ms)',payloadBytes:'payload size',memoryBytes:'memory delta',rows:'output lines'}[m]);");
        builder.AppendLine("function render(){const metric=byId('metric').value,view=byId('viewMode').value,line=Number(byId('lineCount').value);const rows=results.filter(r=>r.viewMode===view&&r.lineCount===line);const tests=[...new Set(rows.map(r=>r.caseName))].sort();renderTable(rows,tests,metric);renderChart(rows,tests,metric);renderMargin(rows,tests,metric);}");
        builder.AppendLine("function renderTable(rows,tests,metric){let html=`<div class=\"meta\">Cells show ${metricLabel(metric)} for the selected line count and view.</div><div class=\"scroll\"><table><thead><tr><th>Test</th>`+candidates.map(c=>`<th>${c}</th>`).join('')+'</tr></thead><tbody>';for(const test of tests){const group=rows.filter(r=>r.caseName===test);const measured=group.filter(r=>r.status==='Measured');const best=Math.min(...measured.map(r=>metricValue(r,metric)));html+=`<tr><td>${test}</td>`;for(const c of candidates){const r=group.find(x=>x.candidate===c);html+=cell(r,metric,best);}html+='</tr>';}byId('table').innerHTML=html+'</tbody></table></div>';}");
        builder.AppendLine("function cell(r,metric,best){if(!r)return '<td class=\"skip\">not run</td>';const title=r?`status ${r.status}, diff ${format(r.diffMs,'diffMs')}, serialize ${format(r.serializeMs,'serializeMs')}, payload ${format(r.payloadBytes,'payloadBytes')}, memory ${format(r.memoryBytes,'memoryBytes')}, output ${format(r.rows,'rows')}, ${r.notes}`:'';if(r.status==='ReusedSlow'){const value=reusedValue(r,metric);return `<td class=\"skip\" title=\"${title}\">${value} (old)</td>`;}if(r.status!=='Measured'){const value=metric==='memoryBytes'?format(r.memoryBytes,'memoryBytes'):r.status;return `<td class=\"skip\" title=\"${title}\">${value}</td>`;}const v=metricValue(r,metric);const cls=v===best?'best':'';return `<td class=\"${cls}\" title=\"${title}\">${format(v,metric)}</td>`;}");
        builder.AppendLine("function renderChart(rows,tests,metric){const winners=tests.map(t=>{const group=rows.filter(r=>r.caseName===t&&r.status==='Measured');const best=group.sort((a,b)=>metricValue(a,metric)-metricValue(b,metric))[0];return best?{test:t,candidate:best.candidate,value:metricValue(best,metric)}:null;}).filter(Boolean);const max=Math.max(...winners.map(w=>w.value),1);byId('chart').innerHTML=winners.map(w=>`<div class=\"barRow\"><div class=\"barLabel\">${w.test} · ${w.candidate}</div><div class=\"barTrack\"><div class=\"barFill\" style=\"width:${Math.max(2,w.value/max*100)}%\"></div></div><div>${format(w.value,metric)}</div></div>`).join('');}");
        builder.AppendLine("function renderMargin(rows,tests,metric){const items=tests.map(t=>{const lg=rows.find(r=>r.caseName===t&&r.candidate==='LovelyGit Prototype'&&r.status==='Measured');const others=rows.filter(r=>r.caseName===t&&r.candidate!=='LovelyGit Prototype'&&r.status==='Measured').sort((a,b)=>metricValue(a,metric)-metricValue(b,metric));if(!lg||!others.length)return null;const other=others[0];const ratio=metricValue(other,metric)/Math.max(metricValue(lg,metric),0.001);return {test:t,other:other.candidate,ratio,lg:metricValue(lg,metric),otherValue:metricValue(other,metric)};}).filter(Boolean);byId('margin').innerHTML='<div class=\"meta\">Margin compares LovelyGit Prototype against the fastest other measured candidate, including Git CLI reference rows.</div><div class=\"scroll\"><table><thead><tr><th>Test</th><th>Best other</th><th>LovelyGit</th><th>Best other</th><th>Margin</th></tr></thead><tbody>'+items.map(i=>`<tr><td>${i.test}</td><td>${i.other}</td><td>${format(i.lg,metric)}</td><td>${format(i.otherValue,metric)}</td><td class=\"${i.ratio>=2?'best':i.ratio<1?'fail':'skip'}\">${i.ratio.toFixed(2)}x</td></tr>`).join('')+'</tbody></table></div>';}");
        builder.AppendLine("function format(v,metric){if(metric.endsWith('Bytes'))return bytes(v);if(metric==='rows')return `${fmt.format(v)} lines`;return `${v.toFixed(v<10?3:1)} ms`;}");
        builder.AppendLine("function reusedValue(r,metric){if(metric==='memoryBytes')return format(r.memoryBytes,metric);if(r.diffMs+r.serializeMs===0){if(r.notes.includes('TimedOut'))return 'Timed out';if(r.notes.includes('MemoryLimit'))return 'Memory limit';}return format(metricValue(r,metric),metric);}");
        builder.AppendLine("function bytes(v){const units=['B','KB','MB','GB'];let n=v,u=0;while(n>=1024&&u<units.length-1){n/=1024;u++;}return `${n.toFixed(n<10&&u>0?2:n<100&&u>0?1:0)} ${units[u]}`;}");
        builder.AppendLine("for(const id of ['metric','viewMode','lineCount'])byId(id).addEventListener('change',render);render();");
    }

    private static string FormatNumber(int value) => value.ToString("N0", CultureInfo.InvariantCulture);

    private static string Label(string value)
    {
        if (int.TryParse(value, out var number))
        {
            return FormatNumber(number);
        }

        return value switch
        {
            "totalMs" => "Total time",
            "diffMs" => "Diff time",
            "serializeMs" => "Serialize time",
            "payloadBytes" => "Payload bytes",
            "memoryBytes" => "Memory bytes",
            "rows" => "Rows",
            _ => value,
        };
    }

    private static string Html(string value) => System.Net.WebUtility.HtmlEncode(value);
}
