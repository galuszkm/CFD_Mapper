# CFD Pressure Mapper

Convert fluid pressure from CFD analysis into a boundary condition of Finite Element model.\
[Download "CFD_Mapper.zip"](https://github.com/galuszkm/CFD_Mapper/raw/main/bin/CFD_Mapper.zip) from "bin" directory to test it!

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/Intro.png"/>


## Introduction

CFD Mapper is a stand-alone application developed to convert fuid pressure into a boundary condition of Finite Element model. It is an open-source software, supports 3D data visualization and parallel computing.<br/>

CFD simulation is a commonly used tool to identify pressure distribution along the structure walls. Usually FE mesh does not match with CFD cell grid. In this case
mapping procedure needs to be involved to transfer load across the domains.<br/>
First reason of creating this program was to couple independent CFD and FEA solvers. Advanced simulation environments like ANSYS offer similar tools with, frankly speaking, quite limited features. The problem starts if you want to mix different software. Second reason was a mapping outcome visualization. Obviously, everyone wants to check what is the actual distribution of the load applied to the structure, but not all solvers create this kind of output. Therefore, I put great effort into effcient graphics rendering using third party packages.<br/>

Main idea behind the code is distance based pressure mapping from CFD points to finite element faces with optional averaging called pressure smoothing. Application is compiled to single file assembly (.exe) with all .dll files embedded - no additional packages or installation is needed to run it.<br/>

Fluid flow analysis can be performed with any solver (Fluent, CFX, OpenFOAM, etc.). Currently only LS-Dyna and MSC.Marc FE models are supported as a mapping target.


## Getting started

Application is quite well documented in [Manual](https://github.com/galuszkm/CFD_Mapper/blob/main/doc/CFD_Mapper_Doc.pdf).

Fluid flow analysis can be performed with any solver (Fluent, CFX, OpenFOAM, etc.). You can use any post-processor to export pressure data at the specified fluid domain boundary. The only requirement is to keep 4 column ASCII format of input file: x, y, z coordinates and pressure value.

CFD input:
```
 X [ m ]         Y [ m ]         Z [ m ]         Total Pressure [ Pa ]
-1.50302556E-04, -1.00666806E-02, 3.60378213E-02, 1.74407488E+06
-1.50302556E-04, -1.00666638E-02, 3.55752371E-02, 1.64430800E+06
-1.46276827E-04, -1.00662326E-02, 3.41895483E-02, 1.60428963E+06
...
```
Finite element model input could be Marc Input (.dat) or LS-Dyna input deck (.k). In Marc set of faces called "Pressure faces" needs to be defined. In LS-Dyna SET_SEGMENT keyword is required.

Marc input:
```
define              facemt              set                 Pressure_faces
            9224:3            9225:3            9226:3            9227:3            9228:3            9229:3            9230:3            9231:3   c
            9232:3            9233:3            9234:3            9235:3            9236:3            9237:3            9238:3            9239:3   c
            9240:3            9241:3            9242:3            9243:3            9244:3            9245:3            9246:3            9247:3   c
...
```

LS-Dyna input:
```
*NODE
    2081             0.0          -150.0          -106.0                
    2100             0.0          -150.0          -133.0
...
*SET_SEGMENT
$#      n1        n2        n3        n4        a1        a2        a3        a4
    530274    530892    530894    530276       0.0       0.0       0.0       0.0
    136550    136548    139448    139450       0.0       0.0       0.0       0.0
...
```

In CFD Mapper pressure input data are represented by points with color specified by default scale (min/max). You can modify CFD data appearance with Color settings subpanel. Use CFD point size box to increase/decrease vertex size. Colorbar scale could be adjusted with Color range boxes.
FE faces have grey color in Plot input mode. Wireframe and transparency could be turn on/off in View section in Top toolbar. You can also create clip plane normal to X, Y or Z axis using Clip plane subpanel.

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/GUI.PNG">


<br>Plot output button is enabled after mapping, click it to evaluate the outcome. You can quickly switch between input and output data to compare them, color scale remains unchanged. In output mode CFD points are hidden and faces with no pressure assignment have gray color. <br/><br><br/>

<img src="https://github.com/galuszkm/CFD_Mapper/blob/main/other/Input_Output.png">

See [Manual](https://github.com/galuszkm/CFD_Mapper/blob/main/doc/CFD_Mapper_Doc.pdf) for more details!
