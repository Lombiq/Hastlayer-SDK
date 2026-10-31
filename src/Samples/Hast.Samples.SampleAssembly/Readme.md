# Hastlayer - Sample Assembly

A project that contains Hastlayer-compatible sample algorithms that can be transformed from another project.

`PositUnaryCalculator` provides bounded raw-memory entry points for `Posit8E2`, `Posit16E2`, `Posit32E2`, and `Posit32`.
Allocate nine 32-bit cells: cell 0 holds the input encoding, and cells 1-8 receive negate, absolute value, posit-valued
sign, nearest integer (even-integer ties), ceiling, floor, next, and prior, in that order. Narrow encodings occupy the
low 8/16 bits. NaR propagates except through next/prior, which traverse representations with wraparound.
CPU tests check all 8/16-bit encodings and independent exact 32-bit boundary/seeded vectors; the Catapult VHDL
regression covers transformation. Vendor synthesis, simulation, and device execution remain deferred.
This source-only sample is excluded from the NuGet test solution until its APIs exist in the published package.

`PositDivisionCalculator` provides bounded raw-memory entry points for `Posit8E2`, `Posit16E2`, `Posit32E2`, and
`Posit32` division. Allocate three 32-bit cells: cells 0 and 1 hold the dividend and divisor encodings, and cell 2
receives the correctly rounded quotient. Narrow encodings occupy the low 8/16 bits. NaR operands and division by zero
give NaR. CPU tests check all posit8 pairs and independent exact 16/32-bit boundary, power-of-two, near-rounding-boundary
and seeded pairs; the Catapult VHDL regression covers transformation. Vendor synthesis, simulation, and device execution
remain deferred. This source-only sample is excluded from the NuGet test solution like `PositUnaryCalculator`.
