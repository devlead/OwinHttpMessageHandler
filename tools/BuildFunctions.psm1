function New-DependencyReport {
    param
    (
        [string]$rootDir,
        [string]$toolsPath,
        [string]$reportsDir
    )
    
    $reportName = "DependencyReport"

    $zipArchiver = "$toolsPath\7Zip\7za.exe"
    $dependencyReportPath = "$reportsDir\Dependency"
    if (-not (Test-Path $dependencyReportPath))
    {
        Write-Host "Creating folder $dependencyReportPath"
        New-Item -type directory -path $dependencyReportPath
    }
    
    [xml]$xmlDoc = "<?xml version=`"1.0`" encoding=`"utf-8`"?><?xml-stylesheet type=`"text/xsl`" href=`"$reportName.xsl`"?><DependencyReport/>"
    
    $xmlRoot = $xmlDoc.DocumentElement
    
    $packages = Get-ChildItem $rootDir -include *.nupkg -recurse | Sort-Object FullName
	foreach($package in $packages) {
        Write-Host "Analyzing package $package"
        
        [xml]$nuspecXml = .$zipArchiver e $package -so *.nuspec
        $packageFolder = Get-FirstSubDirectory $rootDir $package.FullName
        $nupkgName = $nuspecXml.Package.Metadata.Id
        $nupkgVersion = $nuspecXml.Package.Metadata.Version
        
        $packageElement = $xmlDoc.CreateElement("Package")
        $nameElement = New-XmlNode $xmlDoc "Name" "$nupkgName"
        [void]$packageElement.AppendChild($nameElement)
        $versionElement = New-XmlNode $xmlDoc "Version" "$nupkgVersion"
        [void]$packageElement.AppendChild($versionElement)
        
        $packagesElement = $xmlRoot.SelectSingleNode("Packages[@Folder=`"$packageFolder`"]")
        if ($packagesElement -eq $null)
        {
            $packagesElement = $xmlDoc.CreateElement("Packages")
            $folderAttribute = $xmlDoc.CreateAttribute("Folder")
            $folderAttribute.Value = $packageFolder
            $packagesElement.SetAttributeNode($folderAttribute)
            [void]$xmlRoot.AppendChild($packagesElement)
        }
        
        [void]$packagesElement.AppendChild($packageElement)
	}
    
    $xmlDoc.Save("$dependencyReportPath\$reportName.xml")
    
    Copy-Item "$toolsPath\Beam.Standards\$reportName.xsl" "$dependencyReportPath"
}

function Get-FirstSubDirectory
{
    param
    (
        [string]$basePath,
        [string]$fullPath
    )
    return $fullPath.Replace($basePath, "").Split("\\")[1]
}

function New-XmlNode
{
	param
	(
        [xml]$xmlDoc,
		[string]$name,
        [string]$innerText
	)
	
    $xmlElement = $xmlDoc.CreateElement("$name")
    [void]$xmlElement.AppendChild($xmlDoc.CreateTextNode("$innerText"))
	return $xmlElement
}

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

function Invoke-Coverage
{
	param
	(
		[string]$dotCoverRunner,
		[string]$srcDir,
		[string]$reportsDir
	)
    
    if (!(Test-Path $reportsDir\xUnit))
    {
        New-Item -type directory -path "$reportsDir\xUnit"
    }
    
	gci $srcDir -Recurse -Include "DotCoverConfig.xml" | Select -expandproperty FullName |% { 
		.$dotCoverRunner "cover" "$_" 
	}
	$coverageReports = @( gci $srcDir -Recurse -Include "coverage.xml" | Select -expandproperty FullName)
	$coverageReports = [string]::Join(";",$coverageReports)
	.$dotCoverRunner "merge" "/source=$coverageReports" "/output=$reportsDir\DotCover\merged.xml"
	.$dotCoverRunner "report" "/source=$reportsDir\DotCover\merged.xml" "/output=$reportsDir\DotCover\index.html" "/ReportType=HTML"
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

function Publish-Packages
{
	param
	(
		[array]$packages,
        [string]$destinationPath = "\\storage.int.beamfs.com\Dev\Packages\NuGet"
	)	
	
	foreach ($package in $packages) {
		Copy-Item $package $destinationPath
	}
}

function Invoke-SolutionCop
{
    param 
    (
        [string]$rootDir,
		[string]$solutionCopRunner,
        [string]$reportsDir,
		[string]$solutionCopReportsDirName = "SolutionCop"
	)
        
    if (-not $solutionCopRunner.EndsWith("\SolutionCop.exe"))  
    {
        $solutionCopRunner = "$solutionCopRunner\SolutionCop\net40\SolutionCop.exe"
    }  
        
    $solutionCopReportsDir = "$reportsDir\$solutionCopReportsDirName"
    
    if (-not (Test-Path $solutionCopReportsDir))
    {
        Write-Host "Creating folder $solutionCopReportsDir"
        New-Item -type directory -path $solutionCopReportsDir
    }
    
    .$solutionCopRunner $rootDir $solutionCopReportsDir     
}

function Invoke-StyleCop
{
    param 
    (
        [string]$solutionFilePath,
		[string]$styleCopConsoleRunner,
        [string]$reportsDir,
		[string]$styleCopReportsDirName = "StyleCop",
        [string]$styleCopReportFileName = "StyleCopReport.xml",
        [string]$styleCopConsoleAddinsPath = "$reportsDir\..\..\tools\Beam.StyleCop",
        [string]$styleCopReportXslPath = "$styleCopConsoleRunner\StyleCopConsole\net40\StyleCopReport.xsl"
	)
    
    if (-not $styleCopConsoleRunner.EndsWith("\StyleCopConsole.exe"))  
    {
        $styleCopConsoleRunner = "$styleCopConsoleRunner\StyleCopConsole\net40\StyleCopConsole.exe"
    }
    
    $styleCopReportsDir = "$reportsDir\$styleCopReportsDirName"
        
    if (-not (Test-Path $styleCopReportsDir))
    {
        Write-Host "Creating folder $styleCopReportsDir"
        New-Item -type directory -path $styleCopReportsDir
    }

    .$styleCopConsoleRunner -sf $solutionFilePath -of "$styleCopReportsDir\$styleCopReportFileName" -tf "$styleCopReportXslPath" -ad $styleCopConsoleAddinsPath
}