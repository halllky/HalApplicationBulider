@echo off

@REM �A�v���P�[�V�����e���v���[�g��zip�����ăr���h��f�B���N�g���ɔz�u����B
@REM ���̃X�N���v�g�̓r���h������Ɏ��s�����B
@REM - ��1����: MSBuild�� $(ProjectDir)
@REM - ��2����: MSBuild�� $(OutDir)

set PROJECT_DIR=%1
set OUT_DIR=%PROJECT_DIR%%2

set ZIP_PATH=%OUT_DIR%templates.zip
set UNZIP_PATH=%OUT_DIR%ApplicationTemplates
set TEMPLATE_DIR=%PROJECT_DIR%..\Nijo.ApplicationTemplates

@REM -------------------------------------------
echo:
echo �A�v���P�[�V�����e���v���[�g�̓������J�n���܂��B
echo TEMPLATE_DIR: %TEMPLATE_DIR%
echo ZIP_PATH:     %ZIP_PATH%
echo UNZIP_PATH:   %UNZIP_PATH%
echo:

@REM zip��
pushd %TEMPLATE_DIR%
git archive --output="%ZIP_PATH%" HEAD
popd

@REM ���݂̉𓀌�f�B���N�g�����폜
del /s /q "%UNZIP_PATH%" >nul

@REM zip����
call powershell -command "Expand-Archive -Force %ZIP_PATH% %UNZIP_PATH%"

@REM zip���폜
del /q "%ZIP_PATH%"

echo:
exit /b
