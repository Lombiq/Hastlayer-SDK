# Hastlayer Vitis Driver



## Overview

This project contains the communication service used to connect with [Vitis Unified Software Platform](https://www.xilinx.com/products/design-tools/vitis/vitis-platform.html) devices, such as the [Xilinx Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html) FPGA accelerator card via the [OpenCL](https://www.khronos.org/opencl/) library.

Note that the SH scripts in this project should use LF line endings!


## Requirements

* The system running the FPGA card must be 64-bit Linux (e.g. Ubuntu 18.04.2 LTS or CentOS 7.6) The installation instructions can be found here [in the platform documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/vhc1571429852245.html).
* The device specific software requirements can be found in the card's Getting Started page, e.g. [Alveo U250](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html#gettingStarted).
* Hastlayer has its own software requirements which can be found in the repository's *GettingStarted.md* file.

## Preparation

Even after everything is installed, you have to make sure that the executing user's environment variables are correctly set by sourcing the setup scripts [as described in the documentation](https://www.xilinx.com/html_docs/xilinx2019_2/vitis_doc/rbk1547656041291.html). You can add these commands into the `~/.bashrc` file to avoid having to type them every time. If running from the cloud, like Nimbix, this is probably handled automatically.

For setup instructions on the Nimbix cloud see the [Nimbix-specific instructions](Nimbix.md).

## Remarks

The kernel binary (XCLBIN) is presently not portable, running on a different machine than the one it was compiled on will cause various exceptions. Running on multiple instances of the same kind of compute node works however.
