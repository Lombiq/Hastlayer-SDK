#!/bin/bash

# Ensures that the XRT environment is set up.
[ -z $XILINX_XRT ] && [ -f /opt/xilinx/xrt/setup.sh ] && source /opt/xilinx/xrt/setup.sh

# Includes the EPEL (needed for GDI+) and Microsoft package repositories.
sudo yum install -y epel-release
sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm

# Installs the latest Python 3.x, needed for the bit-to-bin conversion. See:
# https://github.com/topic-embedded-products/meta-topic/blob/master/recipes-bsp/fpga/fpga-bit-to-bin/fpga-bit-to-bin.py
yum install -y python3

# This installs the full SDK allowing you to build from source. If you only want to run the compiled application, you
# can install just the runtime by swapping the comment on the two lines below.
sudo yum install -y dotnet-sdk-6.0
#sudo yum install -y dotnet-runtime-6.0

# The current Linux-compatible samples don't require GDI+ or System.Drawing. If you need an image processor  library in
# your code, we suggest using ImageSharp instead. It provides competitive performance without native dependencies.
# If the version of libgdiplus changed, type 'yum whatprovides libgdiplus' to get the package name.
#sudo yum install -y libgdiplus-2.10-10.el7.x86_64
