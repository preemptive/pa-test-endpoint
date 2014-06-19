function ZipFiles( $zipfilename, $sourcedir )
{
    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" )
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory( $sourcedir, $zipfilename, $compressionLevel, $false )
}

$startdir = $PSScriptRoot
$deploymentDir = "\\gkar\data\RI APIs\PA Endpoint"
$exeName = "endpoint.exe"
$zipFileName = "pa-test-endpoint.zip"
$timestamp = Get-Date -Format yyyy-MM-dd-HH-mm

Set-Location $startdir

# package into a zipfile

if((test-path "staging" -pathtype container)) 
{
	Remove-Item -Recurse -Force "staging" -ErrorAction Stop
}
if(test-path "$startdir\$zipFileName")
{
    Remove-Item "$startdir\$zipFileName"
}

mkdir "staging"
copy-item "..\Test Endpoint\bin\Release\$exeName" "staging\" -ErrorAction Stop
copy-item "..\README.md" "staging\" -ErrorAction Stop
copy-item "..\License.txt" "staging\" -ErrorAction Stop

$version = (Get-Command .\staging\$exeName).FileVersionInfo.FileVersion

ZipFiles "$startdir\$zipFileName" "$startdir\staging\" 

$thisdeployment = "$deploymentdir\$version\$timestamp"
mkdir $thisdeployment
Copy-Item "$zipFileName" $thisdeployment -ErrorAction Stop
