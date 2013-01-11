param(
	[int]$buildNumber = 0
	)
Import-Module .\tools\psake.psm1
Import-Module .\tools\BuildFunctions.psm1
Invoke-Psake .\default.ps1 default -framework "4.0x64" -properties @{ buildNumber=$buildNumber }
Remove-Module BuildFunctions
Remove-Module psake