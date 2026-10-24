#!/bin/sh
# Compatibility entry: the existing root release command still builds Launcher.
set -eu
cd "$(dirname "$0")"
exec ./CPFileLauncher/build-release.sh "$@"
