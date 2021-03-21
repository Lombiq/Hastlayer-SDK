FROM centos:7

# Download, extract and copy the directory of the "Xilinx Vitis 2020.2: All OS installer Single-
# File Download" from the Xilinx website. Copy the "centos7-install.sh" from the Hastlayer SDK to
# this directory. Download the platform files from the Xilinx Lounge, extract them until you
# have all RPMs and copy them to the platform directory.
COPY Xilinx_Unified_2020.2_* /root/
COPY centos7-install.sh /root/
COPY platform /root/

# Install dependencies
RUN yum install -y wget sudo;
RUN ["/bin/sh", "-c", "cd /root; cd Xilinx_Unified_2020.2_*/; ./xsetup -b ConfigGen; ./xsetup --a XilinxEULA,3rdPartyEULA,WebTalkTerms -b Install -c /root/.Xilinx/install_config.txt"]
RUN yum install /root/platform/*.rpm
RUN cat /root/centos7-install.sh | sed '1d' | sed 's/\r$//' | sed 's/sudo //' | sh
