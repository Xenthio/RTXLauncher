[![.NET App Publish](https://github.com/Xenthio/RTXLauncher/actions/workflows/dotnet-core-publish-cross.yml/badge.svg)](https://github.com/Xenthio/RTXLauncher/actions/workflows/dotnet-core-publish-cross.yml)

# Garry's Mod RTX Launcher (+Installer/Updater)


See also: [RTX Launcher Rust](https://github.com/sambow23/RTXLauncher-rs) 

## Workshop PBR startup assets (Windows x64)

Fixes packages that include `bin/win64/astra-startup/` can prepare DDS and USD
assets from subscribed Workshop GMAs before Remix initializes. The launcher
installs this component after either the fixes or Remix package, including local
ZIP installs. Either installation order is supported. Enable Workshop addon
mounting to use subscribed packages; no manual copying of their GMAs is needed.

The loader is specific to the renderer recorded in its SHA-256 contract. A
matching renderer is preserved as `bin/win64/d3d9_astra_renderer.dll`; the wrapper
becomes `d3d9.dll`. Reinstalls verify the preserved renderer, and updates reapply
the wrapper only to a matching renderer. Unsupported renderers remain unwrapped
and the installation progress reports that startup PBR loading is not enabled.
Install matching fixes and Remix releases to enable it. Close the game before
updating either package. Older fixes packages and 32-bit/Linux installs retain
their existing behavior.
