# SEO Audit Report — app.geta.pet

**Date:** 2026-03-14
**Business Type:** Utility PWA (niche local — Kansas City dog adoption)

---

## SEO Health Score: 30 / 100

| Category | Weight | Score | Weighted |
|---|---|---|---|
| Technical SEO | 22% | 51/100 | 11.2 |
| Content Quality | 23% | 31/100 | 7.1 |
| On-Page SEO | 20% | 18/100 | 3.6 |
| Schema / Structured Data | 10% | 0/100 | 0.0 |
| Performance (CWV) | 10% | 45/100 | 4.5 |
| AI Search Readiness | 10% | 14/100 | 1.4 |
| Images | 5% | 35/100 | 1.8 |
| **Total** | | | **30** |

**Context:** The low score reflects that this was built as a functional utility PWA, not a content-driven site. The technical and security foundations are solid. Most gaps are structural (CSR-only SPA) or simply missing metadata that takes minutes to add. The niche specificity ("Kansas City shelter dog notifications") is actually a GEO advantage — a low-competition space where small improvements yield outsized visibility.

---

## Visual Analysis

**Desktop (1280px):** Clean 4-column dog grid with warm orange header, tab bar (Available 304 / Recently Adopted 26), and sort/filter controls. Above-the-fold content is immediately useful. Strong visual design.

**Mobile (390px — iPhone 14):** Adapts to a clean 2-column grid. Tab bar, sort, and shelter filters all above the fold. No horizontal overflow. Bell icon for notifications is accessible.

**Console:** 0 errors, 1 warning (`apple-mobile-web-app-capable` deprecated).

---

## Findings by Priority

### Critical

**1. robots.txt and sitemap.xml are swallowed by the SPA fallback**
Both `/robots.txt` and `/sitemap.xml` return HTTP 200 with the HTML shell (same ETag as the homepage) because the `navigationFallback` exclude list in `staticwebapp.config.json` does not cover `.txt` or `.xml` extensions. Googlebot sees HTML at `robots.txt` and ignores it. No sitemap exists at all.

Fix: Add both to the `exclude` array in `staticwebapp.config.json`, then create static files in `app/ui/public/`.

**2. No `<meta name="description">` tag**
The HTML shell has OG/Twitter description tags but no standard `<meta name="description">`. Google generates SERP snippets from the meta description first; without it, and with a JS-rendered body, there is nothing to generate a snippet from.

Fix: One line in `app/ui/index.html`.

**3. No canonical tags on any URL**
Every URL on the site — `/`, `/dogs/12345/details`, `/robots.txt`, `/anything` — returns the same HTML with no `<link rel="canonical">`. Google sees every URL as a duplicate of every other. The navigationFallback makes this worse.

Fix: Add a static canonical to `index.html` for the root. Use React meta injection for detail pages.

**4. 100% client-side rendered — crawlers receive an empty body**
The HTML body is `<div id="root"></div>`. Every piece of user-facing content requires JavaScript execution. Bing, DuckDuckGo, Perplexity, ChatGPT's crawler, and ClaudeBot will index an empty page. Google renders JS but with a crawl-budget delay of days to weeks for secondary renders.

Fix: At minimum, add a `<noscript>` block or static description paragraph to `index.html` before the React mount point.

---

### High

**5. No JSON-LD structured data whatsoever**
Zero `<script type="application/ld+json">` blocks anywhere. No `WebSite`, `WebApplication`, `Organization`, or `BreadcrumbList`. Ready-to-use blocks are provided in the Schema section below.

**6. Render-blocking Google Fonts (LCP risk)**
`fonts.googleapis.com` CSS is loaded as a synchronous stylesheet in `<head>`. Even with `preconnect` hints, this blocks rendering until the font CSS downloads, parses, and fetches WOFF2 files from `fonts.gstatic.com`. `display=swap` prevents invisible text but not LCP delay.

Fix: Self-host Archivo Black and DM Sans WOFF2 files and preload them directly from `index.html`.

**7. Missing `Cache-Control: immutable` on hashed Vite assets**
`/assets/*` bundles have content-hash filenames but no explicit `immutable` cache header in `staticwebapp.config.json`. Repeat-visit LCP suffers without guaranteed cache hits on the JS bundle.

Fix: Add a route rule in `staticwebapp.config.json` (see Quick Wins).

**8. Dog card images missing `width`/`height` attributes (CLS risk)**
`app/ui/src/components/DogCard.tsx`: `<img src={dog.photoUrl} alt={...} loading="lazy" />` — no explicit dimensions. `aspect-ratio: 1/1` in CSS mitigates this in most cases but is not guaranteed before the containing CSS grid column width is computed.

Fix: Add `width="1" height="1"` attributes (or actual pixel values).

**9. No `llms.txt` file**
Perplexity and some ChatGPT crawl paths read `llms.txt` as an authoritative plain-text site description, bypassing the JS rendering problem entirely. Nothing exists at `https://app.geta.pet/llms.txt`.

Fix: Create `app/ui/public/llms.txt` with a 200–300 word description of the app, shelters covered, and how it works.

