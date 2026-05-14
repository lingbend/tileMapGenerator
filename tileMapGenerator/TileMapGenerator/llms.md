# LLM Context & Guidance for TileMapGenerator

## 1. Style Considerations
1. **1 Class Per file**
2. **Pascal case for properties**
3. **Snake case for internal variables**
4. **Pascal case for class names and methods**
5. **// for all comments**
6. **DON'T add new files. Ask and I will do it first if I think its necessary**
7. **Classes should be static unless otherwise is necessary**

## 2. Order of Processes
The order of use of the generator pieces is as follows:
1. **CellularGrower (WFC)**
2. **NodeTreeGenerator (Graph theory)**
3. **ExitStamper (directly places exits)**
4. **HallGenerator (carves tunnels)**

## 3. Research
Before working on any algorithmic changes, please read all the research files in the "Docs" folder.

*Last Updated: 2025-05-23*