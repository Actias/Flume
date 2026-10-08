#!/usr/bin/env bash
# Print the NuGet version for HEAD: UTC committer time as year.month.day.(hour*100+minute).
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ts="$(git -C "$root" show -s --format=%ct HEAD)"
year="$(date -u -d "@$ts" +%Y)"
month="$(date -u -d "@$ts" +%-m)"
day="$(date -u -d "@$ts" +%-d)"
hour="$(date -u -d "@$ts" +%-H)"
minute="$(date -u -d "@$ts" +%-M)"
time_of_day="$((hour * 100 + minute))"
printf '%s.%s.%s.%s\n' "$year" "$month" "$day" "$time_of_day"
