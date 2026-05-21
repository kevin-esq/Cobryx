import { defaultThemePreference, THEME_COOKIE } from "./config";

/**
 * Inline script string injected into <head> to apply the resolved theme
 * before paint, eliminating any flash of unstyled content (FOUC).
 *
 * Keep this self-contained: no external imports at runtime, no template
 * variables, and resilient to malformed cookies.
 */
export const themeInitScript = `(function(){try{var n="${THEME_COOKIE}";var d="${defaultThemePreference}";var m=document.cookie.match(new RegExp("(^|; )"+n+"=([^;]*)"));var v=m?decodeURIComponent(m[2]):d;if(v!=="light"&&v!=="dark"&&v!=="system"){v=d;}var prefers=window.matchMedia&&window.matchMedia("(prefers-color-scheme: dark)").matches;var dark=v==="dark"||(v==="system"&&prefers);var root=document.documentElement;if(dark){root.classList.add("dark");}else{root.classList.remove("dark");}root.style.colorScheme=dark?"dark":"light";}catch(e){}})();`;