**10. No privacy policy**
The app requests browser notification permission. Sites requesting sensitive browser permissions without a privacy policy are candidates for reduced trustworthiness ratings per the Sept 2025 QRG update. No `/privacy` route, no contact info, no attribution.

Fix: A single static route with one paragraph disclosing data handling is sufficient.

**11. Generic `<title>` with no keyword or geo signal**
Current title: `get-a-pet` (lowercase). No city, no shelter names, no function descriptor.

Fix: Change to `Get-A-Pet — Kansas City Shelter Dog Alerts`. One line in `index.html`.

---

### Medium

**12. `og:image` and `twitter:image` use relative paths**
`content="/icon-512.png"` should be `content="https://app.geta.pet/icon-512.png"`. Social platform crawlers (Slack, iMessage, Discord) silently fail on relative image URLs, producing no preview on link shares.

**13. Missing `og:url` and `og:site_name`**
Add `<meta property="og:url" content="https://app.geta.pet/">` and `<meta property="og:site_name" content="Get-A-Pet">`.

**14. No Content-Security-Policy header**
The only missing major security header. Start with `Content-Security-Policy-Report-Only` to identify violations before enforcing.

**15. Deprecated `X-XSS-Protection: 1; mode=block` header**
Modern browsers ignore this IE-era header; some scanners flag it as a misconfiguration. Remove from `staticwebapp.config.json` `globalHeaders`.

**16. Detail page hero image missing width/height**
`app/ui/src/pages/DogDetailPage.tsx`: `<img src={dog.photoUrl} alt={...} />` — no `loading`, `width`, or `height`. This is the LCP candidate on the detail page and should not be lazy-loaded.

Fix: Add `width` and `height` attributes. Do not add `loading="lazy"` to an above-fold LCP element.

**17. `staleTime: 0` causes unnecessary refetch on tab focus (INP risk)**
`app/ui/src/hooks/useStatusQuery.ts`: `staleTime: 0` combined with `refetchOnWindowFocus: true` fires a new `/api/status` request every time the user returns to the tab, triggering a React re-render on the main thread exactly when users are most likely to interact.

Fix: Set `staleTime` to at least `30000` (matching poll interval).

**18. Card stagger animation delays above-fold paint**
`App.css` applies `card-in` with `animation-delay: calc(var(--i, 0) * 40ms)`. Cards start at `opacity: 0`. Consider removing the entrance animation for the first 4 cards so the LCP element paints immediately.

**19. All invalid URLs return HTTP 200 (soft 404 risk)**
`/nonexistent-path` returns HTTP 200 + the app shell instead of a 404. Googlebot may flag these as soft 404s or index them as duplicate pages.

**20. HSTS `max-age` below 1-year preload minimum**
Current: `max-age=10886400` (~126 days). HSTS preload list requires 31,536,000 (1 year). The `preload` directive is declared but the duration does not qualify. Increase the max-age or remove the `preload` directive.

---

### Low

