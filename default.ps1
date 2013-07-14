properties { 
	$projectName = "Owin.HttpMessageHandler"
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
	New-Item "$buildOutputDir\bin\portable-net45+win8\" -Type Directory
	New-Item "$buildOutputDir\bin\portable-net40+sl4+win8+wp71\" -Type Directory
	New-Item "$buildOutputDir\Source" -Type Directory
	gci "$srcDir\Owin.HttpMessageHandler(portable-net45+win8)\bin\Release" |% { Copy-Item $_.FullName "$buildOutputDir\bin\portable-net45+win8\" }
	gci "$srcDir\Owin.HttpMessageHandler(portable-net40+sl4+win8+wp71)\bin\Release" |% { Copy-Item $_.FullName "$buildOutputDir\bin\portable-net40+sl4+win8+wp71\"}
	gci "$srcDir\Owin.HttpMessageHandler(portable-net45+win8)\*.cs" |% { Copy-Item $_ "$buildOutputDir\Source"  }
	gci $buildOutputDir\Source\*.cs |%  { (gc $_) -replace "public", "internal" | sc -path $_ }
}

task CreateNuGetPackages -depends CopyBuildOutput {
	$packageVersion = Get-Version $assemblyInfoFilePath
	copy-item $srcDir\*.nuspec $buildOutputDir
	exec { .$srcDir\.nuget\nuget.exe pack $buildOutputDir\Owin.HttpMessageHandler.nuspec -BasePath .\ -o $buildOutputDir -version $packageVersion }
	exec { .$srcDir\.nuget\nuget.exe pack $buildOutputDir\Owin.HttpMessageHandler.Sources.nuspec -BasePath .\ -o $buildOutputDir -version $packageVersion }
}