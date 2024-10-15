# MakefileBuild Visual Studio Extension

## Overview

The `MakefileBuild` Visual Studio Extension adds functionality to Visual Studio to open and build Makefiles. The extension provides a command that can be triggered to build the active Makefile in the editor, using `make.exe` found in the system PATH.

## Features

- **Open Makefiles**: Recognize Makefiles in the Visual Studio environment.
- **Build Makefiles**: Execute the `make` command to build the Makefile.
- **Output Window Integration**: Displays build output and errors in the Visual Studio Output window.

## Prerequisites

To use this extension, you need to have the GNU build tools installed on your system. Ensure `make.exe` is available in your system PATH.

## Installation

To install the extension:
1. Clone this repository.
2. Open the solution in Visual Studio.
3. Build the solution to produce the `.vsix` file.
4. Run the `.vsix` file to install the extension into Visual Studio.

## Usage

1. Open a Makefile in Visual Studio.
2. Ensure `make.exe` is available in your system PATH.
3. Trigger the `MakeCommand` from the command palette.

## Code Explanation

The core functionality is implemented in the `MakeCommand` class:

- **Initialization**: The `InitializeAsync` method sets up the command and output window.
- **Execute Command**: The `Execute` method retrieves the active Makefile, identifies `make.exe` in the system PATH, and runs the `make` command.
- **Output Handling**: Standard output and error from the `make` command are captured and displayed in the Visual Studio Output window.


