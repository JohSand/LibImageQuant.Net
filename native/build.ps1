[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent
Set-Location $PSScriptRoot

# Builds the native libraries for win-x64 and lays them out under
# runtimes/win-x64/native/, matching the NuGet native-asset convention
# consumed by src/LibImageQuant.Net.Core and src/Zopfli.Net.

$Rid = "win-x64"
$OutDir = Join-Path $PSScriptRoot "runtimes\$Rid\native"
New-Item -Force -Path $OutDir -ItemType Directory | Out-Null

#imagequant

cd libimagequant

new-item -Force -Name build -ItemType directory

cd build

cmake -A x64 -DLIB_INSTALL_DIR=$pwd -DCMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=TRUE -DBUILD_SHARED_LIBS=TRUE ..
cmake --build . --config Release
Copy-Item -Force -Path "Release\imagequant.dll" -Destination $OutDir
cd ../..


# zopfli

cd zopfli

new-item -Force -Name build -ItemType directory

cd build

cmake -A x64 -DCMAKE_WINDOWS_EXPORT_ALL_SYMBOLS=TRUE -DBUILD_SHARED_LIBS=TRUE ..
cmake --build . --config Release
Copy-Item -Force -Path "Release\zopfli.dll" -Destination $OutDir
cd ../..