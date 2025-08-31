# Lumin Warrior Game

Lumin Warrior is a complete 2D space shooter game developed with C# and the Windows Presentation Foundation (WPF) framework.  The game contains 6 levels, each filled with enemy spaceships, obstacles, and  bosses.

## 📸 Screenshots
![Screenshot for the UI of Lumin Warrior Game.](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/blob/master/readme-images/luminwarrior1.png)
<div align="center">
  Screenshot for the UI of Lumin Warrior Game.<br>
</div>
<br>

![Screenshot for the player with a protective shield.](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/blob/master/readme-images/luminwarrior2.png)
<div align="center">
  Screenshot for the player with a protective shield.<br>
</div>
<br>

![Screenshot for the player with a multi-directional rocket boost.](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/blob/master/readme-images/luminwarrior3.png)
<div align="center">
  Screenshot for the player with a multi-directional rocket boost.<br>
</div>
<br>

![Screenshot for the player fighting different types of enemies.](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/blob/master/readme-images/luminwarrior4.png)
<div align="center">
  Screenshot for the player fighting different types of enemies.<br>
</div>
<br>

![Screenshot for the player fighting one of main bosses in the game.](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/blob/master/readme-images/luminwarrior5.png)
<div align="center">
  Screenshot for the player fighting one of main bosses in the game.<br>
</div>
<br>

![Screenshot for the player fighting the final boss in the game.](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/blob/master/readme-images/luminwarrior6.png)
<div align="center">
  Screenshot for the player fighting the final boss in the game.<br>
</div>
<br>


## ✨ Features
- Interactive UI: A modern and responsive user interface.
- Six Levels: A complete campaign from start to finish.
- Dynamic Enemies: A Variety of enemy types with different behaviors, including a challenging final boss.
- Power-Up System: You can collect power-ups to gain a temporary advantage, such as a multi-directional rocket boost or a protective shield or health Boost.
- Obstacles : Obstacles that add a new layer of challenge to the gameplay.
- FREE FLY MODE: To Practice your flying skills in a simplified environment without enemies or obstacles - just you in the space.

## 📂 Project Structure
- UI/
  - MainWindow.xaml : defines the game's UI layout (the canvas, buttons, etc)
  - MainWindow.xaml.cs: contains the logic that initializes the game, handles events, and bridges all the other components together.
  - UIManager.cs: This class centralizes all the logic for managing the UI elements, such as updating the score, health bar, and showing/hiding menus.
- GameManager/
  - GameManager.cs: This class is the main controller for the entire game. It orchestrates the flow of the game, managing transitions between levels, pausing the game, and overseeing the interaction between the player, enemies, and other game objects
- Levels/
  - Levels.cs: Manages the progression of the game. It controls level loading, transitions, and the spawning of enemies and obstacles for each of the 6 levels.
- Player/
  - Player.cs: This is the core class for the main character. It handles player input, movement, firing mechanics, and its state.
- PowerUps/
  - PowerUps.cs: A dedicated system for spawning, managing, and applying the effects of power-ups to the player.
- HealthSystem/
  - Health.cs: A fundamental class that manages the health of the player.
  - BossHealthBar.cs: A specialized class for visually representing the health of the main bosses.
- Enemy/
  - Enemy.cs: This is a base class that defines the core behavior of all enemies and it contains the 2 types of the regular enemies (red enemy & rounded enemy)
  - MiniBoss.cs & FinalBoss.cs: These classes represent the final bosses in the game, with more complex behavior, health, and attacks than regular enemies.
  - SpaceStation.cs: This class represents a specific, static object in the final level, you can consider it as a source of enemies and it appears in the last level of the game with the final Boss.
- Obstacles/
  - Obstacles.cs: Manages the spawning and movement of the obstacles (the rocks).
- Background/
  - BackgroundManager.cs: Handles the animation of the game's background.
- Settings/
  - PausePopup.cs: Manages the UI and logic for the pause menu.
- Effects/
  - HitEffects.cs: Manages the visual effects that happen when objects are hit like explosions and particles.
  - SoundEffectsManager.cs: A global manager for playing all sound effects and music of the game.
- assets/
  - images
  - sounds

## 🎮 How to Play
### Controls
- Use Arrow Keys to move your spaceship
- Press 'M' button to switch to mouse control mode
- Press 'SPACEBAR' to fire rockets
- Press 'ESC' to pause/resume the game or return to main menu
### Objective
Destroy all incoming enemies and survive each level. As you progress, the game becomes more challenging with faster enemies and new obstacles. Defeat the final boss in Level 6 to win the game!

## 🤝 Contribution
Contributions, issues, and feature requests are welcome! Feel free to to submit a Pull Request.

## 📜 License
This project is licensed under the MIT License - see the ([LICENSE](LICENSE)) file for details.

## ⬇️ Download & Play
[**Win64-bit**](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/releases/download/1.0.1/luminWarriorWin64.rar)

[**Win32-bit**](https://github.com/MuhammidKhaled/Lumin-Warrior-Game/releases/download/1.0.1/LuminWarriorWin32.rar)
  
