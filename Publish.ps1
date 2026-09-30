# Path to project metadata files
$csprojPath = "Marketeer\Marketeer.csproj"
$jsonPath = "Marketeer\Marketeer.json"

if (-Not (Test-Path $csprojPath) -or -Not (Test-Path $jsonPath)) {
    Write-Host "Error: Metadata files not found. Are you at the repository root?" -ForegroundColor Red
    exit
}

# Read current version from csproj
$csprojContent = Get-Content -Path $csprojPath -Raw
$versionMatch = [regex]::Match($csprojContent, "<Version>(.*?)</Version>")

if (-Not $versionMatch.Success) {
    Write-Host "Error: Could not find <Version> tag in $csprojPath." -ForegroundColor Red
    exit
}

$currentVersion = $versionMatch.Groups[1].Value
Write-Host "Current version detected: " -NoNewline
Write-Host "$currentVersion" -ForegroundColor Cyan

# Parse version parts
$versionParts = $currentVersion.Split('.')
if ($versionParts.Length -ne 4) {
    Write-Host "Warning: Version format is not X.X.X.X. Custom input recommended." -ForegroundColor Yellow
    $major = 0; $minor = 0; $patch = 0; $build = 0
} else {
    $major = [int]$versionParts[0]
    $minor = [int]$versionParts[1]
    $patch = [int]$versionParts[2]
    $build = [int]$versionParts[3]
}

# Generate automatic bump proposals
$bumpBuild = "$major.$minor.$patch.$($build + 1)"
$bumpPatch = "$major.$minor.$($patch + 1).0"
$bumpMinor = "$major.$($minor + 1).0.0"
$bumpMajor = "$($major + 1).0.0.0"

Write-Host "`nSelect the next version number:"
Write-Host "1) Build bump ($bumpBuild)"
Write-Host "2) Patch bump ($bumpPatch)"
Write-Host "3) Minor bump ($bumpMinor)"
Write-Host "4) Major bump ($bumpMajor)"
Write-Host "5) Custom version"
Write-Host "Q) Quit"

$choice = Read-Host "Choice"
$newVersion = ""

switch ($choice) {
    "1" { $newVersion =$bumpBuild }
    "2" { $newVersion =$bumpPatch }
    "3" { $newVersion =$bumpMinor }
    "4" { $newVersion =$bumpMajor }
    "5" { 
        $newVersion = Read-Host "Enter custom version (e.g., 1.2.3.4)" 
        if ([string]::IsNullOrWhiteSpace($newVersion)) { exit }
    }
    "Q" { exit }
    "q" { exit }
    default { Write-Host "Invalid choice."; exit }
}

$isTestPrompt = Read-Host "`nIs this a test prerelease? (y/N)"
$isTest = $isTestPrompt -eq "y" -or $isTestPrompt -eq "Y"

$tagName = "v$newVersion"
if ($isTest) { $tagName += "-test" }

Write-Host "`nRunning unit tests before publishing..." -ForegroundColor Cyan
dotnet test --nologo

if ($LASTEXITCODE -ne 0) {
    Write-Host "`nError: One or more unit tests failed. Publishing aborted to prevent shipping broken code." -ForegroundColor Red
    exit
}

Write-Host "`nAll unit tests passed successfully!" -ForegroundColor Green
Write-Host "Preparing release for " -NoNewline
Write-Host "$tagName" -ForegroundColor Yellow

# Update Marketeer.csproj
$csprojContent = $csprojContent -replace "<Version>.*?</Version>", "<Version>$newVersion</Version>"
Set-Content -Path $csprojPath -Value $csprojContent

# Update Marketeer.json
$jsonContent = Get-Content -Path $jsonPath -Raw
$jsonContent = $jsonContent -replace '"AssemblyVersion":\s*".*?"', "`"AssemblyVersion`": `"$newVersion`""
Set-Content -Path $jsonPath -Value $jsonContent

Write-Host "Files updated successfully. Performing Git operations..." -ForegroundColor DarkGray

# Git automation
git add $csprojPath $jsonPath
git commit -m "chore: bump version to $tagName"
git tag $tagName

Write-Host ""
$pushConfirm = Read-Host "Push commit and tag to origin now? (Y/n)"
if ($pushConfirm -ne "n" -and $pushConfirm -ne "N") {
    git push origin HEAD
    git push origin $tagName
    Write-Host "`nRelease pushed successfully! GitHub Actions will now build $tagName." -ForegroundColor Green
} else {
    Write-Host "`nPush cancelled. The commit and tag remain locally on your machine." -ForegroundColor Yellow
}