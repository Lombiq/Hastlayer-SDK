# Taken from: https://stackoverflow.com/a/246128/220230
CURRENT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" >/dev/null 2>&1 && pwd )"
cd ${CURRENT_DIR}/HardwareFramework/rtl
make all TARGET=hw DEVICE=xilinx_u280_xdma_201920_1
asd
cd $CURRENT_DIR
