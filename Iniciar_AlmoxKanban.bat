@echo off
chcp 65001 > nul
title AlmoxKanban - Sistema de Gestao de Almoxarifado

:: Suporte automatico a caminhos de rede UNC (\\forest.local\...)
pushd "%~dp0"

echo ===================================================================
echo           ALMOXKANBAN - GESTAO OPERACIONAL DE TAREFAS
echo ===================================================================
echo.
echo Diretorio atual: %CD%
echo Iniciando o servidor local...
echo.
echo Endereco Local:   http://localhost:5000
echo Login Inicial:    admin
echo Senha Inicial:    Admin@123
echo.
echo Abrindo o navegador automaticamente...
echo ===================================================================
echo.

:: Aguarda 2 segundos e abre o navegador
start "" "http://localhost:5000"

:: Executa a aplicacao ASP.NET Core
dotnet run --no-build

if %ERRORLEVEL% NEQ 0 (
    echo.
    echo Tentando compilar e executar...
    dotnet run
)

popd
pause
