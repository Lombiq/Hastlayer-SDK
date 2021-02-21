#!/bin/bash

# Ensures that the XRT environment is set up.
[ -z $XILINX_XRT ] && source /opt/xilinx/xrt/setup.sh

# Includes the EPEL (needed for GDI+) and Microsoft package repositories.
sudo yum install -y epel-release
sudo rpm -Uvh https://packages.microsoft.com/config/centos/7/packages-microsoft-prod.rpm

# This installs the full SDK allowing you to build from source. If you only want to run the compiled application, you
# can install just the runtime by swapping the comment on the two lines below.
sudo yum install -y dotnet-sdk-5.0
#sudo yum install dotnet-runtime-5.0

# If the version of libgdiplus changed, type 'yum whatprovides libgdiplus' to get the package name.
sudo yum install -y libgdiplus-2.10-10.el7.x86_64
