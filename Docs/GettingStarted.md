# Getting started



## Flavors of the Hastlayer solution

The Hastlayer components come in two "flavors" with corresponding branches in their repositories:

- Developer (*dev* branch): This is used by developers of Hastlayer itself. It includes the full source code.
- Client (*client* branch): Used by end-users of Hastlayer who run Hastlayer in a client mode, accessing *Hast.Core* as a remote service.

You'll see this terminology be used further on.


## First steps

These would be your first steps on starting to work with Hastlayer:

1. Clone the [Hastlayer repo](https://bitbucket.org/Lombiq/hastlayer) and the [Hastlayer Hardware](https://bitbucket.org/Lombiq/hastlayer-hardware) repo from Bitbucket (these are Mercurial repositories, similar to Git, that you can interact with via a GUI with e.g. [TortoiseHg](https://tortoisehg.bitbucket.io/); if you're unfamiliar with TortoiseHg check out [our video tutorial](https://www.youtube.com/watch?v=sbRxMXVEDc0)).
2. Update both repos to the `client` or `dev` branch, corresponding to the flavor you want to use.
3. Set up a Vivado and Xilinx SDK project in the Hardware project as documented there, power up and program a compatible FPGA board.
4. Open the Visual Studio project corresponding to your flavor of Hastlayer.
5. Set the `Hast.Samples.Consumer` project (under the *Samples* folder) as the startup project here. If you're working in the *client* flavor then you'll need to configure your credentials, see the that project's documentation.
6. Start the sample project. That will by default run the sample that is also added by default to the Hardware project.
7. You should be able to see the results of the sample in its console window.

If everything is alright follow up with the rest of this documentation to write your first own Hastlayer-using algorithm.