using System.Text;
using System.Text.Encodings.Web;
using Kentos.SharedKernel.Modules;

namespace Kentos.Infrastructure.OpenApi;

/// <summary>
/// Server-renders the API docs home page (<c>/docs</c>): a responsive, mobile-first card
/// grid of the enabled modules. Each card shows the module's inline SVG icon, display
/// name and version and links to that module's dedicated Scalar UI
/// (<c>/scalar/{slug}</c>); a featured "Tüm Modüller" card links to the combined Scalar
/// (<c>/scalar</c>). Everything is inline (no external assets) so it works behind any
/// reverse proxy, and a tiny progressive-enhancement filter narrows the grid as you type.
/// </summary>
public static class DocsHomePage
{
    private static readonly HtmlEncoder Html = HtmlEncoder.Default;

    // A neutral grid-of-squares glyph for the combined "all modules" card.
    private const string AllModulesIcon =
        """
        <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><rect width="7" height="7" x="3" y="3" rx="1"/><rect width="7" height="7" x="14" y="3" rx="1"/><rect width="7" height="7" x="14" y="14" rx="1"/><rect width="7" height="7" x="3" y="14" rx="1"/></svg>
        """;

    public static string Render(IReadOnlyList<IModule> modules)
    {
        var cards = new StringBuilder();
        cards.Append(Card("/scalar", AllModulesIcon, "Tüm Modüller", "Birleşik API referansı", "tümü", featured: true));
        foreach (var module in modules)
        {
            cards.Append(Card(
                $"/scalar/{Html.Encode(module.Slug)}",
                module.Icon,
                module.DisplayName,
                $"v{module.Version}",
                module.Slug,
                featured: false));
        }

        var count = modules.Count;
        var countLabel = count == 1 ? "1 modül" : $"{count} modül";

        return $$"""
        <!DOCTYPE html>
        <html lang="tr">
        <head>
        <meta charset="utf-8">
        <meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
        <meta name="color-scheme" content="light dark">
        <meta name="theme-color" content="#ffffff" media="(prefers-color-scheme: light)">
        <meta name="theme-color" content="#0b1120" media="(prefers-color-scheme: dark)">
        <title>Kentos API Dokümantasyonu</title>
        <style>
        :root {
          --bg: #f7f8fa; --bg-grad: radial-gradient(1200px 600px at 50% -10%, #eef2ff 0%, transparent 60%);
          --fg: #0f172a; --muted: #64748b; --card: #ffffff; --card-2: #fbfcfe;
          --border: #e6e9ef; --accent: #4f46e5; --accent-2: #7c3aed; --accent-soft: #eef2ff;
          --shadow: rgba(15,23,42,.08); --shadow-lg: rgba(15,23,42,.14); --ring: rgba(79,70,229,.35);
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --bg: #0b1120; --bg-grad: radial-gradient(1200px 600px at 50% -10%, #1e1b4b 0%, transparent 55%);
            --fg: #e2e8f0; --muted: #94a3b8; --card: #111827; --card-2: #0f1626;
            --border: #1f2937; --accent: #818cf8; --accent-2: #a78bfa; --accent-soft: #1e1b4b;
            --shadow: rgba(0,0,0,.35); --shadow-lg: rgba(0,0,0,.55); --ring: rgba(129,140,248,.4);
          }
        }
        * { box-sizing: border-box; }
        html { -webkit-text-size-adjust: 100%; }
        body {
          margin: 0; min-height: 100dvh; background: var(--bg); background-image: var(--bg-grad);
          background-repeat: no-repeat; color: var(--fg);
          font-family: ui-sans-serif, system-ui, -apple-system, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
          -webkit-font-smoothing: antialiased; text-rendering: optimizeLegibility;
        }
        .wrap {
          max-width: 1120px; margin: 0 auto;
          padding: clamp(36px, 7vw, 72px) clamp(16px, 5vw, 32px) 96px;
          padding-left: max(clamp(16px, 5vw, 32px), env(safe-area-inset-left));
          padding-right: max(clamp(16px, 5vw, 32px), env(safe-area-inset-right));
        }
        header { margin-bottom: clamp(24px, 4vw, 40px); }
        .brand {
          display: inline-flex; align-items: center; gap: 9px; font-weight: 700;
          font-size: 13px; letter-spacing: .04em; text-transform: uppercase; color: var(--accent);
        }
        .brand .mark {
          width: 22px; height: 22px; border-radius: 7px; display: grid; place-items: center; color: #fff;
          background: linear-gradient(135deg, var(--accent), var(--accent-2));
          box-shadow: 0 4px 12px var(--ring);
        }
        .brand .mark svg { width: 14px; height: 14px; }
        h1 { margin: 16px 0 10px; font-size: clamp(26px, 5.5vw, 38px); line-height: 1.08; letter-spacing: -.02em; }
        .sub { margin: 0; color: var(--muted); font-size: clamp(15px, 2.4vw, 17px); max-width: 60ch; }
        .toolbar { display: flex; flex-wrap: wrap; align-items: center; gap: 12px; margin-top: 22px; }
        .search {
          position: relative; flex: 1 1 260px; min-width: 0;
        }
        .search svg {
          position: absolute; left: 14px; top: 50%; transform: translateY(-50%);
          width: 18px; height: 18px; color: var(--muted); pointer-events: none;
        }
        .search input {
          width: 100%; padding: 12px 14px 12px 42px; font-size: 15px; color: var(--fg);
          background: var(--card); border: 1px solid var(--border); border-radius: 12px;
          outline: none; transition: border-color .15s, box-shadow .15s;
        }
        .search input::placeholder { color: var(--muted); }
        .search input:focus { border-color: var(--accent); box-shadow: 0 0 0 4px var(--ring); }
        .count { color: var(--muted); font-size: 13px; font-weight: 600; white-space: nowrap; }
        .grid {
          display: grid; gap: clamp(12px, 2vw, 18px); margin-top: clamp(20px, 3vw, 28px);
          grid-template-columns: repeat(auto-fill, minmax(min(100%, 250px), 1fr));
        }
        .card {
          position: relative; display: flex; flex-direction: column; gap: 14px;
          padding: clamp(18px, 3vw, 22px);
          background: var(--card); border: 1px solid var(--border); border-radius: 18px;
          text-decoration: none; color: inherit; box-shadow: 0 1px 2px var(--shadow);
          transition: transform .16s ease, box-shadow .16s ease, border-color .16s ease;
          -webkit-tap-highlight-color: transparent; overflow: hidden;
        }
        .card::after {
          content: ""; position: absolute; inset: 0; border-radius: inherit; pointer-events: none;
          background: linear-gradient(135deg, var(--accent), var(--accent-2)); opacity: 0; transition: opacity .16s;
          mix-blend-mode: normal;
        }
        .card:hover { transform: translateY(-3px); border-color: transparent; box-shadow: 0 16px 36px var(--shadow-lg); }
        .card:active { transform: translateY(-1px); }
        .card:focus-visible { outline: none; box-shadow: 0 0 0 4px var(--ring); }
        .card.featured { background: var(--card-2); border-style: dashed; }
        .icon {
          display: inline-flex; align-items: center; justify-content: center;
          width: 52px; height: 52px; border-radius: 14px;
          background: var(--accent-soft); color: var(--accent); flex: none;
        }
        .icon svg { width: 28px; height: 28px; }
        .featured .icon { background: linear-gradient(135deg, var(--accent), var(--accent-2)); color: #fff; }
        .title { font-size: clamp(16px, 2.4vw, 18px); font-weight: 650; letter-spacing: -.01em; }
        .meta { display: flex; align-items: center; gap: 8px; flex-wrap: wrap; color: var(--muted); font-size: 13px; }
        .chip {
          font-size: 11px; font-weight: 700; letter-spacing: .02em; padding: 3px 9px; border-radius: 999px;
          background: var(--accent-soft); color: var(--accent);
        }
        .arrow {
          margin-top: auto; display: inline-flex; align-items: center; gap: 6px;
          color: var(--accent); font-size: 13px; font-weight: 650;
        }
        .arrow svg { width: 15px; height: 15px; transition: transform .16s ease; }
        .card:hover .arrow svg { transform: translateX(3px); }
        .empty { display: none; color: var(--muted); padding: 28px 4px; font-size: 15px; }
        footer { margin-top: clamp(40px, 6vw, 56px); color: var(--muted); font-size: 13px; line-height: 1.9; }
        footer a { color: var(--accent); text-decoration: none; }
        footer a:hover { text-decoration: underline; }
        @media (prefers-reduced-motion: reduce) { * { transition: none !important; } .card:hover { transform: none; } }
        </style>
        </head>
        <body>
        <div class="wrap">
          <header>
            <div class="brand">
              <span class="mark"><svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="m8 3 4 8 5-5 5 15H2L8 3z"/></svg></span>
              Kentos
            </div>
            <h1>API Dokümantasyonu</h1>
            <p class="sub">Bir modül seçin; o modüle ait Scalar referansı açılır. Her modülün kendi OpenAPI dökümanı vardır.</p>
            <div class="toolbar">
              <label class="search">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="11" cy="11" r="8"/><path d="m21 21-4.3-4.3"/></svg>
                <input id="q" type="search" placeholder="Modül ara…" autocomplete="off" aria-label="Modül ara">
              </label>
              <span class="count" id="count">{{countLabel}}</span>
            </div>
          </header>
          <div class="grid" id="grid">
        {{cards}}  </div>
          <p class="empty" id="empty">Eşleşen modül yok.</p>
          <footer>
            Birleşik OpenAPI: <a href="/openapi/v1.json">/openapi/v1.json</a> · Modül manifestleri: <a href="/api/v1/metadata">/api/v1/metadata</a><br>
            Sağlık: <a href="/health/ready">/health/ready</a>
          </footer>
        </div>
        <script>
        (function () {
          var q = document.getElementById('q'), grid = document.getElementById('grid'),
              empty = document.getElementById('empty'), count = document.getElementById('count'),
              cards = Array.prototype.slice.call(grid.querySelectorAll('.card'));
          function norm(s){ return (s||'').toLocaleLowerCase('tr'); }
          q.addEventListener('input', function () {
            var term = norm(q.value.trim()), shown = 0;
            cards.forEach(function (c) {
              var hit = !term || norm(c.getAttribute('data-search')).indexOf(term) !== -1;
              c.style.display = hit ? '' : 'none';
              if (hit) shown++;
            });
            empty.style.display = shown ? 'none' : 'block';
            count.textContent = term ? (shown + ' sonuç') : count.getAttribute('data-default');
          });
          count.setAttribute('data-default', count.textContent);
        })();
        </script>
        </body>
        </html>
        """;
    }

    private static string Card(string href, string icon, string title, string meta, string badge, bool featured)
    {
        var cls = featured ? "card featured" : "card";
        var search = Html.Encode($"{title} {badge} {meta}");
        return $$"""
            <a class="{{cls}}" href="{{href}}" data-search="{{search}}">
              <div class="icon">{{icon}}</div>
              <div class="title">{{Html.Encode(title)}}</div>
              <div class="meta"><span class="chip">{{Html.Encode(badge)}}</span><span>{{Html.Encode(meta)}}</span></div>
              <span class="arrow">Referansı aç <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.4" stroke-linecap="round" stroke-linejoin="round"><path d="M5 12h14"/><path d="m12 5 7 7-7 7"/></svg></span>
            </a>

        """;
    }
}
