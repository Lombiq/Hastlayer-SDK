#!/bin/bash

# This script replaces `xterm` and `xterm -e "some command"` with GNU screen calls for users 
# without graphical user interfaces.

command="bash"

while [[ "$1" != "-e" ]] && shift; do true ;done
[[ "$1" == "-e" ]] && [ ! -z "$2" ] && command="$2"

screen $command