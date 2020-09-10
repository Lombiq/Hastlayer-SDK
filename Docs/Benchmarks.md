# Benchmarks



Here are some basic performance benchmarks on how Hastlayer-accelerated code compares to standard .NET. Since with FPGAs you're not running a program on a processor like a CPU or GPU but rather you create a processor out of your algorithm direct comparisons are hard. Nevertheless, here we tried to compare FPGAs and host PCs (CPUs) with roughly on the same level (e.g. comparing a mid-tier CPU to a mid-tier FPGA). All the algorithms are samples in the Hastlayer solution and available for you to check.


## Notes on the hardware used

- "Vitis": [Xilinx Vitis Unified Software Platform](https://www.xilinx.com/products/design-tools/vitis/vitis-platform.html) cards were used (eg. [Alveo U280 Data Center Accelerator Card](https://www.xilinx.com/products/boards-and-kits/alveo/u280.html)).
- "Catapult": [Microsoft Project Catapult](https://www.microsoft.com/en-us/research/project/project-catapult/) servers used via the [Project Catapult Academic Program](https://www.microsoft.com/en-us/research/academic-program/project-catapult-academic-program/). These contain the following hardware:
    - FPGA: Mt Granite card with an Altera Stratix V 5SGSMD5H2F35 FPGA and two channels of 4 GB DDR3 RAM, connected to the host via PCIe Gen3 x8. Main clock is 150 Mhz, power consumption is at most 29 W (source: "[A Cloud-Scale Acceleration Architecture](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/10/Cloud-Scale-Acceleration-Architecture.pdf)").
    - Host PC: 2 x Intel Xeon E5-2450 CPUs with 16 physical, 32 logical cores each, with a base clock of 2.1 GHz. Power consumption is around 95 W under load (based on [the processor's TDP](https://ark.intel.com/content/www/us/en/ark/products/64611/intel-xeon-processor-e5-2450-20m-cache-2-10-ghz-8-00-gt-s-intel-qpi.html); this is just a rough number and power draw is likely larger when the CPU increases its clock speed under load)
- "i7 CPU": Intel Core i7-960 CPU with 4 physical, 8 logical cores and a base clock of 3.2 Ghz. Power consumption is around 130 W under load (based on [the processor's TDP](https://ark.intel.com/content/www/us/en/ark/products/37151/intel-core-i7-960-processor-8m-cache-3-20-ghz-4-80-gt-s-intel-qpi.html)).
- "Nexys": [Nexys A7-100T FPGA board](https://store.digilentinc.com/nexys-a7-fpga-trainer-board-recommended-for-ece-curriculum/) with a Xilinx XC7A100T-1CSG324C FPGA of the Artix-7 family, with 110 MB of user-accessible DDR2 RAM. Main clock is 100 Mhz, power consumption is at most about 2.5 W (corresponding to the maximal power draw via a USB 2.0 port). The communication channel used was the serial one: Virtual serial port via USB 2.0 with a baud rate of 230400 b/s.


## Measurements

Here you can find some measurements of execution times of various algorithms on different platforms. Note:

- Measurements were made with binaries built in Release mode.
- Figures are rounded to the nearest integer.
- Speed and power advantage means the execution time and power consumption advantage of the Hastlayer-accelerated FPGA implementation. E.g. a 100% speed advantage means that the Hastlayer-accelerated implementation took half the time to finish than the original CPU one.
- Degree of parallelism indicates the level of parallelization in the algorithm (typically the number of concurrent `Task`s). For details on these check out the source of the respective algorithm (these are all classes in the Hastlayer SDK's solution). Note that the degree of parallelism, as indicated, differs between platforms. On every platform the highest possible parallelism was used (so CPUs were under full load and thus peak power consumption can be reasonably assumed).
- For CPU execution always the lowest achieved number is used to disregard noise. So this is an optimistic approach and in real life the CPU executions will most possibly be slower.
- Power consumption is an approximation based on hardware details above. For PCs it only contains the power consumption of the CPU(s). For FPGA measurements the "total" time is used (though presumably when just communication is running the power consumption is much lower than when computations are being executed).
- FPGA resource utilization figures are based on the "main" resource's utilization with all other resource types assumed to be below 100%. For Xilinx FPGAs the main resource type is LUT, for Intel (Altera) ones ALM.
- For FPGA measurements "total" means the total execution time, including the communication latency of the FPGA; since this varies because of the host PC's load the lowest achieved number is used. "Net" means just the execution of the algorithm itself on the FPGA, not including the time it took to send data to and receive from the device; FPGA execution time is deterministic and doesn't vary significantly. With faster communication channels "total" can be closer to "net". If the input and output data is small then the two measurements will practically be the same.

### Vitis

Comparing the performance of a Vitis platform FPGA (Xilinx Alveo U280) to the host PC's performance on a [Nimbix](https://www.nimbix.net/alveo) "Xilinx Vitis Unified Software Platform 2019.2" instance. Only a single CPU is assumed to be running under 100% load for the power usage figures for the sake of simplicity.


| Algorithm             | Speed advantage | Power advantage |   Parallelism  |   CPU  | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power |
|:----------------------|:---------------:|:---------------:|:--------------:|:------:|:---------:|:----------------:|:--------:|:----------:|:----------:|
| ImageContrastModifier |       568%      |       ???%      |        25      | 568 ms |   ?? Ws   |        ??%       |   28 ms  |    85 ms   |   ?? Ws    |
| ImageContrastModifier |       620%      |       ???%      | 150<sup>1</sup>| 568 ms |   ?? Ws   |        ??%       |   24 ms  |    79 ms   |   ?? Ws    |

<sup>1</sup>More could fit actually, needs more testing.

### Catapult

Comparing the performance of the Catapult FPGA (i.e. the Mt Granite card) to the Catapult node's host PC's performance. Only a single CPU is assumed to be running under 100% load for the power usage figures for the sake of simplicity.


| Algorithm             | Speed advantage | Power advantage |   Parallelism  |   CPU  | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power |
|:----------------------|:---------------:|:---------------:|:--------------:|:------:|:---------:|:----------------:|:--------:|:----------:|:----------:|
| ImageContrastModifier |       10%       |       255%      | 50<sup>1</sup> | 817 ms |   78 Ws   |        59%       |   65 ms  |   743 ms   |   22 Ws    |
| MonteCarloPiEstimator |      535%       |      1500%      |       350      | 165 ms |   16 Ws   |  56%<sup>2</sup> |   26 ms  |    26 ms   |    1 Ws    |
| ParallelAlgorithm     |       99%       |       535%      |       650      | 397 ms |   38 Ws   |        80%       |  200 ms  |   200 ms   |    6 Ws    |


<sup>1</sup>More would fit actually, needs more testing.

<sup>2</sup>Uses 88% of the DSPs. With a degree of parallelism of 400 it would be 101% of DSPs.

### Nexys

Comparing the performance of the Nexys A7-100T FPGA board to a host PC with an Intel Core i7-960 CPU.

| Algorithm                         | Speed advantage | Power advantage |   Parallelism  |   CPU   | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power |
|-----------------------------------|:---------------:|:---------------:|:--------------:|:-------:|:---------:|:----------------:|:--------:|:----------:|:----------:|
| ImageContrastModifier<sup>1</sup> |     -398550%    |      -679%      |       25       |  148 ms |   19 Ws   |        66%       |  147 ms  |  59000 ms  |   148 Ws   |
| MonteCarloPiEstimator             |       15%       |      5233%      | 78<sup>2</sup> |  120 ms |   16 Ws   |        61%       |   34 ms  |   104 ms   |   0.3 Ws   |
| ParallelAlgorithm                 |       391%      |      23600%     |270<sup>3</sup> | 1818 ms |   236 Ws  |        77%       |  300 ms  |   370 ms   |    1 Ws    |

<sup>1</sup>The low degree of parallelism available due to the resource constraints of the FPGA coupled with the slow serial connection makes this sample worse than on the CPU. Due to data transfer using only a fraction of the resources compared to doing the actual computations the power advantage of the FPGA implementation is most possibly closer to +4700%.

<sup>2</sup> With a degree of parallelism of 79 the FPGA resource utilization would jump to 101% so this is the limit of efficiency.

<sup>3</sup> With a degree of parallelism of 270 the resource utilization goes above 90% (94% post-synthesis) and the implementation step of bitstream generation fails.

### Further data

- In the ["High-level .NET Software Implementations of Unum Type I and Posit with Simultaneous FPGA Implementation Using Hastlayer" whitepaper](https://dl.acm.org/authorize?N659104) presented at the CoNGA 2018 conference the performance and clock cycle efficiency (which can be roughly equated to power efficiency) of operations of the posit floating point number format are compared. While the FPGA implementation is about 10x slower it's about 2-3x more power efficient.
- While details can't be disclosed an Italian company observed a 10x speed increase of various high-frequency trading algorithms, compared to the original C++ implementation.
