# Run this script in destination folder (..\Bluepath\Service References)

param (
	[string]$server = "127.0.0.1",
	[int]$port = $(throw "-port is required.")
)

$metadataEndpoint="http://" + $server + ":" + $port + "/BluepathExecutorService.svc"
$env:WCF="C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools"
$env:Path="$env:WCF"

svcutil.exe $metadataEndpoint /namespace:"*,Bluepath.ServiceReferences"
