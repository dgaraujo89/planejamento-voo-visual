# Publica o app (Release, self-contained x64) e gera o instalador MSI.
# Pré-requisitos (uma vez):
#   dotnet tool install --global wix --version 5.0.2
#   wix extension add -g WixToolset.UI.wixext/5.0.2
#
# Uso:  powershell -ExecutionPolicy Bypass -File installer\build-installer.ps1

$ErrorActionPreference = 'Stop'
$raiz = Split-Path $PSScriptRoot -Parent

Write-Host "1/2  Publicando o app..." -ForegroundColor Cyan
dotnet publish "$raiz\PlanejamentoVooVisual\PlanejamentoVooVisual.csproj" `
    -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false `
    -o "$raiz\artifacts\publish"

Write-Host "2/2  Gerando o instalador MSI..." -ForegroundColor Cyan
$msi = "$raiz\artifacts\PlanejamentoVooVisual-Setup.msi"
# Executa a partir da pasta do .wxs para que os caminhos relativos (ícone, licença) resolvam.
Push-Location $PSScriptRoot
try {
    wix build "installer.wxs" -ext WixToolset.UI.wixext -arch x64 -o $msi
}
finally {
    Pop-Location
}

Write-Host "`nInstalador pronto: $msi" -ForegroundColor Green
