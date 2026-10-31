# Hastlayer - Sample Assembly

A project that contains Hastlayer-compatible sample algorithms that can be transformed from another project.

`PositUnaryCalculator` provides bounded raw-memory entry points for `Posit8E2`, `Posit16E2`, `Posit32E2`, and `Posit32`.
Allocate nine 32-bit cells: cell 0 holds the input encoding, and cells 1-8 receive negate, absolute value, posit-valued
sign, nearest integer (even-integer ties), ceiling, floor, next, and prior, in that order. Narrow encodings occupy the
low 8/16 bits. NaR propagates except through next/prior, which traverse representations with wraparound.
CPU tests check all 8/16-bit encodings and independent exact 32-bit boundary/seeded vectors; the Catapult VHDL
regression covers transformation. Vendor synthesis, simulation, and device execution remain deferred.
This source-only sample is excluded from the NuGet test solution until its APIs exist in the published package.

`PositAdditionCalculator` provides bounded raw-memory entry points for `Posit8E2`, `Posit16E2`, `Posit32E2`, and
`Posit32`. Allocate eight 32-bit cells: cells 0-2 hold the encodings a, b, and c, and cells 3-7 receive a + b, a - b,
(a + b) - c, a + (b - c), and (a - c) + b, rounding once after every operator in source order. Narrow encodings occupy
the low 8/16 bits. CPU tests check all 65,536 posit8 pairs and hard/seeded 16/32-bit pairs and triples against an
independent exact-sum oracle, including triples whose explicitly fused sum differs; the Catapult VHDL regression covers
transformation. Vendor synthesis, simulation, and device execution remain deferred.
This source-only sample is excluded from the NuGet test solution until its APIs exist in the published package.

`PositMultiplicationCalculator` provides bounded raw-memory entry points for `Posit8E2`, `Posit16E2`, `Posit32E2`, and
`Posit32`. Allocate three 32-bit cells: cells 0 and 1 hold the operand encodings, and cell 2 receives their product,
rounded once from the exact value as the Posit Standard (2022) defines in sections 4.1 and 5.4. Narrow encodings
occupy the low 8/16 bits. NaR is decided before zero, so zero times NaR is NaR. CPU tests check all 65,536 posit8
pairs and independent exact hard/seeded 16/32-bit vectors; the Catapult VHDL regression covers transformation. Vendor
synthesis, simulation, and device execution remain deferred.
This source-only sample is excluded from the NuGet test solution until its APIs exist in the published package.
