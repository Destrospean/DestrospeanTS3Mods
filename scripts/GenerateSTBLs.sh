#!/bin/bash
cd "${0%/*}"
projectDir=$1
rm "${projectDir}/resources"/*.stbl
for i in "${projectDir}/strings"/*.yaml; do
    mono ../tools/STBLize+/STBLize+.exe "$i" -d "${projectDir}/resources" -nu
done
