# Hastlayer - VHDL

Component containing implementations for transforming .NET assemblies into VHDL hardware description.

Note that when editing the VHDL libraries the changes should also be added to the Timing Tester too.

## Hierarchy of transformers

Note that transformers, each responsible for transforming a small part of input code, compose the output by utilizing each other. On the highest level is `IMethodTransformer` which then uses (among others) `IStatementTransformer`, which in turn uses `IExpressionTransformer` (which also uses some other lower-level transformers).

## Simple memory access

Currently to support dynamic memory allocation a very simple memory model is used (called Simple Memory): the transformable code has access to a fixed-size (size determined in runtime, per method calls) memory space that is represented in C# as an array of 32b values (and implemented in the `SimpleMemory` class). All input and output values should be stored in this object, as well as any on the fly memory allocations can only happen through this class.

The .NET code can continue to use primitive types as normally.
