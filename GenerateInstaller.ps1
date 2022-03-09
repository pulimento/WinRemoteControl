<#
.SYNOPSIS
	Generates an installer. (c) 2022 Javi Pulido
.DESCRIPTION
	This script generates a setup package. It uses Inno Setup. 
.EXAMPLE
	.\<script_name> -buildOutputPath WinRemoteControl\bin\Release\net5.0-windows\win-x86\publish setupExecutableNameIncludingEXEExtension WinRemoteControl_Setup_32 -CsprojPath .\WinRemoteControl\WinRemoteControl.csproj [-buildIdentifier 1.0.1] [-$baseDirectory $PSScriptRoot] [-installerOutputPath Installer\Output]
.LINK
	https://github.com/pulimento
#>

param
(
  [Parameter(HelpMessage="Overrides build identifier")][string]$buildIdentifier,
  [string]$baseDirectory = "$PSScriptRoot",
  [string]$installerOutputPath = "Installer\Output",
  [Parameter(mandatory=$true)][string]$buildOutputPath = "WinRemoteControl\bin\Release\net5.0-windows\win-x86\publish",
  [string]$setupExecutableNameIncludingEXEExtension = "WinRemoteControl_Setup_32.exe",
  [string]$CsprojPath = ".\WinRemoteControl\WinRemoteControl.csproj"
)

################################################################
##### FUNCTIONS
################################################################

function LogWarning($line)
{ Write-Host "##[warning] $line" -Foreground DarkYellow -Background Black
}
function LogDebug($line)
{ Write-Host "##[debug] $line" -Foreground Blue -Background Black
}
function LogSection($line)
{ Write-Host "##[section] $line" -Foreground Green -Background Black
}

################################################################
##### GLOBAL VARIABLES
################################################################

# Set working directory
Push-Location (Get-Location).Path
Set-Location $baseDirectory

# Constants
$installerFolderPath = Join-Path (Get-Location).Path "Installer"
$innoSetupScriptPath = Resolve-Path (Join-Path $installerFolderPath "InstallerScript.iss")
$innoSetupResourcesPath = Resolve-Path (Join-Path $installerFolderPath "InnoSetupResources")
$baseSetupNameIncludingEXEExtension = "WinRemoteControl_Setup.exe"

# Normalize installer output path
if(![System.IO.Path]::IsPathRooted($installerOutputPath))
{
  $installerOutputPath = Join-Path (Get-Location).Path $installerOutputPath
}

############################################################################################################################################
##### STAGES
############################################################################################################################################

LogDebug "################## VARIABLES ##################"
LogDebug "Build source path        : $buildOutputPath"
LogDebug "Inno Setup Script path   : $innoSetupScriptPath"
LogDebug "Inno Setup Resources path: $innoSetupResourcesPath"
LogDebug "Installer output         : $installerOutputPath"
LogDebug "Setup EXE name           : $setupExecutableNameIncludingEXEExtension"
LogDebug ".csproj path             : $CsprojPath"
LogDebug "###############################################"

################################################################
##### STAGE: PRE-COMPILATION
################################################################

LogSection "START Pre-compilation tasks"

# FIND INNO SETUP
$innoSetupCompilerPath = Resolve-Path (Join-Path $innoSetupResourcesPath "ISCC.exe")
if (-Not (Test-Path $innoSetupCompilerPath))
{ throw "InnoSetup compiler not found. Exiting..."
}

# Get version from csproj
$xml = [Xml] (Get-Content $CsprojPath)
$buildIdentifier = [Version] $xml.Project.PropertyGroup.Version

LogSection "END Pre-compilation tasks"

################################################################
##### STAGE: GENERATE INSTALLER
################################################################

LogSection "START generating installer"

LogDebug "innoSetupCompilerPath $innoSetupCompilerPath"
LogDebug "buildIdentifier $buildIdentifier"

& $innoSetupCompilerPath -DAppVersion="$buildIdentifier" -DOutputDir="$installerOutputPath" -DSourceDir="$buildOutputPath" $innoSetupScriptPath
if(-Not $?)
{
    LogWarning "lastexitcode $lastexitcode"
    exit $lastexitcode
}

LogSection "END generating installer"

################################################################
##### STAGE: Change installer name
################################################################

LogSection "Changing installer name"

if ($setupExecutableNameIncludingEXEExtension)
{
    $baseInstallerFullPath = Join-Path $installerOutputPath $baseSetupNameIncludingEXEExtension
    $destinationInstallerFullPath = Join-Path $installerOutputPath $setupExecutableNameIncludingEXEExtension
    LogDebug "Moving installer from: $baseInstallerFullPath to $destinationInstallerFullPath"
    Move-Item -Force $baseInstallerFullPath -Destination $destinationInstallerFullPath
}

LogSection "END Changing installer name"

Pop-Location