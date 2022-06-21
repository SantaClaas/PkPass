#!/usr/bin/env bash

set -eu
set -o pipefail

dotnet tool restore
#dotnet fake "$@"
dotnet fake build