cd src
%SYSTEMROOT%\Microsoft.NET\Framework\v4.0.30319\msbuild build.proj /target:Test /verbosity:normal
if errorlevel 1 pause
cd..
