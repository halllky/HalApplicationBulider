@echo off

set NIJO_ROOT=%~dp0
set PROJECT_ROOT=%NIJO_ROOT%�����e�X�g�ō쐬���ꂽ�v���W�F�N�g

@rem �R�[�h���������c�[�����ŐV��
dotnet publish %NIJO_ROOT%Nijo\Nijo.csproj -p:PublishProfile=PUBLISH

robocopy /s /NFL /NDL /NJH /NJS /nc /ns /np ^
  %NIJO_ROOT%Nijo\bin\Release\net8.0\win-x64\ApplicationTemplates ^
  %NIJO_ROOT%Nijo\bin\publish\ApplicationTemplates

@rem �R�[�h���������ƃr���h�����s
nijo fix %PROJECT_ROOT%

@rem �R���p�C���`�F�b�N
npm run tsc --prefix %PROJECT_ROOT%\react
dotnet build --project %PROJECT_ROOT%\webapi
