# Steam Coding Time Tracker (Kode Studio Sync)

[English Version](./README.en.md)

> **Linguagem / Stack:** `C# (.NET Framework 4.8 WinForms + System Tray)` | **Autor:** `piercer`

## O que faz

Monitora automaticamente suas IDEs e editores de código no Windows e sincroniza seu tempo de programação com a Steam (por padrão usando o app gratuito **Kode Studio — AppID `779260`**, ou qualquer AppID configurado).

- **Detecção automática de IDEs:** VS Code (`Code`), Cursor, Windsurf, Antigravity, Visual Studio (`devenv`), JetBrains (`pycharm64`, `idea64`, `rider64`, `webstorm64`, `goland64`, `clion64`), Sublime Text, Notepad++, Neovim, Android Studio e Zed.
- **Auto-Launch na Steam:** ao abrir qualquer IDE monitorada, inicia `steam://rungameid/<AppID>` automaticamente.
- **Auto-Close inteligente:** quando todas as IDEs são fechadas (após o tempo de tolerância configurável), encerra o processo alvo para parar a contagem na Steam.
- **Histórico local & System Tray:** registra sessões em `coding_sessions.csv`, acumula horas totais e roda leve na bandeja do Windows.

## Como executar / compilar

Execute `piercer_steam_coding_tracker.exe` ou recompile com:

```bat
build.bat
```

---
Desenvolvido por **piercer**
