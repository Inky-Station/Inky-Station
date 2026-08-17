@echo off
cd ../../

call dotnet run --project Content.Inky.Server --no-build %*

pause
