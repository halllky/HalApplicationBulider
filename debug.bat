@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%�����e�X�g�ō쐬���ꂽ�v���W�F�N�g

@rem �R�[�h���������c�[�����ŐV��
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem �f�o�b�O�J�n
nijo debug %PROJECT_ROOT%
