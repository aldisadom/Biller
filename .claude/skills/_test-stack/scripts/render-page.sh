#!/usr/bin/env bash
# Fetch a server-rendered BillerFrontEnd page and print what it shows: headings, bold labels,
# inputs with their values, the selected option of each dropdown, and table cells, in page order.
# Clicks (Blazor's SignalR circuit) are not possible this way; check those in a browser.
#
# Usage: render-page.sh <url>
#   e.g. render-page.sh http://localhost:8092/invoices/update/<id>
# Exit code: 0 when the page rendered, 1 for an HTTP error or the app's error box.
set -uo pipefail

URL="${1:-}"
[[ -n "$URL" ]] || { sed -n '2,8p' "${BASH_SOURCE[0]}" | sed 's/^# \{0,1\}//'; exit 2; }

PAGE="$(mktemp)"
trap 'rm -f "$PAGE"' EXIT
CODE="$(curl -s -o "$PAGE" -w '%{http_code}' "$URL")"
echo "HTTP $CODE $URL"
[[ "$CODE" == 200 ]] || exit 1

python3 -I - "$PAGE" <<'EOF'
import html, re, sys

page = open(sys.argv[1], encoding="utf-8").read()
# Pages with [StreamRendering] first send their "Loading..." markup, then the final render in
# <template blazor-component-id> blocks. Read only the final render. If none came, loading failed.
streamed = re.findall(r'<template blazor-component-id="[^"]*">(.*?)</template>', page, flags=re.S)
if streamed:
    page = "\n".join(streamed)
elif "Loading..." in page:
    print("STUCK at Loading...: the page's data didn't load. See: docker logs biller-test-frontend")
    sys.exit(1)
body = re.sub(r"<script.*?</script>|<style.*?</style>|<!--.*?-->", "", page, flags=re.S)
body = re.sub(r'<div id="blazor-error-ui".*?</div>', "", body, flags=re.S)

def text(s):
    return html.unescape(re.sub(r"<[^>]+>", "", s)).strip()

error = re.search(r'<div class="alert alert-danger"[^>]*>(.*?)</div>', body, flags=re.S)
if error:
    print("ERROR BOX:", re.sub(r"\s+", " ", text(error.group(1))))
    sys.exit(1)

controls = r"<input[^>]*>|<select[^>]*>.*?</select>"
tokens = r"<h[1-3][^>]*>.*?</h[1-3]>|<b>.*?</b>|" + controls + r"|<td[^>]*>.*?</td>"

def emit(tag):
    if tag.startswith("<input"):
        kind = re.search(r'type="([^"]*)"', tag)
        value = re.search(r'value="([^"]*)"', tag)
        print(f"  [{kind.group(1) if kind else 'text'}] {html.unescape(value.group(1)) if value else ''}")
    elif tag.startswith("<select"):
        options = re.findall(r"<option([^>]*)>(.*?)</option>", tag, flags=re.S)
        chosen = [text(label) for attrs, label in options if "selected" in attrs]
        print(f"  [select] {chosen[0] if chosen else '(none selected)'}  ({len(options)} options)")
    elif tag.startswith("<td"):
        inner = re.findall(controls, tag, flags=re.S)
        if inner:
            for control in inner:
                emit(control)
        elif text(tag):
            print(f"  | {text(tag)}")
    elif text(tag):
        print(text(tag))

for tag in re.findall(tokens, body, flags=re.S):
    emit(tag)
EOF
