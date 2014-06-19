
function ZipFiles( $zipfilename, $sourcedir )
{
    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" )
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory( $sourcedir, $zipfilename, $compressionLevel, $false )
}

$startdir = (Get-Location).Path
$deploymentDir = "\\gkar\data\RI APIs\PA Endpoint"
$zipFileName = "pa-test-endpoint.zip"
$timestamp = Get-Date -Format yyyy-MM-dd-HH-mm

Set-Location $startdir

# package into a zipfile

if((test-path "staging" -pathtype container)) {
	Remove-Item -Recurse -Force "staging" -ErrorAction Stop
}
if(test-path "$startdir\pa-test-endpoint.zip")
{
    Remove-Item "$startdir\pa-test-endpoint.zip"
}

mkdir "staging"
copy-item "..\Test Endpoint\bin\Release\endpoint.exe" "staging\" -ErrorAction Stop
copy-item "..\README.md" "staging\" -ErrorAction Stop
copy-item "..\License.txt" "staging\" -ErrorAction Stop

$version = [System.Diagnostics.FileVersionInfo]::GetVersionInfo("..\Test Endpoint\bin\Release\endpoint.exe").FileVersion

ZipFiles "$startdir\pa-test-endpoint.zip" "$startdir\staging\" 

$thisdeployment = "$deploymentdir\$version\$timestamp"
mkdir $thisdeployment
Copy-Item "$zipFileName" $thisdeployment -ErrorAction Stop