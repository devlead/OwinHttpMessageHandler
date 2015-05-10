properties {
	$projectName = "OwinHttpMessageHandler"
	$buildNumber = 0
	$rootDir  = Resolve-Path .\
	$buildOutputDir = "$rootDir\build"
	$reportsDir = "$buildOutputDir\reports"
	$srcDir = "$rootDir\src"
	$solutionFilePath = "$srcDir\$projectName.sln"
	$assemblyInfoFilePath = "$srcDir\SharedAssemblyInfo.cs"
	$nugetPath = "$srcDir\.nuget\nuget.exe"
}

task default -depends UpdateVersion, RunTests, CreateNuGetPackages

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

task Compile -depends Clean {
	exec { msbuild /nologo /verbosity:quiet $solutionFilePath /p:Configuration=Release }
}

task RunTests -depends Compile {
	$xunitRunner = "$srcDir\packages\xunit.runners.1.9.2\tools\xunit.console.clr4.exe"
	gci . -Recurse -Include *Tests.csproj, Tests.*.csproj | % {
		$project = $_.BaseName
		if(!(Test-Path $reportsDir\xUnit\$project)){
			New-Item $reportsDir\xUnit\$project -Type Directory
		}
        .$xunitRunner "$srcDir\$project\bin\Release\$project.dll" /html "$reportsDir\xUnit\$project\index.html"
    }
}

task BuildDnx {
	$env:Path+=";c:\temp"
	$env:Path
	Remove-Item "$srcDir\artifacts" -Force -Recurse -ErrorAction SilentlyContinue
	&{$Branch='dev';iex ((new-object net.webclient).DownloadString('https://raw.githubusercontent.com/aspnet/Home/dev/dnvminstall.ps1'))}
	$env:Path
	dnvm upgrade
	pushd
	cd .\src
	dnu restore
	dnu build .\OwinHttpMessageHandler
	dnu build .\OwinHttpMessageHandler.Tests
	dnx .\OwinHttpMessageHandler.Tests test
	popd
}

task CreateNuGetPackages -depends Compile {
	$versionString = Get-Version $assemblyInfoFilePath
	$version = New-Object Version $versionString
	$packageVersion = $version.Major.ToString() + "." + $version.Minor.ToString() + "." + $version.Build.ToString() + "-build" + $buildNumber.ToString().PadLeft(5,'0')
	gci $srcDir -Recurse -Include *.nuspec | % {
		exec { .$srcDir\.nuget\nuget.exe pack $_ -o $buildOutputDir -version $packageVersion }
	}
}
