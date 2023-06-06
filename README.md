# Steam Achievement Manager

Changes made in this fork:
- https://github.com/gibbed/SteamAchievementManager/pull/118
- https://github.com/gibbed/SteamAchievementManager/pull/119
- https://github.com/gibbed/SteamAchievementManager/pull/128
- Added sorting of games the global stats percentage of achievements (descending) using steam API
- Added system of gaining achievement over time (specified by the user in minutes), here is how it works:
- Added How Long To Beat (HLTB) system that makes call to their api to get an approx. time needed to complete the game with 100%

1) Choose your game as usual.
2) Click the new button 'Unlock all legit'.
3) Input a number of minutes you want the entire process to take.
  (approx. since this process is going to gradually add time for harder achievements- i.e. if you input 900 and there is 10 achievements, you can expect this number to go by about 10% which is 990minuts total).
4) Click Run and leave running in the background.

There is some tracking provided but it is far from ideal, I am sure some of those things (like minutes) are still broken but the system itself works just fine.
I am aware of some of the bugs and I am sure there is plenty more, the code is full of smells but I did not want to spend more than a day creating this so here we are. Feel free to edit, it's all there.

[Download latest release](https://github.com/Wosiu6/SteamAchievementManager/releases/latest).

[![Build status](https://ci.appveyor.com/api/projects/status/iovgeotwg1xtf7ik?svg=true)](https://ci.appveyor.com/project/JDM170/steamachievementmanager)

## Attribution

This is a fork of https://github.com/gibbed/SteamAchievementManager, credits to gibbed
Most (if not all) icons are from the [Fugue Icons](http://p.yusukekamiyamane.com/) set.
