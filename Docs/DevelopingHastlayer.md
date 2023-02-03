# Developing Hastlayer

How to work on Hastlayer itself?

## Design principles

- From a user's (i.e. using developer's) perspective Hastlayer should be as simple as possible. To achieve this e.g. use generally good default configurations so in the majority of cases there is no configuration needed.
- Software that was written previously, without knowing about Hastlayer should be usable if it can live within the constraints of transformable code. E.g. users should never be forced to use custom attributes or other Hastlayer-specific elements in their code if the same effect can be achieved with runtime configuration (think about how members to be processed are configured: when running Hastlayer, not with attributes).
- If some code uses unsupported constructs it should be made apparent with exceptions. The hardware implementation silently failing (or working unexpectedly) should be avoided. Exceptions should include contextual information (e.g. the full expression or method of the problematic source, link to the GitHub issue on adding support for it; see the `AddParentEntityName()` extension method) and offer hints on possible solutions.

## When creating a new project

If you add a new project to the solution make sure link the *SharedAssemblyInfo.cs* file from the root to its Properties node. See the comments in the file on what that is.

## Notes on dynamically linked projects

Be aware that Hastlayer is a modular application using dynamic linking to scan for and attach non-essential components; for example the Core projects. Because of this if you change something in those (i.e. in projects that are not directly or indirectly statically referenced from the currently executing assembly), then you need to explicitly build the projects in question so MSBuild can deploy them to the dependencies folder.
