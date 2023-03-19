#!/bin/bash

# Ensures that the XRT environment is set up.
[ -z $XILINX_XRT ] && source /opt/xilinx/xrt/setup.sh

# DotNet Core
# see: https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-ubuntu-1604
wget https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update
yes | sudo apt-get --yes --force-yes install apt-transport-https
sudo apt-get update
# This installs the full SDK allowing you to build from source. If you only want to run the compiled application, you
# can install just the runtime by swapping the comment on the two lines below.
yes | sudo apt-get --yes --force-yes install dotnet-sdk-3.1
# yes | sudo apt-get --yes --force-yes install dotnet-runtime-3.1



# GDI plus (for image processing)
yes | sudo apt --yes --force-yes install libgdiplus



# Environment
[ -d /data/bin ] || mkdir /data/bin
echo -e "#!/bin/bash\ntput reset" > /data/bin/cls
chmod +x /data/bin/cls
export PATH="/data/bin:$PATH"
