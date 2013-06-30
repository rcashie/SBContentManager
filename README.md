SBContentManager
================

Session Based XNA ContentManager

- Manages assets by associating them with specific sessions.
- A session can be freed when it is no longer needed (e.g. switching over to a new level).
- Assets that are only associated with freed sessions can be unloaded without affecting the rest of the game.
- Supports asynchronous batch loading for background loading.

See [Game.cs]( https://github.com/rcashie/SBContentManager/blob/master/SessionBasedContentManager/Source/Game.cs
) for the usage example.
