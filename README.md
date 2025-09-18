<!--
Replace the placeholders below (VIDEO_ID, STORE_URL, WINGET_ID, image paths) when they’re ready.
This README is intentionally end‑user focused: simple, visual, friendly.
-->

<div align="center">

# WinWallhaven — Beautiful wallpapers for Windows 🖼️✨

Your fast, modern, and private way to explore, search, and set stunning wallpapers from wallhaven.cc on Windows 10/11.

<!-- Demo video (YouTube) -->
<a href="https://www.youtube.com/watch?v=VIDEO_ID" target="_blank">
	<img src="https://img.youtube.com/vi/VIDEO_ID/maxresdefault.jpg" alt="WinWallhaven demo video" style="max-width:100%; border-radius:12px;" />
</a>

<!-- Short GIF for a quick preview -->
<p>
	<img src="docs/media/winwallhaven-demo.gif" alt="Short animated preview" style="max-width:100%; border-radius:12px;" />
</p>

<!-- Microsoft Store badge -->
<p>
	<a href="STORE_URL" target="_blank">
		<img alt="Get it from Microsoft" src="https://get.microsoft.com/images/en-us%20dark.svg" height="64" />
	</a>
</p>

<p>
	<strong>Free</strong> • <strong>Open Source</strong> • <strong>No ads</strong> • <strong>Privacy‑friendly</strong>
</p>

</div>

## Why WinWallhaven? 💡

wallhaven.cc is one of the most loved wallpaper communities on the web—high‑quality images, thoughtful tagging, powerful filters, and a free API focused entirely on wallpapers. While there have been Windows apps for Wallhaven in the past, many are outdated, clunky, or built on legacy tech.

WinWallhaven is built for today’s Windows:
- Modern, native Windows app that’s fast and fluid
- Clean experience focused on discovery and personalization
- Built on modern Windows app technologies and packaged for the Microsoft Store

And because it’s open source, it’s transparent, community‑driven, and free—forever.

## Key features 🚀

- Explore the latest, random, and toplist wallpapers
- Powerful search with Wallhaven’s filters (resolutions, ratios, colors, tags, and purity)
- One‑click Set as Wallpaper or Set as Lock Screen
- Smooth, asynchronous image loading and browsing
- Light/Dark theme that matches your Windows style
- Local history of what you’ve viewed and set
- Open in browser, copy link, and other handy actions
- Keyboard and mouse friendly, great on both desktop and touch

## Screenshots 📸

Drop your screenshots into `docs/screenshots/` and replace the placeholders below.

### Home — Latest
![Latest feed](docs/screenshots/latest.png)

### Random
![Random feed](docs/screenshots/random.png)

### Toplist
![Toplist](docs/screenshots/toplist.png)

### Search (with filters)
![Search page with filters](docs/screenshots/search.png)

### Browsing a wallpaper
![Wallpaper details / actions](docs/screenshots/browse.png)

### History
![History of viewed/set wallpapers](docs/screenshots/history.png)

### Settings
![Settings](docs/screenshots/settings.png)

## Get the app 🛍️

- Microsoft Store: click the badge at the top or use the link: STORE_URL
- WinGet (Microsoft Store source):

```powershell
winget install --id WINGET_ID --source msstore
```

Replace `WINGET_ID` with the final identifier once published (for example: `PublisherName.WinWallhaven`).

## Security and privacy 🔒

- Built with modern Windows app technologies and packaged for the Microsoft Store, WinWallhaven only requests the permissions it needs (e.g., network access to Wallhaven’s API, changing the wallpaper and lock screen, and optional storage for saved images).
- When installed from the Microsoft Store, the package is digitally signed by Microsoft and delivered via the Store’s secure update channel.
- No ads, no trackers, no hidden data collection—period.
- Not affiliated with wallhaven.cc; this app simply uses their public, free API.

## How it works (at a glance) 🧠

- Connects to wallhaven.cc’s public API to fetch listings, details, and image links
- Renders fast, async image previews for smooth scrolling
- Applies wallpapers and lock screen images using modern Windows APIs

## Requirements ✅

- Windows 10 (version 1903+) or Windows 11
- Internet connection for browsing and downloading wallpapers

## Tips & tricks 💫

- Use search filters (resolution, ratio, color, and purity) to find exactly what you like
- Keep a tidy history to revisit your favorites
- Right‑click or use the context actions for quick “Set as Wallpaper/Lock Screen”

## FAQ ❓

- Is it free?
	- Yes—100% free and open source.
- Do I need a Wallhaven account?
	- No. You can browse freely. If you use API keys or advanced features later, we’ll document it here.
- Does it collect my data?
	- No. The app respects your privacy and only talks to wallhaven.cc to fetch wallpapers.
- Can I still use my own images as wallpaper?
	- Absolutely—this app focuses on Wallhaven, but it doesn’t remove any Windows features.

## Support 💬

If you find a bug or have a feature request, please open an issue on the GitHub “Issues” tab.

## Contribute ❤️

WinWallhaven is free and open source. If you’d like to help with translations, accessibility, UX, or testing, contributions are welcome—no code required. Developers can also contribute features and improvements.

## Credits 🙏

- Wallpapers and metadata provided by wallhaven.cc (via their public API)
- Microsoft Store distribution and digital signing by Microsoft

## License 📄

This project is licensed under the GNU General Public License v3.0 (GPL‑3.0). See `LICENSE.txt` for details.

---

Made with ❤️ for wallpaper lovers.