@ECHO off

ECHO.
set /p version="--> Enter version (eg. 1.0.2): " %=%

ECHO.
set /p tag=Enter tag: %=%

@ECHO off
set tag_without_spaces=%tag%
set tag_without_spaces=%tag_without_spaces: =_%
@ECHO off

ECHO.
..\Tools\AssemblyInfoUtil\AssemblyInfoUtil.exe -set:%VERSION%.* "..\Source\Cogworks.UmbracoFlare.Core\Properties\AssemblyInfo.cs"
..\Tools\AssemblyInfoUtil\AssemblyInfoUtil.exe -set:%version%.* "..\Source\Cogworks.UmbracoFlare.UI\Properties\AssemblyInfo.cs"

ECHO.
git commit -am "Updated to version %version%"
git tag "%tag_without_spaces%"
ECHO.
ECHO.

ECHO "Done"
pause

