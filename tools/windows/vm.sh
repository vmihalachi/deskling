#!/bin/sh
# Builds and checks the companion app's Windows side in a Parallels Windows VM on this Mac, from the working tree as
# it is: status, sync, build, test, screenshots, smoke, run (vm.py says how; --help).
exec python3 "$(dirname "$0")/vm.py" "$@"
