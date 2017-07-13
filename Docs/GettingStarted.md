# Getting started



## Flavors of the Hastlayer solution

The Hastlayer components come in two "flavors" with corresponding branches in their repositories:

- Developer (*dev* branch): This is used by developers of Hastlayer itself. It includes the full source code.
- Client (*client* branch): Used by end-users of Hastlayer who run Hastlayer in a client mode, accessing *Hast.Core* as a remote service.

You'll see this terminology be used further on.


## First steps

These would be your first steps on starting to work with Hastlayer:

1. Clone the [Hastlayer repo](https://bitbucket.org/Lombiq/hastlayer) and the [Hastlayer Hardware](https://bitbucket.org/Lombiq/hastlayer-hardware) repo from Bitbucket (these are Mercurial repositories, similar to Git, that you can interact with via a GUI with e.g. [TortoiseHg](https://tortoisehg.bitbucket.io/); if you're unfamiliar with TortoiseHg check out [our video tutorial](https://www.youtube.com/watch?v=sbRxMXVEDc0)).

    Since both repos contain multiple branches you need to make sure to clone them to the branch corresponding to the flavor you want to use, the `client` or `dev` branch. This is easiest done by using the following commands from a Windows command window (these will clone both repos on the `client` branch to the folder where the command line is opened):

    `hg clone --verbose https://bitbucket.org/Lombiq/hastlayer .\Hastlayer -r client`

    `hg clone --verbose https://bitbucket.org/Lombiq/hastlayer-hardware .\HastlayerHardware -r client`

2. Set up a Vivado and Xilinx SDK project in the Hardware project as documented there, power up and program a compatible FPGA board.
3. Open the Visual Studio project corresponding to your flavor of Hastlayer.
4. Set the `Hast.Samples.Consumer` project (under the *Samples* folder) as the startup project here. If you're working in the *client* flavor then you'll need to configure your credentials, see the that project's documentation.
5. Start the sample project. That will by default run the sample that is also added by default to the Hardware project.
6. You should be able to see the results of the sample in its console window.

If everything is alright follow up with the rest of this documentation to write your first own Hastlayer-using algorithm.