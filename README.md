# Floe Forward

A 3D climate action game for OCSEFHacks 2026

Floe Forward is a short game about climate change and Arctic sea ice. You control a polar bear as it swims between ice floes, hoping to find a new safe one. You collect fish to restore stamina and choose community actions that change the difficulty of later stages.

A floe is a piece of sea ice that floats on the ocean. The game uses the bear's journey to show why sea ice matters. It also gives players practical ideas for climate action.

## Why this matters

Coal, oil, and gas release greenhouse gases when people burn them. These gases trap heat and increase Earth's temperature. 
There currently exists a long-term decrease in Arctic sea ice. From 1979 through 2026, the trend in September sea-ice extent was a loss of 11.6% per decade, relative to the 1981-2010 average. This is a historical trend, with the extent changing from year to year, but is frightening nonetheless.

Polar bears use sea ice to hunt seals. Less sea ice can decrease their access to food, as well as refuge as they swim across the expansive Arctic Ocean.

The game connects this urgent problem to actions that communities can take. The project supports the UN's 13th sustainable development goal (SDG), climate action. Although the game does not reduce emissions by itself, it aims to educate people about such a pressing issue through an interactive video game.

## How to play

The game has three stages: 2016, 2026, and 2036.

- Each stage starts on thin ice, the polar bear's old home. The ice then breaks and the bear's journey starts.
- In water, the bear loses stamina. Treading on ice stops this loss.
- Fish restore up to 20 stamina out of a maximum of 100
- Solid ice is white, whereas thin ice cracks on contact and breaks shortly. If the bear is still on cracking thin ice, it loses 5 stamina.
- If stamina reaches zero, the journey stops, as the bear becomes too exhausted to move forward.
- If the bear completes a stage, it moves onto a safe floe, and opens up the player to make a community choice that could help, harm, or be moot for the environment.

### Community choices

After each of the first two stages, choose one of three cards. Each card shows its game effect before you select it.

The game adds the floe changes from your choices, and a later choice can offset an earlier choice. The result screen shows your choices and the practical climate actions they led to.

This mechanic was inspired by roguelike elements, with each run being one attempt, and a loss ending the run. Since choices change per stage, you can restart a run to make different ones, or progress further to see new, different climate actions. 

### Actions outside the game

As outlined in the game, there are a myriad ways an everyday person can help combat the climate problem countless organisms face.

- Walk, cycle, or use public transit if possible.
- Use less energy in your daily life (for example, remember to turn off lights you aren't using).
- Help your community use clean energy and make less waste.

## Learning and next steps

Before Floe Forward, I had only had experiences with making and finishing video games in Unity 2D. However, for this hackathon, I wanted to make a 3D game and show my expertise one step further.

I used basic geometric shapes for the block art style, inspired by simple yet engaging games such as Crossy Road. Unity primitives made this approach easier within the hackathon time frame, and, as such the bear has no skeleton or authored character animation. The code also creates the ice models and moves objects for simple visual effects. This approach let me create 3D models using pure code, letting me actualize my vision within the scope of the hackathon.

I used GitHub Copilot with a student license in Visual Studio Code in this project. Although I had experience with writing code for a simple video game in Unity before this and didn't use much of Copilot on that part as such, Copilot helped me learn how to write test code and use a test harness, and assisted me greatly on that portion. The harness gives me a repeatable way to check the game after code changes.
Therefore, I learned a lot about AI-assisted automated testing during this project. It was an amazing help in making the game more reliable. In the future, I would like to learn more about practical uses for this approach.

In the future, I hope to be able to expand this game or other 3D games with full 3D models with skeletons and character animation.

## Controls

- Start or restart: Press Enter. You can also click the button. 
- Move left: Hold A or the left arrow key. 
- Move right: Hold D or the right arrow key. 
- Select a card: Press 1, 2, or 3. You can also click the button on a card.

## Start the game

### Windows build

1. Keep all files in `Build\Windows` together.
2. Open `Build\Windows\FloeForward.exe`.

The game runs without an internet connection. The window starts at 1600 x 900 pixels and cannot be resized.

If the build folder is missing, make a Windows build with the steps below.

### Unity Editor

1. Install Unity Editor 6000.6.9f1 (Unity 6.6).
2. Open the `FloeForward` folder as a Unity project.
3. Open `Assets\Scenes\Journey.unity`.
4. Select Play.

### Make a Windows build

1. Install the Windows build module for Unity 6000.6.9f1.
2. Open the project in Unity.
3. Select Floe Forward > Build Windows.
4. Keep the complete `Build\Windows` folder when you copy the game.

## Technical architecture

The project uses Unity 6000.6.9f1, C#, the Universal Render Pipeline (URP), and the Unity Input System. The game runs on your computer. It does not need a server or an account.

The `Journey.unity` scene contains the main camera, `GameController`, and `GameUI`. The controller reads the stage data and creates the world. Trigger colliders detect contact with ice and fish. The user interface reads the game state and shows each screen.

| File | Function |
| --- | --- |
| `Assets\Scenes\Journey.unity` | Contains the main camera and the game object. |
| `Assets\Scripts\GameController.cs` | Controls input, game phases, stamina, world movement, and stage progress. |
| `Assets\Scripts\JourneyData.cs` | Contains the stage layouts, game values, and choice effects. |
| `Assets\Scripts\WorldObjects.cs` | Makes geometric art and controls the bear, ice, and fish. |
| `Assets\Scripts\GameUI.cs` | Draws the interface. The Input System handles keyboard and mouse input. Edit screen text, choice text, and font sizes on the `GameUI` component in `Journey.unity`. |
| `Assets\Scripts\JourneySelfTest.cs` | Runs automated checks when the `-floe-test` argument is present. |
| `Assets\Editor\ProjectBuilder.cs` | Sets up the project and builds the Windows game. |

## Automated checks

The test harness checks stage layouts, stamina, fish, thin ice, exhaustion, restart, and one full run with choice effects. It uses a fixed random seed. It does not test every choice path or replace a human playtest.

Build the latest source first. The Windows build may not contain the latest test code.

1. Open the project in Unity.
2. Select Floe Forward > Build Windows.
3. In PowerShell, go to the `FloeForward` folder.
4. Enter this command:

```powershell
.\Build\Windows\FloeForward.exe -floe-test -logFile .\floe-test.log
```

Wait for the game to close. Open `floe-test.log` and find `FLOE TEST PASS`.
