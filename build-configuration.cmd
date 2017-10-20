@echo off

%FAKE% %NYX% "target=clean" -st
%FAKE% %NYX% "target=RestoreNugetPackages" -st

IF NOT [%1]==[] (set RELEASE_NUGETKEY="%1")

SET SUMMARY="First OSS project in the open space"
SET DESCRIPTION="First OSS project in the open space"

%FAKE% %NYX% appName=Elders.Cronus.Transport.AzureServiceBus appSummary=%SUMMARY% appDescription=%DESCRIPTION% nugetPackageName=Cronus.Transport.AzureServiceBus nugetkey=%RELEASE_NUGETKEY%
