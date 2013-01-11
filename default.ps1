properties { 
	$projectName = "OwinHttpMessageHandler"
	$buildNumber = 0
	$rootDir  = Resolve-Path ..\
	$buildOutputDir = "$rootDir\build"
	$reportsDir = "$buildOutputDir\reports"
	$srcDir = "$rootDir\src"
	$solutionFilePath = "$srcDir\$projectName.sln"
	$assemblyInfoFilePath = "$srcDir\SharedAssemblyInfo.cs"
}

task default -depends Clean, UpdateVersion, RunTests, CopyBuildOutput

task Clean {
	Remove-Item $buildOutputDir -Force -Recurse -ErrorAction SilentlyContinue
	exec { msbuild /nologo /verbosity:quiet $solutionFilePath /t:Clean }
}

task UpdateVersion {
	$version = Get-Version $assemblyInfoFilePath
	$oldVersion = New-Object Version $version
	$newVersion = New-Object Version ($oldVersion.Major, $oldVersion.Minor, $oldVersion.Build, $buildNumber)
	Update-Version $newVersion $assemblyInfoFilePath
}

task Compile { 
	exec { msbuild /nologo /verbosity:quiet $solutionFilePath /p:Configuration=Release }
}

task RunTests -depends Compile {
	$xunitRunner = "$toolsPath\XUnitRunner\xunit.console.clr4.exe"
	Invoke-Tests $reportsDir $xunitRunner $srcDir Release
	
	$mspecRunner = @(gci -r -i mspec-clr4.exe $srcDir)[0].FullName
	exec { &$mspecRunner "$srcDir\Beam.Domain.Tests\bin\Release\Beam.Domain.Tests.dll" }
}

task CopyBuildOutput -depends Compile {
	$binOutputDir = "$buildOutputDir\OwinHttpMessageHandler\bin\net45\"
	New-Item $binOutputDir -Type Directory
	@("OwinHttpMessageHandler.???") |% { Copy-Item "$srcDir\OwinHttpMessageHandler\bin\Release\$_" $binOutputDir}
}

task CreateNuGetPackages -depends CopyBuildOutput {
	$nugetPath = Get-Item .
    Invoke-NugetPackRecursive $assemblyInfoFilePath $buildOutputDir $nugetPath $rootDir
	
	$packageVersion = Get-Version $assemblyInfoFilePath
	Get-ChildItem $rootDir\nuget -Recurse -Include *.nuspec | % { 
		exec { .$nugetPath\nuget.exe pack $_ -BasePath .\ -o $buildOutputDir -version $packageVersion }
	}
}

task PublishPackages {
	$packages = Get-ChildItem $buildOutputDir\*.nupkg
    Publish-Packages $packages
}