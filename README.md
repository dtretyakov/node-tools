# Node.js Tools for Visual Studio 2012

*Node Tools* is an extension which provides support for editing and debugging node.js applications.

## Table Of Contents
* [Features](#features)
* [Installation](#installation)
* [Configuration](#configuration)
* [Contribution](#contribution)
* [License](#license)

## Features

### Node.js Projects

Node Tools package extends Visual Studio project system by njsproj type. Project templates can be found in the _New Project_ dialog under _JavaScript -> Web_ subcategory.

### Debugger

Node Tools brings full debugging capability for a node.js applications. You navigate thought stacktraces (backtraces), change variable values, set conditional breakpoints and breaks on JavaScript exceptions.

### Node Package Manager Console

Since npm is a standard de-facto for node.js modules Node Tools provides special console window for a package management purposes. So you can easily type usual commands inside of Visual Studio.

### Nuget Package Manager

Also do not forget about regular nuget packages which can allow your scripts be up to date. Just use _Nuget Package Manager_ dialog or console window.

## Installation

Installation process is really straightforward:

* Install package inside Visual Studio by typing "Node Tools" in the _Extensions and Updates_ dialog or download it from [Visual Studio Gallery page](http://visualstudiogallery.msdn.microsoft.com/885a8a68-e38b-4e6a-b96d-083d5572b645)
* Download & install node.js installer from the [download page](http://nodejs.org/download/)
* Download & extract archive with a node.js source code from the [download page](http://nodejs.org/download/)

## Configuration

### Node Tools Settings

Node Tools can automatically determine node.exe interpreter location, but if you want to launch concrete node.js interpreter version you can do that in the Tools -> Options menu in the Node.js Tools dialog.
Here you can change following settings:

* Node.js location
* Node.js startup parameters

### Node.js Source Code Debugging

Currently to provide Visual Studio ability to navigate throught stacktraces during debugging session you should:

* Open properties dialog from the solution context menu
* Select _Debug Source Files_ under _Common Properties_ category
* Add to list src and lib folders from the extracted node.js source code archive (see installation section) 

### Node.js Debugger Settings

By default node.js lunched to accept debigging sessions on the port 5858. You can customize that at the project settings.
Here you can change following settings:

* Debug port
* Startup file

## Contribution

Your feedback are very welcome. Please feel free to create issues and write you comments.
If you want to provide code contribution please fork this project and create a pull request.

### Launching Project

* Clone a repository and open a NodeTools.sln
* Set NodeTools project as startup and open project properties
* In the Debug screen set _Start external program_ and select devenv.exe, in the _Command line arguments_ set /RootSuffix Exp

## License

Project source code is licensed under Apache 2.0. It contains portions of code from Microsoft Corporation and Outercurve Foundation.