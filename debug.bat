@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%�����e�X�g�ō쐬���ꂽ�v���W�F�N�g

@rem �R�[�h���������c�[�����ŐV��
rmdir /s /q %NIJO_ROOT%Nijo\bin\Debug\net7.0\win-x64\ApplicationTemplates
rmdir /s /q %NIJO_ROOT%Nijo\bin\publish\ApplicationTemplates
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem �f�o�b�O�J�n
nijo debug %PROJECT_ROOT%
