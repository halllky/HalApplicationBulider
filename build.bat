@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%�����e�X�g�ō쐬���ꂽ�v���W�F�N�g

@rem �R�[�h���������c�[�����ŐV��
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem �R�[�h���������ƃr���h�����s
nijo fix %PROJECT_ROOT%
npm run tsc --prefix %PROJECT_ROOT%\react
dotnet build --project %PROJECT_ROOT%\webapi
