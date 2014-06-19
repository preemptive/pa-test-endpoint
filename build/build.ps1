
function ZipFiles( $zipfilename, $sourcedir )
{
    [Reflection.Assembly]::LoadWithPartialName( "System.IO.Compression.FileSystem" )
    $compressionLevel = [System.IO.Compression.CompressionLevel]::Optimal
    [System.IO.Compression.ZipFile]::CreateFromDirectory( $sourcedir, $zipfilename, $compressionLevel, $false )
}

$startdir = (Get-Location).Path

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


ZipFiles "$startdir\pa-test-endpoint.zip" "$startdir\staging\" 
