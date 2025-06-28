param($installPath, $toolsPath, $package, $project)

$analyzersPaths = Join-Path (Join-Path (Split-Path -Path $toolsPath -Parent) "analyzers" ) *\* -Resolve

foreach ($analyzersPath in $analyzersPaths)
{
    # Uninstall the language agnostic analyzers.
    if (Test-Path $analyzersPath)
    {
        foreach ($analyzerFilePath in Get-ChildItem -Path "$analyzersPath\*.dll" -Exclude *.resources.dll)
        {
            if ($project.Object.AnalyzerReferences)
            {
                $project.Object.AnalyzerReferences.Remove($analyzerFilePath.FullName)
            }
        }
    }
}

$languageFolder = ""
if ($project.Type -eq "C#")
{
    $languageFolder = "cs"
}
if ($project.Type -eq "VB.NET")
{
    $languageFolder = "vb"
}
if ($languageFolder -eq "")
{
    return
}

foreach ($analyzersPath in $analyzersPaths)
{
    # Uninstall language specific analyzers.
    $languageAnalyzersPath = join-path $analyzersPath $languageFolder
    if (Test-Path $languageAnalyzersPath)
    {
        foreach ($analyzerFilePath in Get-ChildItem $languageAnalyzersPath -Filter *.dll)
        {
            if ($project.Object.AnalyzerReferences)
            {
                try
                {
                    Write-Host "Removing Analyzer from project: $($analyzerFilePath.FullName)"
                    $project.Object.AnalyzerReferences.Remove($analyzerFilePath.FullName)
                }
                catch
                {
                    Write-Host "Failed to remove Analyzer. Ignoring."
                }
            }
        }
    }
}