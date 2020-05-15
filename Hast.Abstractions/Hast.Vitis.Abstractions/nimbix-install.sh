#!/bin/bash



# DotNet Core
wget https://packages.microsoft.com/config/ubuntu/16.04/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb

sudo apt-get update
sudo apt-get install apt-transport-https
sudo apt-get update
sudo apt-get install dotnet-sdk-3.1



# GDI plus
sudo apt install libgdiplus



# Environment
[ -d /data/bin ] || mkdir /data/bin
echo -e "#!/bin/bash\ntput reset" > /data/bin/cls
chmod +x /data/bin/cls
export PATH="/data/bin:$PATH"