#!/bin/bash

# Auxiliary functions.

function repeat() { for i in $(seq $1); do echo -n "$2"; done; echo; }
function title()
{
  repeat $((${#1} + 4)) '*'
  echo "* $1 *"
  repeat $((${#1} + 4)) '*'
}

function cls() { clear; echo -e '\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n\n'; clear; }

# Initialize .NET and other environment settings.

export DOTNET_ROOT=/media/sd-mmcblk0p1/dotnet-sdk-5*-linux-arm/
export PATH=$PATH:$DOTNET_ROOT
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export XILINX_VITIS=/usr
export XILINX_XRT=/usr
export BENCHMARK_RUN_DOT_READY=1
[ -f /usr/lib/libOpenCL.so ] || ln -s /usr/lib/libOpenCL.so.1 /usr/lib/libOpenCL.so
[ -f /lib/libdl.so ] ||ln -s /lib/libdl.so.2 /lib/libdl.so
[ -d /media/sd-mmcblk0p1/Hast.Samples.Consumer/HardwareFramework/bin ] || mkdir /media/sd-mmcblk0p1/Hast.Samples.Consumer/HardwareFramework/bin

# Define program

ConsumerPath="/media/sd-mmcblk0p1/Hast.Samples.Consumer"

function run-benchmark-inner()
{
  XclbinFileName="$1"
  shift

  Label="$(cat "$XclbinFileName.name")"

  title "$XclbinFileName - $Label - $(date)"

  echo fpgautil -b "$(echo $XclbinFileName | sed 's/\.xclbin$/.bit.bin/')"
  time fpgautil -b "$(echo $XclbinFileName | sed 's/\.xclbin$/.bit.bin/')"

  ProjectPath="$PWD"
  pushd "$ConsumerPath"
    echo dotnet Hast.Samples.Consumer.dll -device "TE0715-04-30-1C" -sample $(echo "$Label" | sed 's/\s.*//') "$@" -bin "$ProjectPath/$XclbinFileName"
    time dotnet Hast.Samples.Consumer.dll -device "TE0715-04-30-1C" -sample $(echo "$Label" | sed 's/\s.*//') "$@" -bin "$ProjectPath/$XclbinFileName"
  popd

  echo "FCLK0 = $(cat /sys/devices/soc0/fclk0/set_rate)Hz"
}

function run-benchmark()
{
  if [ $# -eq 0 ]; then
    echo "USAGE: $0 'filename.xclbin'"
    echo "    The filename must be a relative path to the current directory."
    return
  fi

  XclbinFileName="$1"
  shift

  [ -f "$XclbinFileName.log" ] && rm "$XclbinFileName.log"
  run-benchmark-inner "$XclbinFileName" "$@" | tee "$XclbinFileName.log"
}

function run-and-verify()
{
    run-benchmark "$1" -verify true
}
