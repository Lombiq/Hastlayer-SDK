FROM centos:7

# Download, extract and copy the directory of the "Xilinx Vitis 2020.2: All OS installer Single-
# File Download" from the Xilinx website. Copy the "centos7-install.sh" from the Hastlayer SDK to
# this directory. Download the platform files from the Xilinx Lounge, extract them until you
# have all RPMs and copy them to the platform directory.
COPY Xilinx_Unified_2020.2_* /root/Xilinx_Unified_2020.2/
COPY centos7-install.sh /root/
COPY platform /root/platform/

# Install dependencies
RUN yum install -y wget sudo unzip;
RUN ["/bin/sh", "-c", "cd /root/Xilinx_Unified_2020.2/; echo 1 | ./xsetup -b ConfigGen"]
RUN ["/bin/sh", "-c", "cd /root/Xilinx_Unified_2020.2/; ./xsetup --a XilinxEULA,3rdPartyEULA,WebTalkTerms -b Install -c /root/.Xilinx/install_config.txt"]
RUN yum install -y /root/platform/*.rpm
RUN cat /root/centos7-install.sh | sed '1d' | sed 's/\r$//' | sed 's/sudo //' | sh

# Setup for future sessions
RUN ["/bin/sh", "-c", "echo '[ -z $XILINX_XRT ] && [ -f /opt/xilinx/xrt/setup.sh ] && source /opt/xilinx/xrt/setup.sh' >> /root/.bashrc"]
RUN ["/bin/sh", "-c", "echo '[ -f /tools/Xilinx/Vitis/2020.2/settings64.sh ] && source /tools/Xilinx/Vitis/2020.2/settings64.sh' >> /root/.bashrc"]
RUN ["/bin/sh", "-c", "echo 'alias cls=clear' >> /root/.bashrc"]

# Cleanup
RUN rm -Rf /root/Xilinx_Unified_2020.2 /root/platform/

