# GitPad.exe - Use Notepad as your Git commit editor

This single executable allows you to use Notepad as your editor any time Git
requires one (commits, interactive rebase, etc).

![Notepad editing a commit](http://f.cl.ly/items/3A3Y3P3B3Y3P1B0e2Q0Y/Grab.png)

## How to install (short version)

# [Click This Link](https://github-gitpad.s3.amazonaws.com/GitPad.zip)

## Notepad sucks! What about $FAVORITE\_EDITOR instead?

Good news! As of GitPad 1.2, the default editor will be whatever editor is
associated with .txt files. Normally, that's Notepad, but if you like a different
editor, you can now use that instead.

## How to install (long version)

Just copy GitPad.exe to a folder and double-click on it. It will install
itself into your Application Data directory (%AppData%) - if your default
editor is not set, GitPad.exe will set itself to EDITOR via setting an
Environment Variable on your user profile.

## What do I need to use this?

GitPad requires .NET 2.0 or higher.

## How to uninstall

Remove HKEY_CURRENT_USER\Environment\EDITOR from the registry and reboot your
system.
