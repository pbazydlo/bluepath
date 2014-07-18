param (
	[string]$server = $(throw "-server is required."),
	[int]$port = 5986,
	[string]$user = $(throw "-user is required."),
	[string]$password = $(throw "-password is required."),
	[string]$source = $(throw "-source is required."),
	[string]$destination = $(throw "-destination is required.")
)

$secPassword = ConvertTo-SecureString $password -AsPlainText -Force
$credential = New-Object System.Management.Automation.PSCredential($user, $secPassword)
$uri = New-Object System.Uri("https://" + $server + ":" + $port)

Write-Output ("Connecting to " + $uri.ToString() + "...")
$session = New-PSSession -ConnectionUri $uri -Credential $credential -SessionOption (New-PSSessionOption -SkipCACheck)

Write-Output "Sending files..."
Get-ChildItem -Path $source -File | Foreach-Object {
	if ($_.Extension -ne ".pdb") {
		$target = $destination + $_.Name
		.\Send-File.ps1 $_.FullName $target $session
	}
}

Disconnect-PSSession $session
Write-Output "Disconnected."