**21. IndexNow not implemented**
The monitor runs continuously and new dogs are added frequently — exactly the IndexNow use case. Low priority until JS rendering gaps are resolved (no point submitting URLs crawlers can't read), but worth implementing after static content is in place.

**22. Service worker does not cache hashed JS/CSS bundles**
`sw.js` only precaches `/`, `/index.html`, and `/manifest.json`. Hashed bundles in `/assets/` are re-requested on every visit. Consider `vite-plugin-pwa` for automatic Workbox integration.

**23. Font-swap CLS from metric mismatch**
`display=swap` causes a layout shift when Archivo Black replaces the fallback font if metrics differ. Add `size-adjust`, `ascent-override`, `descent-override` to a `@font-face` fallback to match Archivo Black's metrics. Secondary to self-hosting.

**24. Badge icon not in PWA manifest**
`badge-72.png` is used in push notification delivery but not declared in `manifest.json` `icons`.

**25. `apple-mobile-web-app-capable` deprecation warning**
One console warning on load. Replace with `<meta name="mobile-web-app-capable" content="yes">`.

---

## Quick Wins — Do These First

These 9 changes require no architectural work and can be completed in under 2 hours combined.

| # | Change | File(s) | Effort |
|---|---|---|---|
| 1 | Add `"/robots.txt"`, `"/sitemap.xml"` to `navigationFallback` exclude | `staticwebapp.config.json` | 5 min |
| 2 | Create `robots.txt` + `sitemap.xml` in `app/ui/public/` | new files | 15 min |
| 3 | Add `<meta name="description">` + `<link rel="canonical">` | `index.html` | 5 min |
| 4 | Fix og/twitter image to absolute URLs, add `og:url` + `og:site_name` | `index.html` | 5 min |
| 5 | Add WebSite + WebApplication + Organization JSON-LD | `index.html` | 20 min |
| 6 | Create `llms.txt` | `app/ui/public/llms.txt` | 20 min |
| 7 | Remove `X-XSS-Protection`, add `/assets/*` immutable cache rule | `staticwebapp.config.json` | 10 min |
| 8 | Add `width`/`height` to `<img>` in `DogCard.tsx` and `DogDetailPage.tsx` | 2 files | 10 min |
| 9 | Update `<title>` to include city + function | `index.html` | 2 min |

---

## Schema — Ready-to-Use JSON-LD

Add these as `<script type="application/ld+json">` blocks in `app/ui/index.html` `<head>`. No JavaScript required — they are static markup parsed before JS executes.

**Block 1 — WebSite + WebApplication**

```json
{
  "@context": "https://schema.org",
  "@graph": [
    {
      "@type": "WebSite",
      "@id": "https://app.geta.pet/#website",
      "name": "Get-A-Pet",
      "description": "The fastest way from shelter to sofa.",
      "url": "https://app.geta.pet/",
      "inLanguage": "en-US"
    },
    {
      "@type": "WebApplication",
      "@id": "https://app.geta.pet/#webapp",
      "name": "Get-A-Pet",
      "description": "Real-time push notifications for newly available dogs at Kansas City shelters — KHS, KC Pet Project, and Great Plains SPCA.",
      "url": "https://app.geta.pet/",
      "applicationCategory": "LifestyleApplication",
      "operatingSystem": "Any",
      "offers": { "@type": "Offer", "price": "0", "priceCurrency": "USD" },
      "areaServed": { "@type": "City", "name": "Kansas City" }
    }
  ]
}
```

**Block 2 — Organization**

```json
{
  "@context": "https://schema.org",
  "@type": "Organization",
  "name": "Get-A-Pet",
  "url": "https://app.geta.pet/",
  "logo": "https://app.geta.pet/icon-512.png"
}
```

**Block 3 — BreadcrumbList (inject dynamically in `DogDetailPage.tsx` when dog data is loaded)**

```json
{
  "@context": "https://schema.org",
  "@type": "BreadcrumbList",
  "itemListElement": [
    { "@type": "ListItem", "position": 1, "name": "Available Dogs", "item": "https://app.geta.pet/" },
    { "@type": "ListItem", "position": 2, "name": "{dog.name}", "item": "https://app.geta.pet/dogs/{aid}/details" }
  ]
}
```

---

## Recommended `robots.txt`

```
User-agent: *
Allow: /
Disallow: /api/

Sitemap: https://app.geta.pet/sitemap.xml
```

## Recommended `sitemap.xml`

Dog detail pages should **not** be included — they are ephemeral (dogs get adopted), and churning URLs creates soft-404 signals. Static sitemap for the stable root only.

```xml
<?xml version="1.0" encoding="UTF-8"?>
<urlset xmlns="http://www.sitemaps.org/schemas/sitemap/0.9">
  <url>
    <loc>https://app.geta.pet/</loc>
    <lastmod>2026-03-14</lastmod>
  </url>
</urlset>
```

## Recommended `llms.txt`

```
# Get-A-Pet

Get-A-Pet (https://app.geta.pet) is a free web application that monitors Kansas City area animal shelters for newly listed dogs and delivers real-time browser push notifications the moment a new dog becomes available.

## What it does

- Monitors three Kansas City shelters: Kansas Humane Society (KHS), KC Pet Project, and Great Plains SPCA
- Sends web push notifications to subscribed users when a new dog is listed
- Displays a live grid of currently available dogs with photo, name, breed, age, size, and weight
- Allows filtering by shelter and sorting by age, name, or newest arrival
- Shows recently adopted dogs in a separate tab
- Works as an installable Progressive Web App (PWA) on any device

## How it works

The app polls shelter listings via petbridge.org, detects newly added animals, and pushes a browser notification to all subscribed users. Notifications include the dog's name and a link to their detail page. The monitoring runs continuously during daytime hours.

## Target audience

People in the Kansas City metro area who want to adopt a dog and need to act quickly when new animals become available at local shelters.

## Key URLs

- Homepage / dog grid: https://app.geta.pet/
- Dog detail page: https://app.geta.pet/dogs/{aid}/details
```

---

## Open Graph Fixes for `index.html`

Current (broken):
```html
<meta property="og:image" content="/icon-512.png">
<meta name="twitter:image" content="/icon-512.png">
```

Replace with:
```html
<meta property="og:image" content="https://app.geta.pet/icon-512.png">
<meta property="og:url" content="https://app.geta.pet/">
<meta property="og:site_name" content="Get-A-Pet">
<meta name="twitter:image" content="https://app.geta.pet/icon-512.png">
```

---

## `staticwebapp.config.json` Changes

**Add to `navigationFallback.exclude`:**
```json
"/robots.txt",
"/sitemap.xml"
```

**Add to `routes` array (cache headers for hashed assets):**
```json
{
  "route": "/assets/*",
  "headers": {
    "Cache-Control": "public, max-age=31536000, immutable"
  }
},
{
  "route": "/*.png",
  "headers": {
    "Cache-Control": "public, max-age=86400"
  }
}
```

**Remove from `globalHeaders`:**
```json
"X-XSS-Protection": "1; mode=block"
```

**Update HSTS in `globalHeaders`:**
```json
"Strict-Transport-Security": "max-age=31536000; includeSubDomains; preload"
```
