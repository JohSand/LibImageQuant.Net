[CmdletBinding()]
Param(
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "PowerShell $($PSVersionTable.PSEdition) version $($PSVersionTable.PSVersion)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { Write-Error $_ -ErrorAction Continue; exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent


cd libimagequant

new-item -Force -Name build -ItemType directory

cd build

cmake -A x64 -DLIB_INSTALL_DIR=$pwd ..
cmake --build . --config Release
Copy-Item -Force -Path "Release" -Destination "..\.." -Recurse
cd ../..