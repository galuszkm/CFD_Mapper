# CFD Pressure Mapper

Convert fluid pressure from CFD analysis into a boundary condition of Finite Element model.\
[Download "CFD_Mapper.zip"](https://github.com/galuszkm/CFD_Mapper/raw/main/bin/CFD_Mapper.zip) from "bin" directory to test it!

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/Intro.png"/>

## Introduction

CFD Mapper is a stand-alone application developed to convert fuid pressure into a boundary condition of Finite Element model. It is an open-source software, supports 3D data visualization and parallel computing. 
CFD simulation is a commonly used tool to identify pressure distribution along the structure walls. Usually FE mesh does not match with CFD cell grid. In this case
mapping procedure needs to be involved to transfer load across the domains.
First reason of creating this program was to couple independent CFD and FEA solvers. Advanced simulation environments like ANSYS offer similar tools with, frankly speaking, quite limited features. The problem starts if you want to mix different software. Second reason was a mapping outcome visualization. Obviously, everyone wants to check what is the actual distribution of the load applied to the structure, but not all solvers create this kind of output. Therefore, I put great effort into effcient graphics rendering using third party packages.
Main idea behind the code is distance based pressure mapping from CFD points to finite element faces with optional averaging called pressure smoothing. Application is compiled to single file assembly (.exe) with all .dll files embedded - no additional packages or installation is needed to run it.

Fluid flow analysis can be performed with any solver (Fluent, CFX, OpenFOAM, etc.). Currently only LS-Dyna and MSC.Marc FE models are supported as a mapping target.

## Getting started

Application is quite well documented in [Manual](https://github.com/galuszkm/CFD_Mapper/blob/main/doc/CFD_Mapper_Doc.pdf).

Fluid flow analysis can be performed with any solver (Fluent, CFX, OpenFOAM, etc.). You can use any post-processor to export pressure data at the specified fluid domain boundary. The only requirement is to keep 4 column ASCII format of input file: x, y, z coordinates and pressure value.

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/CFD_Format.PNG"/>

Finite element model input could be Marc Input (.dat) or LS-Dyna input deck (.k). In Marc set of faces called "Pressure faces" needs to be defined. In LS-Dyna SET_SEGMENT keyword is required.

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/Marc_input.PNG"/>
<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/LsDyna_input.PNG"/>

In CFD Mapper pressure input data are represented by points with color specified by default scale (min/max). You can modify CFD data appearance with Color settings subpanel. Use CFD point size box to increase/decrease vertex size. Colorbar scale could be adjusted with Color range boxes.
FE faces have grey color in Plot input mode. Wireframe and transparency could be turn on/off in View section in Top toolbar. You can also create clip plane normal to X, Y or Z axis using Clip plane subpanel.

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/GUI.PNG">

Plot output button is enabled after mapping, click it to evaluate the outcome. You can quickly switch between input and output data to compare them, color scale remains unchanged. In output mode CFD points are hidden and faces with no pressure assignment have gray color.

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/Input_Output.png">

See [Manual](https://github.com/galuszkm/CFD_Mapper/blob/main/doc/CFD_Mapper_Doc.pdf) for more details!
