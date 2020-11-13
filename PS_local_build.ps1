Get-Process msedge | ForEach-Object { $_.CloseMainWindow() }
./make clean
./make html
explorer "F:\Source\Repos\documentation\topohelper-documentation\_build\html\welcome.html"