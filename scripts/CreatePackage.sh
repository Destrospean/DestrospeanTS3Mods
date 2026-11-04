#!/bin/bash
cd "${0%/*}"

modName=$1
dllPath=$2
projectDir=$3
defaultPath=$4

mono ../tools/TS3BuildTool/ts3buildtool.exe -modName="$modName" -dllPath="$dllPath" -projectDir="$projectDir" -defaultPath="$defaultPath" > ts3buildtool-$modName.log
