properties { 
	$projectName = "OwinHttpMessageHandler"
	$buildNumber = 0
	$rootDir  = Resolve-Path .\
	$buildOutputDir = "$rootDir\build"
	$reportsDir = "$buildOutputDir\reports"
	$srcDir = "$rootDir\src"
	$solutionFilePath = "$srcDir\$projectName.sln"
	$assemblyInfoFilePath = "$srcDir\SharedAssemblyInfo.cs"
}

task default -depends Clean, UpdateVersion, RunTests, CopyBuildOutput, CreateNuGetPackages

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
	$xunitRunner = "$rootDir\tools\xunit.runners.1.9.1\xunit.console.clr4.exe"
	Get-ChildItem . -Recurse -Include *Tests.csproj, Tests.*.csproj | % {
		$project = $_.BaseName
		if(!(Test-Path $reportsDir\xUnit\$project)){
			New-Item $reportsDir\xUnit\$project -Type Directory
		}
        .$xunitRunner "$srcDir\$project\bin\Release\$project.dll" /html "$reportsDir\xUnit\$project\index.html"
    }
}
task CopyBuildOutput -depends Compile {
	$binOutputDir = "$buildOutputDir\Assembly\OwinHttpMessageHandler\bin\net45\"
	New-Item $binOutputDir -Type Directory
	New-Item "$buildOutputDir\Source" -Type Directory
	gci $srcDir\OwinHttpMessageHandler\bin\Release |% { Copy-Item "$srcDir\OwinHttpMessageHandler\bin\Release\$_" $binOutputDir}
	gci $srcDir\OwinHttpMessageHandler\*.cs |% { Copy-Item $_ "$buildOutputDir\Source"  }
	gci $buildOutputDir\Source\*.cs |%  { (gc $_) -replace "public", "internal" | sc -path $_ }
}

task CreateNuGetPackages -depends CopyBuildOutput {
	$packageVersion = Get-Version $assemblyInfoFilePath
	copy-item $srcDir\*.nuspec $buildOutputDir
	exec { .$rootDir\Tools\nuget.exe pack $buildOutputDir\OwinHttpMessageHandler.nuspec -BasePath .\ -o $buildOutputDir -version $packageVersion }
	exec { .$rootDir\Tools\nuget.exe pack $buildOutputDir\OwinHttpMessageHandler.Sources.nuspec -BasePath .\ -o $buildOutputDir -version $packageVersion }
}