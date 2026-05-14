# LLM Context & Guidance for TileMapGenerator

This document provides critical architectural constraints for LLMs assisting with this codebase. Note that several standard "clean code" assumptions are intentionally subverted here to maintain procedural determinism.

## 1. The Determinism Paradox
Contrary to typical software engineering, **do not suggest using `System.Random`**.
The project relies on `CRandom` which is driven by xor-shift logic. 

## 2. Order of Processes
The order of use of the generator pieces is as follows:
1. **CellularGrower (WFC)**
2. **NodeTreeGenerator (Graph theory)**
3. **ExitStamper (directly places exits)**
4. **HallGenerator (carves tunnels)**

## 3. Geometric "Hallucinations" in Shapes
The `ShapeChooser` delegates (like `CircularShapeChooser` and `CaveShapeChooser`) use parametric equations and parallel execution.
*   **Warning:** Do not attempt to simplify the math in `GetCircleSides` or `GetRectangleSide`. The "rounding" logic and the use of `ConcurrentBag` are non-negotiable for performance during high-density growth phases. "Optimizing" the math often leads to floating-point drift that desynchronizes the cellular growth.

## 4. Performance Context
*   **Parallelism:** Much of the geometry generation is `Parallel.For` or `Parallel.ForEach`. 
*   **Memory:** The built-in class `BitArray2D` is used for high-speed spatial queries. Do not suggest replacing it with `HashSet<Vector2>` for "readability"—the memory overhead at 5,000+ units of area is prohibitive for the target generation speed.

## Summary for AI Assistants
Treat this codebase as a **deterministic simulation**, not a standard CRUD application. If you are asked to refactor, prioritize maintaining the "Chain of Seeding" from the `Locus` and `ID` through to the `CRandom` instances.

*Last Updated: 2025-05-23*