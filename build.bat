@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%�����e�X�g�ō쐬���ꂽ�v���W�F�N�g

@rem �R�[�h���������c�[�����ŐV��
rmdir /s /q %NIJO_ROOT%Nijo\bin\Debug\net7.0\win-x64\ApplicationTemplates
rmdir /s /q %NIJO_ROOT%Nijo\bin\publish\ApplicationTemplates
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

@rem �R�[�h���������ƃr���h�����s
nijo fix %PROJECT_ROOT%
npm run tsc --prefix %PROJECT_ROOT%\react
dotnet build --project %PROJECT_ROOT%\webapi
