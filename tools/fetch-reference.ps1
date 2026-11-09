# Fetches the pinned Descent reference source for behavioural study only.
# It is cloned into reference/descent/ which is gitignored, so nothing enters VCS.
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$target = Join-Path $root 'reference/descent'
$repo = 'https://github.com/videogamepreservation/descent'
$pin = 'f6fb1c30a06725b403dfa7db4b251ecea1c3efc1'

if (Test-Path $target) {
    Write-Host "[reference] already present: $target"
    exit 0
}

New-Item -ItemType Directory -Force -Path (Split-Path -Parent $target) | Out-Null

# Blobless clone keeps history small; the pinned checkout pulls only needed blobs.
git clone --filter=blob:none --no-checkout $repo $target
if (-not $?) { throw "clone failed" }

Push-Location $target
try {
    git checkout $pin
    if (-not $?) { throw "checkout of $pin failed" }
    Write-Host "[reference] checked out $pin into $target"
}
finally {
    Pop-Location
}
