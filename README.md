# Dungeon Generator
<details open>
<summary>ENGLISH</summary>
This project is a dungeon generation module for Unity-based games. It uses a modular architecture to create procedurally generated maps with various room types (Entry, Exit, Boss, Treasure, Fight) and corridors, ensuring flexibility and extensibility.

This generation module is similar to Enter the Gungeon generation system.

## Features

- **Procedural Dungeon Generation**  
  Generation is carried out using predefined room type requirements. The process includes placing the initial room, sequentially adding new rooms via corridors, and performing collision checks.

- **Various Room Types**  
  Supported room types:
  - **Entry:** The starting point of the dungeon.
  - **Exit:** The final point that must be reached.
  - **Boss:** A room containing the boss; once this room is placed, the exit is automatically generated.
  - **Treasure:** Rooms containing valuable items.
  - **Fight:** Rooms with enemies for combat encounters.

- **Modular Architecture**  
  The code is divided into several files/classes:
  - **DungeonGenerator.cs:** The main MonoBehaviour that controls the generation process.
  - **RoomInitializer.cs:** Initializes the room system (pools of prefabs, required counts, etc.).
  - **RoomPlacer.cs:** Contains methods for placing the initial room, corridors, new rooms, emergency generation, and the exit.
  - **CorridorHelper.cs:** Provides helper methods for working with corridors (selecting a prefab, determining door directions, finding a room with the appropriate door).
  - **RoomTypeHelper.cs:** Contains logic for selecting the next room type, checking if more rooms can be placed, and determining whether generation is complete.
  - **CollisionChecker.cs:** A separate module for collision checks between objects.

- **Emergency Generation**  
  If the primary algorithm fails to place all rooms, the GenerateDungeonEmergency method is invoked to attempt placing the exit using alternative logic.


- **Lazy Generation**  
  Lazy generation allows the dungeon to be created not instantly but with a specified time interval between iterations.
This feature can be useful for debugging or creating a more dynamic generation effect.  
  
- **Flexible Configuration**  
  All parameters such as generation attempt counts, corridor settings, and room requirements can be easily configured via the Unity Inspector.

## How It Works

1. **Initialization:**  
   The system initializes the room pools and sets the required count for each room type.

2. **Placing the Initial Room:**  
   The first room (Entry) is placed at the starting position.

3. **Main Generation Loop:**  
   The algorithm randomly selects an unconnected door from the already placed rooms and attempts to attach a corridor and a new room.  
   If placement fails (e.g., due to collisions), it will retry until a specified limit is reached.

4. **Placing the Boss and Exit Rooms:**  
   When a boss room is placed, the algorithm automatically triggers an attempt to attach an exit room. If the primary algorithm fails to place the exit, emergency generation is invoked.


## Setup and Usage

1. **Importing the Project:**  
   Copy the files into the appropriate folders in your Unity project (for example, scripts can be placed in the `Scripts/Level` folder).

2. **Configuring Prefabs:**  
   Add the necessary prefabs for rooms and corridors, and set their parameters—including doors and colliders the system can properly perform collision checks.

3. **Generation Configuration:**  
   In the Unity Inspector on the GameObject with the `DungeonGenerator` component, specify:
   - The list of corridor prefabs.
   - The room type requirements (parameters such as `minimumRequiredCount`, `maximumRequiredCount`, and the list of prefabs for each room).

4. **Running the Game:**  
   When the game starts, the `DungeonGenerator` script will automatically create the dungeon, invoke the generation methods.

## License

This project is licensed under MIT License. See the `LICENSE` file for details.

</details>

<details>
<summary>RUSSIAN</summary>
Этот проект представляет модуль генерации подземелий для игр на Unity. Он использует модульную архитектуру для создания процедурно генерируемых карт с различными типами комнат (вход, выход, комната босса, сокровищница, боевая комната) и коридоров, обеспечивая гибкость и возможность расширения.

Этот модуль генерации схож с системой генерации Enter the Gungeon.

## Функционал

- **Процедурная генерация подземелий**  
  Генерация происходит с использованием заданных требований к типам комнат. Процесс включает размещение начальной комнаты, последовательное добавление новых комнат посредством коридоров и проверку коллизий.

- **Различные типы комнат**  
  Поддерживаются следующие типы комнат:
  - **Entry:** Начальная точка подземелья.
  - **Exit:** Конечная точка, которую необходимо достичь.
  - **Boss:** Комната с боссом, после которой автоматически генерируется выход.
  - **Treasure:** Комнаты с ценными предметами.
  - **Fight:** Комнаты с врагами для сражений.

- **Модульная архитектура**  
  Код разделён на несколько файлов/классов:
  - **DungeonGenerator.cs:** Основной MonoBehaviour, управляющий процессом генерации.
  - **RoomInitializer.cs:** Инициализация системы комнат (пулы префабов, количество требуемых экземпляров).
  - **RoomPlacer.cs:** Методы для размещения начальной комнаты, коридоров, новых комнат, экстренной генерации и выхода.
  - **CorridorHelper.cs:** Вспомогательные методы для работы с коридорами (выбор префаба, определение направления двери, поиск подходящей комнаты).
  - **RoomTypeHelper.cs:** Логика выбора следующего типа комнаты, проверки возможности дальнейшего размещения комнат и завершения генерации.
  - **CollisionChecker.cs:** Отдельный модуль для проверки коллизий между объектами.

- **Emergency Generation**  
  Если основной алгоритм не смог разместить все комнаты, вызывается метод GenerateDungeonEmergency, который пытается корректно разместить выход, используя альтернативную логику.

- **Lazy Generation**  
  Ленивое создание подземелья позволяет генерировать уровни не мгновенно, а с заданным промежутком времени между итерациями.
Это может быть полезно для отладки или для создания более динамичного эффекта генерации.


- **Гибкая настройка**  
  Все параметры, такие как количество попыток генерации, настройки коридоров и требования к комнатам, легко настраиваются через инспектор Unity.

## Как это работает

1. **Инициализация:**  
   Система инициализирует пулы комнат и устанавливает требуемое количество экземпляров для каждого типа.

2. **Размещение начальной комнаты:**  
   Первая комната (Entry) размещается в начальной позиции.

3. **Основной цикл генерации:**  
   Алгоритм выбирает случайную незанятую дверь из уже размещённых комнат и пытается прикрепить к ней коридор и новую комнату.  
   Если размещение не удаётся (например, из-за коллизий), производится повторная попытка до заданного лимита.

4. **Размещение босс-комнаты и выхода:**  
   При размещении комнаты с боссом автоматически инициируется попытка прикрепления комнаты выхода. Если основной алгоритм не удаётся разместить выход, вызывается экстренная генерация.


## Установка и использование

1. **Импорт проекта:**  
   Скопируйте файлы в соответствующие папки проекта Unity (например, скрипты можно разместить в папке `Scripts/Level`).

2. **Настройка префабов:**  
   Добавьте необходимые префабы для комнат и коридоров, настройте их параметры, включая двери и коллайдеры, чтобы система могла корректно выполнять проверку коллизий.

3. **Конфигурация генерации:**  
   В инспекторе Unity на объекте с компонентом `DungeonGenerator` укажите:
   - Список префабов коридоров.
   - Список требований к типам комнат (параметры `minimumRequiredCount`, `maximumRequiredCount` и список префабов для каждой комнаты).

4. **Запуск игры:**  
   При запуске игры скрипт `DungeonGenerator` автоматически создаст подземелье, вызовет методы генерации.
   
## Лицензия

Этот проект распространяется под MIT License. Подробнее см. в файле `LICENSE`.

</details>
