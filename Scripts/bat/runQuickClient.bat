@echo off
cd ../../

call dotnet run --project Content.Inky.Client --no-build %*

pause
