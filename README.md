# DayBird
Extension functionality for the NightHawk operator client

## Functionality

- **Plugin command** :: Introduces the 'plugin' command, which allows for the execution of .NET assemblies to automate / provide additional functionality to the base NH operator command suite, similar to Aggressor scripts from Cobalt Strike. These plugins have access to NightHawk functions through an intermediary dll that reflectively accesses functions in the NH Agent UI to run commands in beacons. Plugin names are all tab complete for ease of operator use, and so after entering the base plugin command you should be able to tab through the different loaded plugin modules.
- **AutoRuns menu** :: Allows for configuration of plugins that will execute automatically upon new beacon check-in. Based on configuration of the plugins enabled, arg values will be prompted for at configuration time.
- Additional opsec :: Commands can be filtered to stop certain risky commands detected by EDR from being passed in (e.g., "ps --detailed-info")

## Building

Few notes on building the projects in the solution: 

- Requires the NightHawk operator UI (UI.exe) to be placed into the .\Daybird\Daybird (folder containing DayBird.csproj file) to compile.
- Would recommend building everything as x64 in Visual Studio. May need to do some manual config changes if you're wanting to build Any CPU.
- If build doesnt work initially, would do a clean and re-build. Demo plugins in the solution rely on NHAPI which does get built first, but is not immediately recognized as a reference during the initial build process by the plugin projects.


## Additional Information

Further information on usage, compilation, deployment, and plugin creation can be found in the following blog post: [https://securityintelligence.com/x-force/extending-automating-nighthawk-with-daybird/](https://securityintelligence.com/x-force/extending-automating-nighthawk-with-daybird/)
