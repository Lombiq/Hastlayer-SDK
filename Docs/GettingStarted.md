# Getting started



## Overview

To begin working with Hastlayer you'll need the following:

- The SDK and Hardware Framework repositories cloned.
- Access to Hastlayer Remote Services, which does the actual .NET to hardware transformation. Evaluation access is currently free and you can request it via [guys@hastlayer.com](mailto:guys@hastlayer.com).
- A compatible FPGA board. Currently only the [Nexys 4 DDR](http://store.digilentinc.com/nexys-4-ddr-artix-7-fpga-trainer-board-recommended-for-ece-curriculum/) board is supported, so you'll need to purchase one.

If you have a compatible FPGA board you can run the default sample even without having access to Hastlayer Remote Services.


## Flavors of Hastlayer

The Hastlayer components come in two "flavors" with corresponding branches in their repositories:

- Developer (*dev* branch): This is used by developers of Hastlayer itself. It includes the full source code.
- Client (*client* branch): Used by end-users of Hastlayer who run Hastlayer in a client mode, accessing *Hast.Core* as a remote service, i.e. Hastlayer Remote Services. *Hast.Core* encompasses those components of Hastlayer that do the heavy lifting of software to hardware transformation.

You'll see this terminology be used further on.


## First steps

These would be your first steps on starting to work with Hastlayer:

1. Clone the necessary repositories with either Mercurial or Git:
    - Using Mercurial: Clone the [Hastlayer SDK repo](https://bitbucket.org/Lombiq/hastlayer-sdk) and the [Hastlayer Hardware Framework - Xilinx repo](https://bitbucket.org/Lombiq/hastlayer-hardware-framework-xilinx) from Bitbucket (these are Mercurial repositories, similar to Git, that you can interact with via a GUI with e.g. [TortoiseHg](https://tortoisehg.bitbucket.io/); if you're unfamiliar with TortoiseHg check out [our video tutorial](https://www.youtube.com/watch?v=sbRxMXVEDc0)).

        Make sure to have the *eol* and *hggit* Mercurial extensions enabled!

        Since both repos contain multiple branches you need to make sure to clone them to the branch corresponding to the flavor you want to use, the `client` or `dev` branch. This is easiest done by using the following commands from a Windows command window (these will clone both repos on the `client` branch to the folder where the command line is opened):

        `hg clone --verbose https://bitbucket.org/Lombiq/hastlayer-sdk .\HastlayerSDK -r client`

        `hg clone --verbose https://bitbucket.org/Lombiq/hastlayer-hardware-framework-xilinx .\HastlayerHardwareFramework-Xilinx -r client`
    - Using Git: Clone the [Hastlayer SDK repo](https://github.com/Lombiq/Hastlayer-SDK) and the [Hastlayer Hardware Framework - Xilinx repo](https://github.com/Lombiq/Hastlayer-Hardware-Framework---Xilinx) from GitHub and checkout the `client` or `dev` branch corresponding to your flavor. Make sure to allow Git to initialize submodules!

2. Set up a Vivado and Xilinx SDK project in the Hardware Framework project as documented there, power up and program a compatible FPGA board.
3. Open the Visual Studio project corresponding to your flavor of Hastlayer.
4. Set the `Hast.Samples.Consumer` project (under the *Samples* folder) as the startup project here. If you're working in the *client* flavor then you'll need to configure your credentials, see the that project's documentation.
5. Start the sample project. That will by default run the sample that is also added by default to the Hardware project.
6. You should be able to see the results of the sample in its console window.

If everything is alright follow up with the rest of this documentation to write your first own Hastlayer-using algorithm.