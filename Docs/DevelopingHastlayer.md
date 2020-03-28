# Developing Hastlayer



How to work on Hastlayer itself?


## Design principles

- From a user's (i.e. using developer's) perspective Hastlayer should be as simple as possible. To achieve this e.g. use generally good default configurations so in the majority of cases there is no configuration needed.
- Software that was written previously, without knowing about Hastlayer should be usable if it can live within the constraints of transformable code. E.g. users should never be forced to use custom attributes or other Hastlayer-specific elements in their code if the same effect can be achieved with runtime configuration (think about how members to be processed are configured: when running Hastlayer, not with attributes).
- If some code uses unsupported constructs it should be made apparent with exceptions. The hardware implementation silently failing (or working unexpectedly) should be avoided. Exceptions should include contextual information (e.g. the full expression or method of the problematic source, link to the GitHub issue on adding support for it; see the `AddParentEntityName()` extension method) and offer hints on possible solutions.


## Maintaining the flavors of the Hastlayer solution

Generally the *client* branch should be only merged to, but never from, the *dev* branch.

There are separate solution files for the two flavors that only differ in whether they include *Hast.Core*. Should the solution change then make the changes in the dev solution file, copy it over the client one and remove the *Hast.Core* solution folder. If you switch between the two solutions while on the *dev* branch then temporarily configure the Hastlayer shell to use the Client flavor (see `IHastlayerConfiguration`).

To allow the same code in the samples and elsewhere to support both scenarios dynamic library loading is utilized. For this to work *Hast.Core* projects should adhere to the following:

- The built DLL name to start with "Hast."
- Their projects need to be located in a subfolder of *Hast.Core*.
- Both the Debug and Release build output directories should be set just to *bin\\*.

If a *Hast.Core* projects needs to have types accessible in the Client flavor then create a corresponding `Abstractions` project. Such projects should follow the same rules listed above as *Hast.Core* projects with the only difference being that they should be located in a subfolder of *Hast.Abstractions*. Exceptions are projects that are directly added as imported extensions in `Hast.Layer`: Those can be just normal projects (like `Hast.Transformer.Abstractions`).

Note that if either kinds of projects reference another project that should be treated in the same way, with a manifest file and build output directories set (see e.g. `Hast.VhdlBuilder`).

When merging from the `dev` to the `client` branch make sure to not merge entries for newly added *Hast.Core* subrepos (or remove them after the merge).


## When creating a new project

If you add a new project to the solution make sure link the *SharedAssemblyInfo.cs* file from the root to its Properties node. See the comments in the file on what that is.


## Notes on dynamically linked projects

Be aware that Hastlayer is a modular application using dynamic linking to scan for and attach non-essential components; for example the Core projects. Because of this if you change something in those (i.e. in projects that are not directly or indirectly statically referenced from the currently executing assembly), then you need to explicitly build the projects in question so msbuild can deploy them to the dependencies folder.