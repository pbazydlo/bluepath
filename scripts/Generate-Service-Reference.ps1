# Run this script in destination folder (..\Bluepath\Service References)
# or directly from scripts folder

param (
	[string]$server = "127.0.0.1",
	[int]$port = $(throw "-port is required.")
)

$currentDirectory = $(get-location).Path;
$outputDirectory = $currentDirectory;
if(($currentDirectory | split-path -leaf) -eq "scripts") {
	$outputDirectory = ($currentDirectory + "\\..\\Bluepath\\Service References")
	$outputDirectory = split-path -resolve -path "$outputDirectory"
	$outputDirectory += "\\Service References"
} 

Write-Output $outputDirectory;

$metadataEndpoint="http://" + $server + ":" + $port + "/BluepathExecutorService.svc"
$env:WCF="C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools"
$env:Path="$env:WCF"

svcutil.exe $metadataEndpoint /namespace:"*,Bluepath.ServiceReferences" /out:"$outputDirectory\\RemoteExecutorService.cs" /config:"$outputDirectory\\output.config"
