# Benchmarks



Here are some basic performance benchmarks on how Hastlayer-accelerated code compares to standard .NET. Since with FPGAs you're not running a program on a processor like a CPU or GPU but rather you create a processor out of your algorithm direct comparisons are hard. Nevertheless, here we tried to compare FPGAs and host PCs (CPUs) with roughly on the same level (e.g. comparing a mid-tier CPU to a mid-tier FPGA). All the algorithms are samples in the Hastlayer solution and available for you to check.


## Notes on measurements

Here you can find some measurements of execution times of various algorithms on different platforms. Note:

- Measurements were made with binaries built in Release mode.
- Measurements were always taken from the second or third run of the application to disregard initialization time. 
- Figures are rounded to the nearest integer.
- Speed and power advantage means the execution time and power consumption advantage of the Hastlayer-accelerated FPGA implementation. E.g. a 100% speed advantage means that the Hastlayer-accelerated implementation took half the time to finish than the original CPU one.
- Degree of parallelism indicates the level of parallelization in the algorithm (typically the number of concurrent `Task`s). For details on these check out the source of the respective algorithm (these are all classes in the Hastlayer SDK's solution). Note that the degree of parallelism, as indicated, differs between platforms. On every platform the highest possible parallelism was used (so CPUs were under full load and thus peak power consumption can be reasonably assumed).
- For CPU execution always the lowest achieved number is used to disregard noise. So this is an optimistic approach and in real life the CPU executions will most possibly be slower.
- Power consumption is an approximation based on hardware details. For PCs it only contains the power consumption of the CPU(s). For FPGA measurements the "total" time is used (though presumably when just communication is running the power consumption is much lower than when computations are being executed).
- FPGA resource utilization figures are based on the "main" resource's utilization with all other resource types assumed to be below 100%. For Xilinx FPGAs the main resource type is LUT, for Intel (Altera) ones ALM.
- For FPGA measurements "total" means the total execution time, including the communication latency of the FPGA; since this varies because of the host PC's load the lowest achieved number is used. "Net" means just the execution of the algorithm itself on the FPGA, not including the time it took to send data to and receive from the device; FPGA execution time is deterministic and doesn't vary significantly. With faster communication channels "total" can be closer to "net". If the input and output data is small then the two measurements will practically be the same.


## Vitis

Comparing the performance of a Vitis platform FPGA (Xilinx Alveo U280/250/200/50) to the host PC's performance on a [Nimbix](https://www.nimbix.net/alveo) instance.

### Details

- FPGA: The following [Xilinx Vitis Unified Software Platform](https://www.xilinx.com/products/design-tools/vitis/vitis-platform.html) cards were used:
  - [Alveo U280 Data Center Accelerator Card](https://www.xilinx.com/products/boards-and-kits/alveo/u280.html), PCI Express® Gen3 x16, 225 W Maximum Total Power
  - [Alveo U250 Data Center Accelerator Card](https://www.xilinx.com/products/boards-and-kits/alveo/u250.html), PCI Express® Gen3 x16, 225 W Maximum Total Power
  - [Alveo U200 Data Center Accelerator Card](https://www.xilinx.com/products/boards-and-kits/alveo/u200.html), PCI Express® Gen3 x16, 225 W Maximum Total Power
  - [Alveo U50 Data Center Accelerator Card](https://www.xilinx.com/products/boards-and-kits/alveo/u50.html), PCI Express® Gen3 x16, 75 W Maximum Total Power
- Host: A [Nimbix](https://www.nimbix.net/alveo) "Xilinx Vitis Unified Software Platform 2020.1" instance with 16 x Intel Xeon E5-2640 v3 CPUs with 8 physical, 16 logical cores each, with a base clock of 2.6 GHz. Power consumption is around 90 W under load (based on the processor's TDP, [see here](https://ark.intel.com/content/www/us/en/ark/products/83359/intel-xeon-processor-e5-2640-v3-20m-cache-2-60-ghz.html); the power draw is likely larger when the CPU increases its clock speed under load).
- Only a single CPU is assumed to be running under 100% load for the power usage figures for the sake of simplicity. The table has a matching [Excel sheet](Attachments/BenchmarksVitis.xlsx) that was converted using [this VS Code extension](https://marketplace.visualstudio.com/items?itemName=csholmq.excel-to-markdown-table).
- The kernels were built on the initial 2019.2 version of the Vitis Unified Software Platform. We have seen in one case 20% improvement in frequency (leading to shorter run times and lower total power consumption) by compiling with the newer 2020.1 version.

### Measurements

| Device     | Algorithm                         | Speed advantage | Power advantage | Parallelism | CPU       | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power | FPGA on-chip power |
|------------|-----------------------------------|-----------------|-----------------|-------------|-----------|-----------|------------------|----------|------------|------------|--------------------|
| Alveo U280 | ImageContrastModifier<sup>1</sup> | 1591%           | 6505%           | 150         | 541 ms    | 49 Ws     | 21.44%           | 12 ms    | 32 ms      | 0.74 Ws    | 23.04 W            |
| Alveo U280 | ImageContrastModifier<sup>3</sup> | 3414%           | 13629%          | 150         | 17359 ms  | 1562 Ws   | 21.44%           | 459 ms   | 494 ms     | 11.38 Ws   | 23.04 W            |
| Alveo U280 | ParallelAlgorithm                 | 226%            | 1858%           | 300         | 362 ms    | 33 Ws     | 10.86%           | 102 ms   | 111 ms     | 1.66 Ws    | 14.99 W            |
| Alveo U280 | MonteCarloPiEstimator             | 387%            | 2397%           | 230         | 185 ms    | 17 Ws     | 13.63%           | 16 ms    | 38 ms      | 0.67 Ws    | 17.55 W            |
| Alveo U250 | ImageContrastModifier<sup>1</sup> | 1503%           | 5621%           | 150         | 529 ms    | 48 Ws     | 18.29%           | 13 ms    | 33 ms      | 0.83 Ws    | 25.22 W            |
| Alveo U250 | ImageContrastModifier<sup>2</sup> | 3268%           | 11921%          | 150         | 193158 ms | 17384 Ws  | 18.29%           | 5535 ms  | 5735 ms    | 144.61 Ws  | 25.22 W            |
| Alveo U250 | ParallelAlgorithm                 | 357%            | 2437%           | 300         | 498 ms    | 45 Ws     | 10.30%           | 101 ms   | 109 ms     | 1.77 Ws    | 16.21 W            |
| Alveo U250 | MonteCarloPiEstimator             | 369%            | 2022%           | 230         | 197 ms    | 18 Ws     | 12.39%           | 21 ms    | 42 ms      | 0.84 Ws    | 19.89 W            |
| Alveo U200 | ImageContrastModifier<sup>1</sup> | 735%            | 3047%           | 150         | 543 ms    | 49 Ws     | 27.23%           | 12 ms    | 65 ms      | 1.55 Ws    | 23.89 W            |
| Alveo U200 | ImageContrastModifier<sup>2</sup> | 3448%           | 13266%          | 150         | 198172 ms | 17835 Ws  | 27.23%           | 5340 ms  | 5586 ms    | 133.44 Ws  | 23.89 W            |
| Alveo U200 | ParallelAlgorithm                 | 171%            | 1464%           | 300         | 379 ms    | 34 Ws     | 15.56%           | 110 ms   | 140 ms     | 2.18 Ws    | 15.58 W            |
| Alveo U200 | MonteCarloPiEstimator             | 203%            | 1333%           | 230         | 203 ms    | 18 Ws     | 18.57%           | 17 ms    | 67 ms      | 1.28 Ws    | 19.04 W            |
| Alveo U50  | ImageContrastModifier<sup>1</sup> | 1324%           | 6359%           | 150         | 470 ms    | 42 Ws     | 32.09%           | 12 ms    | 33 ms      | 0.65 Ws    | 19.85 W            |
| Alveo U50  | ImageContrastModifier<sup>3</sup> | 3462%           | 16052%          | 150         | 17167 ms  | 1545 Ws   | 32.09%           | 450 ms   | 482 ms     | 9.57 Ws    | 19.85 W            |
| Alveo U50  | ParallelAlgorithm                 | 258%            | 2653%           | 300         | 379 ms    | 34 Ws     | 16.22%           | 104 ms   | 106 ms     | 1.24 Ws    | 11.69 W            |
| Alveo U50  | MonteCarloPiEstimator             | 348%            | 2693%           | 230         | 197 ms    | 18 Ws     | 20.37%           | 18 ms    | 44 ms      | 0.63 Ws    | 14.43 W            |

1. Using the default 0.2MP image `fpga.jpg`.
2. Using the larger [73.2MP image](https://photographingspace.com/wp-content/uploads/2019/10/2019JulyLunarEclipse-Moon0655-CorySchmitz-PI2_wm-web.jpg).
3. Using the scaled down [6.4MP image](https://photographingspace.com/wp-content/uploads/2019/10/2019JulyLunarEclipse-Moon0655-CorySchmitz-PI2_wm-web50pct-square-scaled.jpg) because the testing binary was built for the High Bandwidth Memory. Currently only one HBM slot is supported, meaning that the available memory without disabling HBM is 256MB.
4. The same applies from the previous point, with the added limitation that the Alveo U50 card doesn't have any DDR memory, so using HBM is the only option.

We used the following shell function to test: 
```shell
function benchmark() {
	name=$1
	sample=$2
	device=$3

	echo -e "\n\n\n\n\n\n\n\n$name"
	dotnet Hast.Samples.Consumer.dll -sample $sample -device "$device" -name "$name" > /dev/null
	dotnet Hast.Samples.Consumer.dll -sample $sample -device "$device" -name "$name" > /dev/null
	dotnet Hast.Samples.Consumer.dll -sample $sample -device "$device" -name "$name"
}
```

Inside the Hast.Samples.Consumer binary's directory, while assuming that the larger image is in the home as `moon.jpg`.
```shell
benchmark image ImageProcessingAlgorithms ImageContrastModifier > run.log
benchmark parallel ParallelAlgorithm ParallelAlgorithm >> run.log
benchmark monte MonteCarloPiEstimator MonteCarloPiEstimator >> run.log

cp ~/moon.jpg fpga.jpg
benchmark image ImageProcessingAlgorithms ImageContrastModifier > run.moon.log
```

The utilization and power usage information was inside the *HardwareFramework/reports* directory.


## Zynq

Comparing the Zynq-7000 FPGA accelerated performance to the ARM CPU on the same system-on-module. The benchmarks use the [Trenz Electric TE0715-04-30-1C](https://shop.trenz-electronic.de/en/TE0715-04-30-1C-SoC-Module-with-Xilinx-Zynq-XC7Z030-1SBG485C-1-GByte-DDR3L-SDRAM-4-x-5-cm) module connected to a [TE0706 carrier board](https://shop.trenz-electronic.de/en/TE0706-03-TE0706-Carrierboard-for-Trenz-Electronic-Modules-with-4-x-5-cm-Form-Factor) (form factor similar to a Raspberri Pi).

### Details

- FPGA: Xilinx Zynq XC7Z030-1SBG485C SoC FPGA. Main clock is 150 Mhz.
- Host: ARM dual-core Cortex-A9 MPCore CPU. Power consumption is around 95 W under load (based on the processor's TDP, [see here](https://ark.intel.com/content/www/us/en/ark/products/64611/intel-xeon-processor-e5-2450-20m-cache-2-10-ghz-8-00-gt-s-intel-qpi.html); the power draw is likely larger when the CPU increases its clock speed under load).
- Both has access to 1 GB (32-bit) DDR3L SDRAM.
- The power consumption is [unspecified in the official documentation](https://wiki.trenz-electronic.de/display/PD/TE0715+TRM#TE0715TRM-PowerConsumption), but it's less than 21.5W because that's the maximum rating of the power supply we've used. The _Power advantage_ column is omitted due to this lack of information and because of the difficulty of measuring power consumption over such short time spans with the lab equipment we can access at the moment. Assuming max consumption, it would be the same as the _Speed advantage_ columns.

### Measurements

| Algorithm             | Speed advantage |   Parallelism  |    CPU   | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power |
|:----------------------|:---------------:|:--------------:|:--------:|:---------:|:----------------:|:--------:|:----------:|:----------:|
| ImageContrastModifier |       4864%     |        25      |  2780 ms |   59.8 Ws |        41%       |   29 ms  |    56 ms   |   1.2 Ws   |
| MonteCarloPiEstimator |       8646%     |        77      |  3236 ms |   69.6 Ws |        33%       |   31 ms  |    37 ms   |   0.8 Ws   |
| ParallelAlgorithm     |       30582%    |       260      | 66273 ms | 1424.9 Ws |        55%       |  210 ms  |   216 ms   |   4.6  Ws  |

You can find more measurements in the [attached table](Attachments/TE0715-04-30-1C_benchmark.pdf).

## Catapult

Comparing the performance of the Catapult FPGA to the Catapult node's host PC's performance.

### Details

[Microsoft Project Catapult](https://www.microsoft.com/en-us/research/project/project-catapult/) servers used via the [Project Catapult Academic Program](https://www.microsoft.com/en-us/research/academic-program/project-catapult-academic-program/). These contain the following hardware:

- FPGA: Mt Granite card with an Altera Stratix V 5SGSMD5H2F35 FPGA and two channels of 4 GB DDR3 RAM, connected to the host via PCIe Gen3 x8. Main clock is 150 Mhz, power consumption is at most 29 W (source: "[A Cloud-Scale Acceleration Architecture](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/10/Cloud-Scale-Acceleration-Architecture.pdf)").
- Host: 2 x Intel Xeon E5-2450 CPUs with 16 physical, 32 logical cores each, with a base clock of 2.1 GHz. Power consumption is around 95 W under load (based on the processor's TDP, [see here](https://ark.intel.com/content/www/us/en/ark/products/64611/intel-xeon-processor-e5-2450-20m-cache-2-10-ghz-8-00-gt-s-intel-qpi.html); the power draw is likely larger when the CPU increases its clock speed under load).
- Only a single CPU is assumed to be running under 100% load for the power usage figures for the sake of simplicity.

### Measurements

| Algorithm             | Speed advantage | Power advantage |   Parallelism  |   CPU  | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power |
|:----------------------|:---------------:|:---------------:|:--------------:|:------:|:---------:|:----------------:|:--------:|:----------:|:----------:|
| ImageContrastModifier |       10%       |       255%      | 50<sup>1</sup> | 817 ms |   78 Ws   |        59%       |   65 ms  |   743 ms   |   22 Ws    |
| MonteCarloPiEstimator |      535%       |      1500%      |       350      | 165 ms |   16 Ws   |  56%<sup>2</sup> |   26 ms  |    26 ms   |    1 Ws    |
| ParallelAlgorithm     |       99%       |       535%      |       650      | 397 ms |   38 Ws   |        80%       |  200 ms  |   200 ms   |    6 Ws    |


<sup>1</sup>More would fit actually, needs more testing.

<sup>2</sup>Uses 88% of the DSPs. With a degree of parallelism of 400 it would be 101% of DSPs.


## Nexys

Comparing the performance of the Nexys A7-100T FPGA board to a host PC with an Intel Core i7-960 CPU.

### Details

- FPGA: [Nexys A7-100T FPGA board](https://store.digilentinc.com/nexys-a7-fpga-trainer-board-recommended-for-ece-curriculum/) with a Xilinx XC7A100T-1CSG324C FPGA of the Artix-7 family, with 110 MB of user-accessible DDR2 RAM. Main clock is 100 Mhz, power consumption is at most about 2.5 W (corresponding to the maximal power draw via a USB 2.0 port). The communication channel used was the serial one: Virtual serial port via USB 2.0 with a baud rate of 230400 b/s.
- Host: Intel Core i7-960 CPU with 4 physical, 8 logical cores and a base clock of 3.2 Ghz. Power consumption is around 130 W under load (based on [the processor's TDP](https://ark.intel.com/content/www/us/en/ark/products/37151/intel-core-i7-960-processor-8m-cache-3-20-ghz-4-80-gt-s-intel-qpi.html); the power draw is likely larger when the CPU increases its clock speed under load).
- Vivado and Xilinx SDK 2016.4 were used.


### Measurements

| Algorithm                         | Speed advantage | Power advantage |   Parallelism  |   CPU   | CPU power | FPGA utilization | Net FPGA | Total FPGA | FPGA power |
|-----------------------------------|:---------------:|:---------------:|:--------------:|:-------:|:---------:|:----------------:|:--------:|:----------:|:----------:|
| ImageContrastModifier<sup>1</sup> |     -398550%    |      -679%      |       25       |  148 ms |   19 Ws   |        66%       |  147 ms  |  59000 ms  |   148 Ws   |
| MonteCarloPiEstimator             |       15%       |      5233%      | 78<sup>2</sup> |  120 ms |   16 Ws   |        61%       |   34 ms  |   104 ms   |   0.3 Ws   |
| ParallelAlgorithm                 |       391%      |      23600%     |270<sup>3</sup> | 1818 ms |   236 Ws  |        77%       |  300 ms  |   370 ms   |    1 Ws    |

<sup>1</sup>The low degree of parallelism available due to the resource constraints of the FPGA coupled with the slow serial connection makes this sample worse than on the CPU. Due to data transfer using only a fraction of the resources compared to doing the actual computations the power advantage of the FPGA implementation is most possibly closer to +4700%.

<sup>2</sup> With a degree of parallelism of 79 the FPGA resource utilization would jump to 101% so this is the limit of efficiency.

<sup>3</sup> With a degree of parallelism of 270 the resource utilization goes above 90% (94% post-synthesis) and the implementation step of bitstream generation fails.


## Further data

- In the ["High-level .NET Software Implementations of Unum Type I and Posit with Simultaneous FPGA Implementation Using Hastlayer" whitepaper](https://dl.acm.org/authorize?N659104) presented at the CoNGA 2018 conference the performance and clock cycle efficiency (which can be roughly equated to power efficiency) of operations of the posit floating point number format are compared. While the FPGA implementation is about 10x slower it's about 2-3x more power efficient.
- While details can't be disclosed an Italian company observed a 10x speed increase of various high-frequency trading algorithms, compared to the original C++ implementation.


## Attribution

- The moon image is from [Cory Schmitz](https://photographingspace.com/100-megapixel-moon/). 
