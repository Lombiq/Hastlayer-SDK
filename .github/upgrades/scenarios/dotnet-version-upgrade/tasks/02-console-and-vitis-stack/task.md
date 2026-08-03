# 02-console-and-vitis-stack: Upgrade the console entry point and the Vitis/Xilinx dependency slice

Upgrade `Hast.Console` as the first entry-point application together with the dependency chain it exercises: `Hast.Vitis`, `Hast.Vitis.HardwareFramework`, `Hast.Xilinx`, `Hast.Synthesis`, `Hast.Common`, and the Helpful Libraries REST support it consumes. The assessment flags API issues in `Hast.Vitis`, `Hast.Xilinx`, and related shared libraries, making this the smallest high-value application slice that will expose real .NET 10 code changes early.

This task should also absorb any direct package and source fixes needed in the Vitis hardware submodule and supporting projects so the console scenario can build cleanly on .NET 10. Start research with the CodeDom and API-compatibility findings in the Vitis/Xilinx path, plus any changes introduced by the refreshed submodule revisions.

**Done when**: `Hast.Console` and its Vitis/Xilinx dependency slice target .NET 10 successfully, the relevant projects build warning-free, and any directly affected tests or verification commands for this slice pass.
