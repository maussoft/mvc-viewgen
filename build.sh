#!/bin/bash
dotnet build
cp bin/Debug/net6.0/Maussoft.Mvc.ViewGen.dll ../mvc-example-cs/tools/
cp bin/Debug/net48/Maussoft.Mvc.ViewGen.dll ../mvc-example-cs/tools/win/
cp bin/Debug/net6.0/Maussoft.Mvc.ViewGen.dll ../mvc-example-vb/tools/
cp bin/Debug/net48/Maussoft.Mvc.ViewGen.dll ../mvc-example-vb/tools/win/
