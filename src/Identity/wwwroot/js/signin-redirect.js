const url = document.querySelector("meta[http-equiv=refresh]").getAttribute("data-url");

// Defense in depth: the server (Redirect/Index.cshtml.cs) already validates
// this via Url.IsLocalUrl before rendering it, but that's a different file
// CodeQL's JS analysis can't see across - so this mirrors the same check
// here directly, rejecting anything that isn't a rooted, same-origin path
// (a scheme like javascript:/https:, or a protocol-relative //host, would
// both fail this and never navigate).
if (url && url.startsWith("/") && !url.startsWith("//")) {
    window.location.href = url;
}
