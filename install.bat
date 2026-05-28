@echo off
chcp 65001 >nul

REM ============================================================
REM  Baum2 PS 插件一键安装脚本
REM  版本: v1.0  (2026-05-28)
REM  功能: 自动检测 Photoshop 安装路径并复制脚本文件
REM ============================================================

echo ============================================================
echo          Baum2 PS 插件一键安装
echo ==========================================
echo.

REM --- 步骤1: 检测 Photoshop 安装路径 ---
echo [1/4] 正在检测 Photoshop 安装路径...

set "PS_BASE=C:\Program Files\Adobe"
set "PS_FOUND=0"
set "PS_COUNT=0"

REM 收集所有匹配的 Scripts 目录
for /f "delims=" %%i in ('dir "%PS_BASE%\Adobe Photoshop*" /b /ad 2^>nul') do (
  set /a PS_COUNT+=1
  set "PS_DIR[!PS_COUNT!]=%%i"
  set "PS_PATH[!PS_COUNT!]=%PS_BASE%\%%i\Presets\Scripts"
)

if %PS_COUNT%==0 (
  echo.
  echo [!] 未检测到 Photoshop 安装目录。
  echo.
  echo 请手动将以下两个文件复制到 Photoshop 脚本目录：
  echo   PhotoshopScript\Baum.js
  echo   PhotoshopScript\BaumMapping.json
  echo.
  echo 目标路径示例：
  echo   C:\Program Files\Adobe\Adobe Photoshop 202X\Presets\Scripts\
  echo.
  pause
  exit /b 1
)

REM --- 步骤2: 让用户选择 PS 版本（如果检测到多个） ---
if %PS_COUNT%==1 (
  set /a SELECTED=1
) else (
  echo.
  echo 检测到 %PS_COUNT% 个 Photoshop 版本：
  echo.
  for /l %%i in (1,1,%PS_COUNT%) do (
    call echo   %%i. !PS_DIR[%%i]!
  )
  echo.
  set /p SELECTED=请选择要安装的版本（输入数字，默认 1）：
  if "!SELECTED!"=="" set "SELECTED=1"
)

REM 获取选中的路径
set "TARGET_DIR=!PS_PATH[%SELECTED%]!"

REM 验证目标目录是否存在
if not exist "%TARGET_DIR%\" (
  echo.
  echo [!] 目标目录不存在：%TARGET_DIR%
  echo 请确认 Photoshop 已正确安装。
  pause
  exit /b 1
)

echo.
echo [2/4] 目标安装目录：
echo   %TARGET_DIR%
echo.

REM --- 步骤3: 复制文件 ---
echo [3/4] 正在复制插件文件...

set "SRC_DIR=%~dp0PhotoshopScript"
set "FILE1=%SRC_DIR%\Baum.js"
set "FILE2=%SRC_DIR%\BaumMapping.json"

if not exist "%FILE1%" (
  echo [!] 找不到源文件：%FILE1%
  echo 请确保 install.bat 与 PhotoshopScript 目录在同一文件夹内。
  pause
  exit /b 1
)

copy /Y "%FILE1%" "%TARGET_DIR%\" >nul
if errorlevel 1 (
  echo [!] 复制 Baum.js 失败，请以管理员身份运行本脚本。
  pause
  exit /b 1
)
echo   [OK] 已复制 Baum.js

if exist "%FILE2%" (
  copy /Y "%FILE2%" "%TARGET_DIR%\" >nul
  echo   [OK] 已复制 BaumMapping.json
) else (
  echo   [跳过] BaumMapping.json 不存在，跳过
)

echo.

REM --- 步骤4: 完成提示 ---
echo [4/4] 安装完成！
echo.
echo ============================================================
echo  安装成功！请重启 Photoshop，然后：
echo.
echo  文件 → 脚本 → Baum.js
echo.
echo  即可使用 PS 转 Unity UI 导出功能。
echo ============================================================
echo.
pause
