set VERSION=0.1.9880

nuget pack NuGet\VmaBundle.template.nuspec -OutputDirectory output -Version %VERSION% -Properties distro=debian13
nuget pack NuGet\VmaBundle.template.nuspec -OutputDirectory output -Version %VERSION% -Properties distro=debian12
nuget pack NuGet\VmaBundle.template.nuspec -OutputDirectory output -Version %VERSION% -Properties distro=ubuntu2404
nuget pack NuGet\VmaBundle.template.nuspec -OutputDirectory output -Version %VERSION% -Properties distro=ubuntu2510