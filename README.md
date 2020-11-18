# Ephemascript
## A simple scripting language for RPG games in Unity.

Here's an example code-snippet of ephemascript:

```
Tualia\ Hello there. No, don't worry, I'm wide awake. I'm just resting. I haven't been able to sleep much recently.

@PlayState shop
```

Ephemascript was developed for the game [Lost to Time](https://gamejolt.com/games/lost_to_time/283145). Please check it out!

## Requirements

Supports Unity 2019.4.12f1.

Most likely supports other versions of Unity as well.

a VSCode extension for syntax highlighting can be found [here](https://github.com/ephemeralglades/ephemascript_syntax_vscode)

## Usage / Setup

1. Download the unity package from `releases`.
2. Import the package into unity. You can uncheck the optional `Examples` folder .
3. Add the `Progress` component (or an extension of it) to a persistent gameobject. This is where variables are stored from scripts.
4. Extend the `NPC` Monobehavior to create your own NPCs. Each NPC can have their own functions that can be called within the script. Alternatively, you can use the base NPC component for a generic npc with basic functionality.
5. Add an `NPC` component to a gameObject. This represents the NPC itself.
6. Add an `NPCInteractable` component to an interface the player can interact with (for example, a button or a sprite).
7. Hook up a call to the `Interact` method of the `NPCInteractable`.
8. Set up a dialogue handler. See `ExampleDialogueHandler.cs` for an example.
--* Subscribe to `NPC.OnDialogueText` to retrieve the currently displayed text.
--* Subscribe to `NPC.OnDialogueHide` to hide the dialogue box
--* Call `NPC.CurrentlyPlayingNPC.ContinueDialogue()` to continue the conversation. Pass in a string parameter of the option to select a dialogue choice.
9. Write Ephemascript for each state of your NPCs.
10. Add your ephemascript.txt files to their corresponding `NPC` components and name the states.

That's it!


## Ephemascript Syntax

There will be a wiki eventually. Basic syntax is as follows:

### Basic Dialogue

```
SPEAKER\ TEXT

MORE TEXT

SPEAKER2\ TEXT
```

Note how if the speaker stays the same, you don't have to relist the speaker in consecutive lines.

All lines must be separated by two line breaks.


### Markers, Flow, Functions
```
SPEAKER\ Hi

@goto END

Bye

@END:
@end
```
In the above example, the speaker says "Hi". "Bye" is not printed, because the script jumps to the end.

The next line is an instruction (as it starts with a `@`). The `@goto` command is one of the built in commands. It immediately jumps the head of the script to a marker, in this case labelled `END`. 

`@END:` is a label. Note how it starts with an `@` and ends with a `:`, and it does NOT follow with a double line break.

`@end` is another built in command. It simply stops the conversation (unless you are in a nested state, in which case you need to write `@end all` to end the entire conversation).

### Variables, More Control
```
@var talkedTo false
@if var talkedTo true

	@end

@endif

Bob\ Hello!

@setvar talkedTo true
```

Variables must be declared at the top of each state script with only a single line break following them.

conditionals take 3 parameters, the conditional command, and 2 conditional parameters. You can implement your own conditional functions by extending the `NPC` class. basic ones such as `var`, `vargt` (is greater than), `varlt` (is less than), `varExt` (a variable from a different state. Must be denoted as STATENAME_VARIABLENAME).

`@setvar` and `@setvarExt` are available by default.

### Dialogue Options

```
Bob\ Who are you?
Nobody\NOBODY
Don't say\NOSAY

@NOBODY:
Me\ I'm no one...

@end

@NOSAY:
Me\ ...

Bob\ A person of very few words, eh

@end
```

Dialogue options directly follow a dialogue line. The format is [Text]\MARKER.

The dialogue simply jumps to the marker after the backslash when it is selected.

### Nesting

`Default.txt`
```
Bob\ Hi!

@PlayState Deep

Bob\ Hi 3!
```

`Deep.txt`
```
Bob\ Hi 2!
```

This will display all 3 His.

### More commands

`@runcr COROUTINENAME` - runs and waits for a coroutine to finish.

`@FUNCTIONNAME` - implement your own function by extending an `NPC` class.
