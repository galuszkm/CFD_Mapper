# CFD Pressure Mapper

Convert fluid pressure from CFD analysis into a boundary condition of Finite Element model.\
[Download "CFD_Mapper.zip"](https://github.com/galuszkm/CFD_Mapper/raw/main/bin/CFD_Mapper.zip) from "bin" directory to test it!

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/IntroPicture.png" width="800" height="500" />

## Introduction

CFD simulation is a commonly used tool to identify pressure distribution along the structure walls.
Usually FE mesh does not match with CFD cell grid, therefore load transfer across the domains is not trival.
This program provides a simple and effective mapping procedure between completely independent fluid and structural models.

Fluid flow analysis can be performed with any solver (Fluent, CFX, OpenFOAM, etc.)
Currently only LS-Dyna and MSC.Marc FE models are supported as a mapping target.

## Example

