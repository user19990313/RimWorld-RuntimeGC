# RimWorld-RuntimeGC
A RimWorld mod - RuntimeGC

# What can this mod do?
Cleaning save files, recycling runtime memory, etc.

# Where can I subscribe this mod?
Ludeon Forums: https://ludeon.com/forums/index.php?topic=46581

Steam Workshop: https://steamcommunity.com/sharedfiles/filedetails/?id=962732083

# What kind of changes may I contribute?
1. Bug fixes. Note that you must test it yourself at least.
2. New features. Note that you must test it with at least 5 players with various behaviors and mod loadouts.
3. UI optimizations. Not recommended; it's simply painful.
4. Translations. See Translation section for details.

# What shouldn't I commit?
1. ANY files in RuntimeGC/About folder. If you found a typo in About.xml, please post it as a new issue.
2. New features of executing any pawn cleanups with a tick-based/auto-triggered control behavior. If you do so, I will reject the whole pull request; because the damage caused by this kind of behaviors is hard to rollback, or you simply do not care about save-file-safety of other players.
3. Change CopyrightStr or other author info. I know that static string is so vulnerable, but it's enough to trick those mod-stealing websites who simply wipe out <author /> tag.
4. Things you shouldn't do in any other repos, such as hiding trojans, name variables with f??k words, use indefinite number of spaces for indentations.

# Why the code style is inconsistent?
Well...Have you heard of the term *"legacy code"*? I started this project with B16 one and a half years ago, and rewriting all codes is too risky for now.

# How can I make translation files for %s?
Follow the steps:
1. Enable RuntimeGC, go to Options-Mod Settings, select RuntimeGC, uncheck "Clear unused translations".
2. Copy RuntimeGC/Languages/English folder.
3. Translate all the Keyed contents. Please do not make DefInjections for MainButtonWorkerDef as it will be translated using 3 Keyed translations instead of using injections.
4. Make sure the .xml files are saved using UTF-8 charset.
5 .Test your translations.
6. *(Optional)* Translate workshop description texts. Recommended; or other players using the same language will see English description texts instead.
7. Commit using PR. Make sure you are targeting the right destination.

# Translation progress
Completed|Outdated
---------|--------
English|Japanese(B18)(incl. workshop text)
ChineseSimplified|German(B18)(incl. workshop text)
ChineseTraditional|

 
 
 
Preview image by @duduluu

See About.xml for Close Alpha testers
