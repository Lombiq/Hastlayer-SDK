#!/bin/bash

. zynq-benchmark.dot.sh

title "Welcome to the Hastlayer Demo for Zynq!"
run-benchmark demo/parallel_algorithm.xclbin Hast.Samples.Demo/Hast.Samples.Demo.dll
