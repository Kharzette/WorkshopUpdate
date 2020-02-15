# Workshop Update
Updates steam workshop items specified in a simple config file

This was created to solve a common problem with running a Conan Exiles server.  Mod authors release updates often, and players get these automagically through steam, but the server keeps running with the old version leaving the players unable to join.

For the server admin, updating the mods manually is a real pain.  It requires figuring out which ones changed and manually copying them from a machine with the game installed.  These mods are usually buried in steam's overcomplicated workshop structure and require a lot of digging to find which have updated.  The steam client downloads section doesn't even mention what it is updating if you manage to catch it in the act for workshop items.

With this you can just set up the config file with the path to SteamCmd, the app ID of the game and server, and an optional mod list path for conan exiles.  Then add all the workshop ids of the mods to keep updated and run the program.

[![Foo](https://c5.patreon.com/external/logo/become_a_patron_button.png)](https://www.patreon.com/bePatron?u=27670106)
