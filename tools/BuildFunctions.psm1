function Invoke-Tests
{
    param
    (
        [string]$reportsDir,
        [string]$xunitRunner,
        [string]$srcDir,
        [string]$buildConfiguration
    )    
	Write-Host "path to runner $xunitRunner"
    
	Get-ChildItem ..\ -Recurse -Include *Tests.csproj, Tests.*.csproj | % {
		$project = $_.BaseName
		if(!(Test-Path $reportsDir\xUnit\$project)){
			New-Item $reportsDir\xUnit\$project -Type Directory
		}
        .$xunitRunner "$srcDir\$project\bin\$buildConfiguration\$project.dll" /html "$reportsDir\xUnit\$project\index.html"
    }
}
function Invoke-NugetPackRecursive
{
    param
    (
        [string]$assemblyInfoFilePath,
        [string]$buildOutputDir,
        [string]$nugetPath,
        [string]$rootDir
    )
    
    $packageVersion = Get-Version $assemblyInfoFilePath
	Get-ChildItem $rootDir\nuget -Recurse -Include *.nuspec | % { 
		exec { .$nugetPath\nuget.exe pack $_ -BasePath .\ -o $buildOutputDir -version $packageVersion }
	}
}

function Get-Version
{
	param
	(
		[string]$assemblyInfoFilePath
	)
	Write-Host "path $assemblyInfoFilePath"
	$pattern = '(?<=^\[assembly\: AssemblyVersion\(\")(?<versionString>\d+\.\d+\.\d+\.\d+)(?=\"\))'
	$assmblyInfoContent = Get-Content $assemblyInfoFilePath
	return $assmblyInfoContent | Select-String -Pattern $pattern | Select -expand Matches |% {$_.Groups['versionString'].Value}
}

function Update-Version
{
	param 
    (
		[string]$version,
		[string]$assemblyInfoFilePath
	)
	
	$newVersion = 'AssemblyVersion("' + $version + '")';
	$newFileVersion = 'AssemblyFileVersion("' + $version + '")';
	$tmpFile = $assemblyInfoFilePath + ".tmp"

	Get-Content $assemblyInfoFilePath | 
		%{$_ -replace 'AssemblyVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $newVersion } |
		%{$_ -replace 'AssemblyFileVersion\("[0-9]+(\.([0-9]+|\*)){1,3}"\)', $newFileVersion }  | Out-File -Encoding UTF8 $tmpFile

	Move-Item $tmpFile $assemblyInfoFilePath -force
}