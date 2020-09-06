#!/bin/bash



# DotNet Core
# see: https://docs.microsoft.com/en-us/dotnet/core/install/linux-package-manager-ubuntu-1604
wget https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
# This installs the full SDK allowing you to build from source. If you only want to run the compiled application, you
# can install just the runtime by swapping the comment on the two lines below.
sudo apt-get install dotnet-sdk-3.1
# sudo apt-get install dotnet-runtime-3.1



# GDI plus (for image processing)
sudo apt install libgdiplus



# Environment
[ -d /data/bin ] || mkdir /data/bin
echo -e "#!/bin/bash\ntput reset" > /data/bin/cls
chmod +x /data/bin/cls
export PATH="/data/bin:$PATH"
