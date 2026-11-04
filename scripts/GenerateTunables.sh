#!/bin/bash
cd "${0%/*}"
modName=$1
filename=ts3buildtool-$modName.log
last_line=$(tail -n 1 $filename)
path="${last_line:52}"
mono ../tools/TuningResourceGenerator/TuningResourceGenerator.exe "$path"
rm $filename
