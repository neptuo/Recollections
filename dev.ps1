wt new-tab -d "$pwd\src\Recollections.Api" --title "Api" powershell.exe -noexit -command "dotnet run";
wt new-tab -d "$pwd\src\Recollections.Blazor.UI" --title "Blazor run" powershell.exe -noexit -command "dotnet run --no-build";
wt new-tab -d "$pwd\src\Recollections.Blazor.UI" --title "Blazor watch" powershell.exe -noexit -command "dotnet watch build";
exit;