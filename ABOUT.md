# VS.Helper — ABOUT

## Команды меню VS.Helper

### Build Zip
Главная команда упаковки решения.

Как работает:
1. Определяет текущее Solution.
2. Загружает или создаёт `{SolutionName}.config`.
3. Если найден старый `VS.Helper.Zip.xml`, автоматически мигрирует его в новый config.
4. Строит список файлов по правилам Include/Exclude.
5. Уважает `Compile Remove`, `None Remove`, `EmbeddedResource Remove` из `.csproj`.
6. Собирает архив `{SolutionName}.zip`.
7. Копирует путь в буфер обмена.

Результат:
- `DietGenerator.zip`
- `GH.MailServer.zip`
- `VS.Helper.zip`

---

### Create Zip Config
Создаёт конфиг упаковки.

Как работает:
1. Берёт имя Solution.
2. Загружает существующий `{SolutionName}.config` или создаёт дефолтный.
3. Заполняет дефолтные Include/Exclude.
4. Открывает UI-редактор конфига.
5. По кнопке редактирования в UI открывает XML-файл в редакторе.

Пример:
- `DietGenerator.config`

---

### Self Upgrade
Самообновление VS.Helper.

Как работает:
1. Повышает версию VSIX manifest.
2. Генерирует и запускает внешний install-only скрипт.
3. Скрипт собирает проект и ищет свежий `.vsix`.
4. Запускает VSIX installer.
5. Устанавливает новую версию поверх текущей.

Важно:
- uninstall не нужен
- install only

---

### Run Swarm
Запускает агентную систему.

Как работает:
1. Запускает `AgentSwarmCore`.
2. Выполняет до нескольких проходов по ошибкам решения.
3. На каждом проходе запускает:
   - FixAgent
   - OptimizationAgent
   - QualityAgent
4. Делает rebuild решения и обновляет swarm memory/rules.

Используется для:
- fixes
- cleanup
- optimization

---

### Evolve Swarm
Режим обучения swarm.

Как работает:
1. Запускает `AgentSwarmEvolutionCore`.
2. Перестраивает правила на основе накопленной swarm memory.
3. Обновляет веса/доступность стратегий и сохраняет rule store.

Это evolutionary layer.

---

### Commit Stamp Sync Git
Git helper.

Как работает:
1. Формирует stamp и краткое описание изменений по staged-файлам.
2. Собирает commit message в формате `Stamp - описание`.
3. Выполняет `git add -A` и `git commit -m "{stamp} - {shortDescription}"`.
4. Выполняет `git pull --rebase` и `git push`.
5. Поддерживает хранение Git token в конфиге с DPAPI-защитой.

Используется для:
- changelog
- version trace
- commit history

---

### Tools
Премиальный сервисный центр VS.Helper для точечной инженерной работы с проектом.

Как работает:
1. Открывает единый интерфейс обслуживающих операций для активного Solution.
2. Выполняет выбранную задачу в управляемом режиме (с проверками и безопасными ограничениями).
3. Поддерживает сценарии backup/recovery для массовых изменений.
4. Отдаёт понятный лог результата, чтобы действие можно было быстро проверить и повторить.

Ключевые сценарии:
- config migration
- cleanup
- diagnostics
- repair
- mass refactoring helpers
- recovery from .bak

---

## CORE OS Layer

VS.Helper использует:

- CoreEngineV5
- SwarmEngineV5
- ExecutionGovernorV5
- PersistentMemory
- EventBus

Execution Flow:

Command
↓
Command Handler
↓
AI Agents / Zip / Upgrade services
↓
Result

Примечание:
- `CoreEngineV5` присутствует в проекте как отдельный runtime-слой.
- Текущие VS-команды в меню в основном используют специализированные сервисы и AI-агенты напрямую.